using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace BagoScoutApp.Pages.AuthUser.Employer
{
    [QueryProperty(nameof(TargetUserId), "userId")]
    [QueryProperty(nameof(TargetUserName), "userName")]
    [QueryProperty(nameof(TargetUserPhoto), "userPhoto")]
    public partial class EMessagesPage : BasePage
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

        private Color GetThemeColor(string key, string fallbackHex)
        {
            if (App.Current != null && App.Current.Resources.TryGetValue(key, out var resource) && resource is Color color)
            {
                return color;
            }
            return Color.FromArgb(fallbackHex);
        }

        public EMessagesPage()
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

            // Check if navigated from Candidates view with a specific seeker target
            if (!string.IsNullOrEmpty(TargetUserId) && int.TryParse(TargetUserId, out var userId))
            {
                var userName = Uri.UnescapeDataString(TargetUserName ?? "Candidate");
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
            // Show loading, hide list
            ConversationsLoadingContainer.IsVisible = true;
            ConversationsLoadingIndicator.IsRunning = true;
            ConversationsCollectionView.IsVisible = false;

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
                        if (!changed)
                        {
                            // Hide loading, show list
                            ConversationsLoadingContainer.IsVisible = false;
                            ConversationsLoadingIndicator.IsRunning = false;
                            ConversationsCollectionView.IsVisible = true;
                            return;
                        }
                    }
                    ConversationsCollectionView.ItemsSource = convs;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employer conversations: {ex.Message}");
            }
            finally
            {
                // Hide loading, show list
                ConversationsLoadingContainer.IsVisible = false;
                ConversationsLoadingIndicator.IsRunning = false;
                ConversationsCollectionView.IsVisible = true;
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

                    var displayMessages = rawMessages.Select(m => {
                        bool isSender = m.senderId == _currentUserId;
                        return new DisplayMessage
                        {
                            messageText = m.messageText,
                            sentAt = m.sentAt.ToLocalTime(),
                            alignment = isSender ? LayoutOptions.End : LayoutOptions.Start,
                            bubbleColor = isSender ? GetThemeColor("Primary", "#6C63FF") : GetThemeColor("Gray200", "#E5E7EB"),
                            textColor = isSender ? Colors.White : GetThemeColor("PrimaryDark", "#1C2B53"),
                            timeColor = isSender ? GetThemeColor("Secondary", "#C7D2FE") : GetThemeColor("Gray500", "#8D94A8"),
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
                System.Diagnostics.Debug.WriteLine($"Error loading messages in employer chat: {ex.Message}");
            }
        }

        private void OnBackToConversationsClicked(object sender, EventArgs e)
        {
            MessagingService.ActiveChatUserId = 0;
            _activeConversation = null;
            ChatThreadGrid.IsVisible = false;
            ConversationsGrid.IsVisible = true;
            
            // Reload conversations to reflect new read status/last message
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
                System.Diagnostics.Debug.WriteLine($"Error sending employer message: {ex.Message}");
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

        private async Task ShowProfileOverlay(int seekerId)
        {
            try
            {
                ShowLoading("Loading profile...");
                var profile = await _api.GetSeekerProfileByIdAsync(seekerId);
                if (profile == null)
                {
                    HideLoading();
                    await ShowAlertAsync("Error", "Could not fetch profile details.", "OK");
                    return;
                }

                if (!string.IsNullOrEmpty(profile.selfiePhotoPath))
                {
                    var path = profile.selfiePhotoPath.Replace('\\', '/');
                    OverlaySelfieImage.Source = path.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(path))
                        : ImageSource.FromUri(new Uri($"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}"));
                }
                else
                {
                    OverlaySelfieImage.Source = null;
                }

                OverlayNameLabel.Text = $"{profile.firstName} {profile.lastName}";
                OverlayJobTitleLabel.Text = "Job Seeker";
                OverlayEmailLabel.Text = $"✉ {profile.email}";
                OverlayPhoneLabel.Text = $"☏ {profile.phoneNumber ?? "N/A"}";

                // Clear and rebuild skills
                OverlaySkillsLayout.Children.Clear();
                if (profile.skills != null && profile.skills.Count > 0)
                {
                    foreach (var skillName in profile.skills)
                    {
                        var border = new Border
                        {
                            BackgroundColor = GetThemeColor("Gray100", "#F3F4F6"),
                            StrokeThickness = 0,
                            StrokeShape = new RoundRectangle { CornerRadius = 12 },
                            Padding = new Thickness(10, 4),
                            Margin = new Thickness(0, 0, 8, 8),
                            Content = new Label { Text = skillName, FontSize = 12, TextColor = GetThemeColor("Gray600", "#4B5563") }
                        };
                        OverlaySkillsLayout.Children.Add(border);
                    }
                }
 
                // Experience
                OverlayExperienceLayout.Children.Clear();
                if (profile.experience != null && profile.experience.Count > 0)
                {
                    foreach (var exp in profile.experience)
                    {
                        var stack = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
                        stack.Children.Add(new Label { Text = exp.jobTitle, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = GetThemeColor("PrimaryDark", "#1C2B53") });
                        stack.Children.Add(new Label { Text = exp.company, FontSize = 12, TextColor = GetThemeColor("Primary", "#6C63FF") });
 
                        string dateText = "";
                        if (exp.startDate.HasValue)
                        {
                            dateText = exp.startDate.Value.ToString("MMM yyyy") + " - " +
                                       (exp.isCurrentJob || !exp.endDate.HasValue ? "Present" : exp.endDate.Value.ToString("MMM yyyy"));
                        }
                        if (!string.IsNullOrEmpty(dateText))
                        {
                            stack.Children.Add(new Label { Text = dateText, FontSize = 11, TextColor = GetThemeColor("Gray500", "#8D94A8") });
                        }
                        if (!string.IsNullOrEmpty(exp.description))
                        {
                            stack.Children.Add(new Label { Text = exp.description, FontSize = 12, TextColor = GetThemeColor("Gray600", "#4B5563"), Margin = new Thickness(0, 4, 0, 0) });
                        }
                        OverlayExperienceLayout.Children.Add(stack);
                    }
                }
                else
                {
                    OverlayExperienceLayout.Children.Add(new Label { Text = "No work experience listed", FontSize = 12, TextColor = GetThemeColor("Gray500", "#8D94A8") });
                }
 
                // Education
                OverlayEducationLayout.Children.Clear();
                if (profile.education != null && profile.education.Count > 0)
                {
                    foreach (var edu in profile.education)
                    {
                        var stack = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
                        stack.Children.Add(new Label { Text = edu.degree, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = GetThemeColor("PrimaryDark", "#1C2B53") });
                        stack.Children.Add(new Label { Text = edu.school, FontSize = 12, TextColor = GetThemeColor("Primary", "#6C63FF") });
 
                        string dateText = "";
                        if (edu.startDate.HasValue)
                        {
                            dateText = edu.startDate.Value.ToString("MMM yyyy") + " - " +
                                       (edu.endDate.HasValue ? edu.endDate.Value.ToString("MMM yyyy") : "Present");
                        }
                        if (!string.IsNullOrEmpty(dateText))
                        {
                            stack.Children.Add(new Label { Text = dateText, FontSize = 11, TextColor = GetThemeColor("Gray500", "#8D94A8") });
                        }
                        OverlayEducationLayout.Children.Add(stack);
                    }
                }
                else
                {
                    OverlayEducationLayout.Children.Add(new Label { Text = "No education listed", FontSize = 12, TextColor = GetThemeColor("Gray500", "#8D94A8") });
                }

                HideLoading();
                ProfileDetailsOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                HideLoading();
                await ShowAlertAsync("Error", $"Failed to show profile: {ex.Message}", "OK");
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
