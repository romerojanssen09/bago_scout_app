using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.AuthUser.Employer
{
    public partial class ECandidatesPage : BasePage
    {
        private readonly ApiClient _api = new();
        private List<ApplicationDto> _allCandidates = new();
        private List<JobDto> _employerJobs = new();
        private bool _isDataLoading = false;
        private ApplicationDto _currentApplicant;

        private Color GetThemeColor(string key, string fallbackHex)
        {
            if (App.Current != null && App.Current.Resources.TryGetValue(key, out var resource) && resource is Color color)
            {
                return color;
            }
            return Color.FromArgb(fallbackHex);
        }

        public ECandidatesPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadData();
        }

        private async Task LoadData()
        {
            _isDataLoading = true;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // Fetch both jobs and candidates concurrently
                var jobsTask = _api.GetEmployerJobsAsync();
                var candidatesTask = _api.GetAllEmployerApplicationsAsync();

                await Task.WhenAll(jobsTask, candidatesTask);

                _employerJobs = jobsTask.Result ?? new List<JobDto>();
                _allCandidates = candidatesTask.Result ?? new List<ApplicationDto>();

                // Setup picker items
                var pickerItems = new List<string> { "All Jobs" };
                pickerItems.AddRange(_employerJobs.Select(j => j.title));
                JobFilterPicker.ItemsSource = pickerItems;

                // Default selection
                if (JobFilterPicker.SelectedIndex < 0)
                {
                    JobFilterPicker.SelectedIndex = 0;
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading candidates: {ex.Message}");
                await ShowAlertAsync("Error", "Failed to load candidate applications.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                _isDataLoading = false;
            }
        }

        private void OnJobFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (JobFilterPicker.SelectedIndex < 0) return;

            var selectedFilter = JobFilterPicker.SelectedItem?.ToString() ?? "All Jobs";
            List<ApplicationDto> filteredList;

            if (selectedFilter == "All Jobs")
            {
                filteredList = _allCandidates;
            }
            else
            {
                filteredList = _allCandidates.Where(c => c.jobTitle.Equals(selectedFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Adjust photo URLs
            foreach (var candidate in filteredList)
            {
                if (!string.IsNullOrEmpty(candidate.seekerPhoto))
                {
                    var path = candidate.seekerPhoto.Replace('\\', '/');
                    candidate.seekerPhoto = path.StartsWith("http")
                        ? path
                        : $"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
                }
            }

            CandidatesCollectionView.ItemsSource = filteredList;
        }

        private async void OnAcceptCandidateClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApplicationDto applicant)
            {
                bool confirm = await ShowConfirmAsync("Accept Candidate?", $"Are you sure you want to accept '{applicant.seekerName}''s application?", "Accept", "Cancel");
                if (!confirm) return;

                try
                {
                    var response = await _api.UpdateApplicationStatusAsync(applicant.applicationId, "Accepted");
                    if (response.IsSuccessStatusCode)
                    {
                        await ShowAlertAsync("Accepted", $"{applicant.seekerName} has been accepted.", "OK");
                        await LoadData();
                    }
                    else
                    {
                        await ShowAlertAsync("Error", "Failed to update application status.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
                }
            }
        }

        private async void OnRejectCandidateClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApplicationDto applicant)
            {
                bool confirm = await ShowConfirmAsync("Reject Candidate?", $"Are you sure you want to reject '{applicant.seekerName}''s application?", "Reject", "Cancel");
                if (!confirm) return;

                try
                {
                    var response = await _api.UpdateApplicationStatusAsync(applicant.applicationId, "Rejected");
                    if (response.IsSuccessStatusCode)
                    {
                        await ShowAlertAsync("Rejected", "Application has been marked as rejected.", "OK");
                        await LoadData();
                    }
                    else
                    {
                        await ShowAlertAsync("Error", "Failed to update application status.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
                }
            }
        }

        private async void OnMoreActionsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApplicationDto applicant)
            {
                _currentApplicant = applicant;

                string emailOption = "Send Email";
                List<EmailHistoryDto> emailHistory = null;
                try
                {
                    emailHistory = await _api.GetEmailHistoryAsync(applicant.applicationId);
                    if (emailHistory != null && emailHistory.Count > 0)
                    {
                        emailOption = $"Emailed ({emailHistory.Count} sent)";
                    }
                }
                catch (Exception) { }

                var optionsList = new List<string> { "Message", "View Profile", emailOption };
                if (applicant.status == "Pending")
                {
                    optionsList.Add("Mark as Reviewed");
                }

                string choice = await ShowActionSheetAsync($"Actions for {applicant.seekerName}", "Cancel", optionsList.ToArray());

                if (choice == "Message")
                {
                    if (applicant.status == "Pending")
                    {
                        await _api.UpdateApplicationStatusAsync(applicant.applicationId, "Reviewed");
                    }
                    // Navigate to messages tab and pass candidate's information
                    await Shell.Current.GoToAsync($"//{nameof(EMessagesPage)}?userId={applicant.seekerId}&userName={Uri.EscapeDataString(applicant.seekerName)}&userPhoto={Uri.EscapeDataString(applicant.seekerPhoto ?? "")}", false);
                }
                else if (choice == "View Profile")
                {
                    if (applicant.status == "Pending")
                    {
                        await _api.UpdateApplicationStatusAsync(applicant.applicationId, "Reviewed");
                    }
                    await ShowProfileOverlay(applicant.seekerId, applicant.jobTitle);
                }
                else if (choice != null && (choice.StartsWith("Send Email") || choice.StartsWith("Emailed")))
                {
                    ShowEmailOverlay(emailHistory);
                }
                else if (choice == "Mark as Reviewed")
                {
                    try
                    {
                        var response = await _api.UpdateApplicationStatusAsync(applicant.applicationId, "Reviewed");
                        if (response.IsSuccessStatusCode)
                        {
                            await ShowAlertAsync("Status Updated", "Application status updated to Reviewed.", "OK");
                            await LoadData();
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async Task ShowProfileOverlay(int seekerId, string jobTitle)
        {
            try
            {
                var profile = await _api.GetSeekerProfileByIdAsync(seekerId);
                if (profile == null)
                {
                    await ShowAlertAsync("Error", "Could not fetch profile details.", "OK");
                    return;
                }

                // Selfie Image
                if (!string.IsNullOrEmpty(profile.selfiePhotoPath))
                {
                    var path = profile.selfiePhotoPath.Replace('\\', '/');
                    var selfieUrl = path.StartsWith("http") ? path : $"{_api.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
                    OverlaySelfieImage.Source = ImageSource.FromUri(new Uri(selfieUrl));
                }
                else
                {
                    OverlaySelfieImage.Source = null;
                }

                OverlayNameLabel.Text = $"{profile.firstName} {profile.lastName}";
                OverlayJobTitleLabel.Text = jobTitle;
                OverlayEmailLabel.Text = $"✉ {profile.email}";
                OverlayPhoneLabel.Text = $"☏ {profile.phoneNumber ?? "N/A"}";

                // Skills
                OverlaySkillsLayout.Children.Clear();
                if (profile.skills != null && profile.skills.Count > 0)
                {
                    foreach (var skillName in profile.skills)
                    { 
                        var border = new Border
                        {
                            Padding = new Thickness(10, 4),
                            BackgroundColor = GetThemeColor("Secondary", "#F0F4FF"),
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                            StrokeThickness = 0,
                            Margin = new Thickness(4)
                        };
                        border.Content = new Label { Text = skillName, FontSize = 12, TextColor = GetThemeColor("Primary", "#6C63FF"), FontAttributes = FontAttributes.Bold };
                        OverlaySkillsLayout.Children.Add(border);
                    }
                }
                else
                {
                    OverlaySkillsLayout.Children.Add(new Label { Text = "No skills listed", FontSize = 12, TextColor = GetThemeColor("Gray500", "#8D94A8"), Margin = new Thickness(4) });
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

                ProfileDetailsOverlay.IsVisible = true;
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Failed to show profile: {ex.Message}", "OK");
            }
        }

        private void OnCloseProfileOverlayClicked(object sender, EventArgs e)
        {
            ProfileDetailsOverlay.IsVisible = false;
            // Reload list to show any status change (Pending -> Reviewed)
            _ = LoadData();
        }

        private void ShowEmailOverlay(List<EmailHistoryDto> history)
        {
            EmailSubjectEntry.Text = "";
            EmailBodyEditor.Text = "";

            if (history != null && history.Count > 0)
            {
                EmailOverlayTitle.Text = "Email History & Composition";
                EmailHistoryHeader.IsVisible = true;
                EmailHistoryLayout.IsVisible = true;

                EmailHistoryLayout.Children.Clear();
                foreach (var mail in history)
                {
                    var border = new Border
                    {
                        Padding = new Thickness(12),
                        BackgroundColor = GetThemeColor("Gray100", "#F3F4F6"),
                        StrokeThickness = 1,
                        Stroke = GetThemeColor("Gray200", "#E5E7EB"),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    var stack = new VerticalStackLayout { Spacing = 4 };
                    stack.Children.Add(new Label { Text = mail.subject, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = GetThemeColor("PrimaryDark", "#1C2B53") });
                    stack.Children.Add(new Label { Text = mail.message, FontSize = 12, TextColor = GetThemeColor("Gray600", "#4B5563") });
                    stack.Children.Add(new Label { Text = $"Sent: {mail.sentAt.ToLocalTime():g}", FontSize = 10, TextColor = GetThemeColor("Gray500", "#8D94A8") });
                    border.Content = stack;
                    EmailHistoryLayout.Children.Add(border);
                }
            }
            else
            {
                EmailOverlayTitle.Text = "Send Email to Candidate";
                EmailHistoryHeader.IsVisible = false;
                EmailHistoryLayout.IsVisible = false;
            }

            EmailOverlay.IsVisible = true;
        }

        private void OnCloseEmailOverlayClicked(object sender, EventArgs e)
        {
            EmailOverlay.IsVisible = false;
        }

        private async void OnSendEmailSubmitClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EmailSubjectEntry.Text) || string.IsNullOrEmpty(EmailBodyEditor.Text))
            {
                await ShowAlertAsync("Validation Error", "Please fill in both the Subject and Message Body.", "OK");
                return;
            }

            SendEmailSubmitBtn.IsEnabled = false;
            try
            {
                var req = new EmailRequest
                {
                    applicationId = _currentApplicant.applicationId,
                    receiverId = _currentApplicant.seekerId,
                    to = _currentApplicant.seekerEmail,
                    subject = EmailSubjectEntry.Text,
                    message = EmailBodyEditor.Text
                };

                var success = await _api.SendEmailAsync(req);
                if (success)
                {
                    await ShowAlertAsync("Email Sent", "Your message has been sent to the candidate.", "OK");
                    EmailOverlay.IsVisible = false;

                    // Automatically mark as Reviewed if currently Pending
                    if (_currentApplicant.status == "Pending")
                    {
                        await _api.UpdateApplicationStatusAsync(_currentApplicant.applicationId, "Reviewed");
                    }

                    await LoadData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to send email. Please check configuration.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SendEmailSubmitBtn.IsEnabled = true;
            }
        }
    }
}
