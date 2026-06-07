using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    [QueryProperty(nameof(TargetUserId), "userId")]
    [QueryProperty(nameof(TargetUserName), "userName")]
    [QueryProperty(nameof(TargetUserPhoto), "userPhoto")]
    public partial class SMessagesPage : BasePage
    {
        private readonly ApiClient _api = new();
        private int _currentUserId;
        private ConversationDto? _activeConversation;

        private string _targetUserId = "";
        public string TargetUserId
        {
            get => _targetUserId;
            set
            {
                _targetUserId = value;
                OnPropertyChanged();
            }
        }

        private string _targetUserName = "";
        public string TargetUserName
        {
            get => _targetUserName;
            set
            {
                _targetUserName = value;
                OnPropertyChanged();
            }
        }

        private string _targetUserPhoto = "";
        public string TargetUserPhoto
        {
            get => _targetUserPhoto;
            set
            {
                _targetUserPhoto = value;
                OnPropertyChanged();
            }
        }

        public SMessagesPage()
        {
            InitializeComponent();
            _currentUserId = Preferences.Get("UserId", 0);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadConversations();
            MessagingService.ChatMessageReceived += OnChatMessageReceivedEvent;
            MessagingService.ConversationsUpdated += OnConversationsUpdatedEvent;

            // Check if navigated with a specific employer target
            if (!string.IsNullOrEmpty(TargetUserId) && int.TryParse(TargetUserId, out var userId))
            {
                var userName = Uri.UnescapeDataString(TargetUserName ?? "Employer");
                var userPhoto = !string.IsNullOrEmpty(TargetUserPhoto) ? Uri.UnescapeDataString(TargetUserPhoto) : "";
                
                // Clear the query properties so it doesn't loop/re-open on next navigation
                TargetUserId = "";
                TargetUserName = "";
                TargetUserPhoto = "";

                // Find existing conversation in list
                var conversations = (List<ConversationDto>?)ConversationsCollectionView.ItemsSource;
                var existing = conversations?.FirstOrDefault(c => c.userId == userId);

                if (existing != null)
                {
                    OpenChatWindow(existing);
                }
                else
                {
                    // Create dummy/new conversation DTO
                    var newConv = new ConversationDto
                    {
                        userId = userId,
                        firstName = userName,
                        lastName = "",
                        lastMessage = "",
                        lastMessageTime = DateTime.UtcNow.AddHours(8),
                        selfiePhotoUrl = userPhoto
                    };
                    OpenChatWindow(newConv);
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingService.ChatMessageReceived -= OnChatMessageReceivedEvent;
            MessagingService.ConversationsUpdated -= OnConversationsUpdatedEvent;
            MessagingService.ActiveChatUserId = 0;
        }

        private async Task LoadConversations()
        {
            try
            {
                var convs = await _api.GetConversationsAsync();
                if (convs != null)
                {
                    foreach (var conv in convs)
                    {
                        if (!string.IsNullOrEmpty(conv.selfiePhotoPath))
                        {
                            var path = conv.selfiePhotoPath.Replace('\\', '/');
                            conv.selfiePhotoUrl = path.StartsWith("http")
                                ? path
                                : $"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
                        }

                        // Convert lastMessageTime to PHT (UTC+8)
                        var utcTime = conv.lastMessageTime;
                        if (utcTime.Kind == DateTimeKind.Local)
                        {
                            utcTime = utcTime.ToUniversalTime();
                        }
                        else if (utcTime.Kind == DateTimeKind.Unspecified)
                        {
                            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
                        }
                        conv.lastMessageTime = utcTime.AddHours(8);
                    }

                    var currentList = (List<ConversationDto>?)ConversationsCollectionView.ItemsSource;
                    if (currentList != null && currentList.Count == convs.Count)
                    {
                        bool changed = false;
                        for (int i = 0; i < convs.Count; i++)
                        {
                            if (currentList[i].lastMessage != convs[i].lastMessage || 
                                currentList[i].unreadCount != convs[i].unreadCount ||
                                currentList[i].selfiePhotoUrl != convs[i].selfiePhotoUrl)
                            {
                                changed = true;
                                break;
                            }
                        }
                        if (!changed) return;
                    }
                    ConversationsCollectionView.ItemsSource = convs;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading conversations: {ex.Message}");
            }
        }

        private void OnConversationTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ConversationDto conversation)
            {
                OpenChatWindow(conversation);
            }
        }

        private async void OpenChatWindow(ConversationDto conversation)
        {
            _activeConversation = conversation;
            MessagingService.ActiveChatUserId = conversation.userId;
            ChatPartnerLabel.Text = conversation.fullName;

            if (!string.IsNullOrEmpty(conversation.selfiePhotoUrl))
            {
                ChatPartnerImage.Source = ImageSource.FromUri(new Uri(conversation.selfiePhotoUrl));
            }
            else
            {
                ChatPartnerImage.Source = null;
            }

            ConversationsGrid.IsVisible = false;
            ChatThreadGrid.IsVisible = true;

            await LoadChatMessages();
        }

        private async Task LoadChatMessages()
        {
            if (_activeConversation == null) return;

            try
            {
                // Clear any notification for this user
                var notificationService = DependencyService.Get<INotificationService>();
                notificationService?.DismissNotification(_activeConversation.userId);

                var rawMessages = await _api.GetMessagesAsync(_activeConversation.userId);
                if (rawMessages != null)
                {
                    var currentList = (List<DisplayMessage>?)MessagesCollectionView.ItemsSource;
                    if (currentList != null && currentList.Count == rawMessages.Count)
                    {
                        return; // Prevent flicker
                    }

                    var primaryColor = App.Current.Resources.TryGetValue("Primary", out var priVal) ? (Color)priVal : Color.FromArgb("#6C63FF");
                    var primaryDark = App.Current.Resources.TryGetValue("PrimaryDark", out var pdVal) ? (Color)pdVal : Color.FromArgb("#1C2B53");
                    var gray200 = App.Current.Resources.TryGetValue("Gray200", out var g2Val) ? (Color)g2Val : Color.FromArgb("#E5E7EB");
                    var gray500 = App.Current.Resources.TryGetValue("Gray500", out var g5Val) ? (Color)g5Val : Color.FromArgb("#8D94A8");

                    var displayMessages = rawMessages.Select(m => {
                        bool isSender = m.senderId == _currentUserId;
                        return new DisplayMessage
                        {
                            messageText = m.messageText,
                            sentAt = m.sentAt.ToLocalTime(),
                            alignment = isSender ? LayoutOptions.End : LayoutOptions.Start,
                            bubbleColor = isSender ? primaryColor : gray200,
                            textColor = isSender ? Colors.White : primaryDark,
                            timeColor = isSender ? Color.FromArgb("#E0E7FF") : gray500, // Using Indigo-100 equivalent for sender message time text color
                            strokeShape = isSender 
                                ? new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 0, 16) } 
                                : new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 16, 0) }
                        };
                    }).ToList();

                    MessagesCollectionView.ItemsSource = displayMessages;

                    // Scroll to bottom
                    if (displayMessages.Count > 0)
                    {
                        MessagesCollectionView.ScrollTo(displayMessages.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
        }

        private void OnBackToConversationsClicked(object sender, EventArgs e)
        {
            MessagingService.ActiveChatUserId = 0;
            _activeConversation = null;
            ChatThreadGrid.IsVisible = false;
            ConversationsGrid.IsVisible = true;
            
            // Reload conversations to reflect read statuses
            _ = LoadConversations();
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            if (_activeConversation == null) return;

            var text = MessageEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return;

            MessageEntry.Text = "";

            try
            {
                var response = await _api.SendMessageAsync(_activeConversation.userId, text);
                if (response.IsSuccessStatusCode)
                {
                    await LoadChatMessages();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private void OnChatMessageReceivedEvent(int senderId)
        {
            if (ChatThreadGrid.IsVisible && _activeConversation?.userId == senderId)
            {
                _ = LoadChatMessages();
            }
        }

        private void OnConversationsUpdatedEvent()
        {
            if (ConversationsGrid.IsVisible)
            {
                _ = LoadConversations();
            }
        }

        private async void OnProfilePicClicked(object sender, EventArgs e)
        {
            if (_activeConversation == null) return;
            await ShowProfileOverlay(_activeConversation.userId);
        }

        private void OnCloseProfileOverlayClicked(object sender, EventArgs e)
        {
            ProfileDetailsOverlay.IsVisible = false;
        }

        private async Task ShowProfileOverlay(int employerId)
        {
            try
            {
                ShowLoading("Loading company profile...");
                var profile = await _api.GetCompanyProfileByIdAsync(employerId);
                if (profile == null)
                {
                    HideLoading();
                    await ShowAlertAsync("Error", "Could not fetch company profile details.", "OK");
                    return;
                }

                if (!string.IsNullOrEmpty(profile.companyLogoPath))
                {
                    var path = profile.companyLogoPath.Replace('\\', '/');
                    OverlayLogoImage.Source = path.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(path))
                        : ImageSource.FromUri(new Uri($"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}"));
                }
                else
                {
                    OverlayLogoImage.Source = null;
                }

                OverlayCompanyNameLabel.Text = profile.companyName;
                OverlayIndustryLabel.Text = !string.IsNullOrEmpty(profile.companyIndustry) ? profile.companyIndustry : "Employer";
                OverlayDescriptionLabel.Text = !string.IsNullOrEmpty(profile.companyDescription) ? profile.companyDescription : "No description available.";
                OverlayAddressLabel.Text = $"Address: {profile.companyAddress}";
                OverlaySizeLabel.Text = $"Size: {profile.companySize}";
                OverlayWebsiteLabel.Text = $"Website: {(!string.IsNullOrEmpty(profile.companyWebsite) ? profile.companyWebsite : "N/A")}";

                OverlayRepNameLabel.Text = $"Representative: {profile.firstName} {profile.lastName}";
                OverlayRepEmailLabel.Text = $"✉ {profile.email}";
                OverlayRepPhoneLabel.Text = $"☏ {profile.phoneNumber ?? "N/A"}";

                HideLoading();
                ProfileDetailsOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                HideLoading();
                await ShowAlertAsync("Error", $"Failed to show company profile: {ex.Message}", "OK");
            }
        }

        public class DisplayMessage
        {
            public string messageText { get; set; } = "";
            public DateTime sentAt { get; set; }
            public LayoutOptions alignment { get; set; }
            public Color bubbleColor { get; set; } = Colors.LightGray;
            public Color textColor { get; set; } = Colors.Black;
            public Color timeColor { get; set; } = Colors.Gray;
            public RoundRectangle strokeShape { get; set; } = new();
        }
    }
}
