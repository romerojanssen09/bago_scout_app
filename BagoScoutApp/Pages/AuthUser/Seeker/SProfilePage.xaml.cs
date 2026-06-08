using BagoScoutApp.Pages.Components;
using BagoScoutApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace BagoScoutApp.Pages.AuthUser.Seeker
{
    public partial class SProfilePage : BasePage
    {
        private readonly ApiClient _api = new();
        private SeekerProfileDto? _profile;
        private List<SkillDto> _allSkills = new();

        public SProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProfileData();
        }

        private async Task LoadProfileData()
        {
            try
            {
                // Fetch profile and all skills concurrently
                var profileTask = _api.GetSeekerProfileAsync();
                var skillsTask = _api.GetAllSkillsAsync();

                await Task.WhenAll(profileTask, skillsTask);

                _profile = profileTask.Result;
                _allSkills = skillsTask.Result ?? new List<SkillDto>();

                if (_profile != null)
                {
                    // Populate basic fields
                    UserFullNameLabel.Text = $"{_profile.firstName} {_profile.lastName}".Trim();
                    UserEmailLabel.Text = _profile.email;
                    
                    FirstNameEntry.Text = _profile.firstName;
                    LastNameEntry.Text = _profile.lastName;
                    PhoneNumberEntry.Text = _profile.phoneNumber;

                    // Load Profile Picture
                    if (!string.IsNullOrEmpty(_profile.selfiePhotoPath))
                    {
                        var photoUrl = _profile.selfiePhotoPath.StartsWith("http") 
                            ? _profile.selfiePhotoPath 
                            : $"{_api.BaseUrl.TrimEnd('/')}/{_profile.selfiePhotoPath.TrimStart('/')}";

                        ProfileImage.Source = ImageSource.FromUri(new Uri(photoUrl));
                        ProfileImage.IsVisible = true;
                        ProfilePlaceholder.IsVisible = false;
                    }
                    else
                    {
                        ProfileImage.IsVisible = false;
                        ProfilePlaceholder.IsVisible = true;
                    }

                    // Render Skills
                    RenderSkillsList(_profile.skills);

                    // Render Education & Experience
                    RenderEducationList(_profile.education);
                    RenderExperienceList(_profile.experience);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile data: {ex.Message}");
                await ShowAlertAsync("Error", "Failed to load profile details.", "OK");
            }
        }

        private void RenderSkillsList(List<string> userSkills)
        {
            SkillsFlexLayout.Children.Clear();

            var gray500 = App.Current.Resources.TryGetValue("Gray500", out var g5Val) ? (Color)g5Val : Color.FromArgb("#8D94A8");
            var chipColor = App.Current.Resources.TryGetValue("Secondary", out var secColor) ? (Color)secColor : Color.FromArgb("#F0F4FF");
            var strokeColor = App.Current.Resources.TryGetValue("Primary", out var priColor) ? (Color)priColor : Color.FromArgb("#6C63FF");
            var darkTextColor = App.Current.Resources.TryGetValue("PrimaryDark", out var pdColor) ? (Color)pdColor : Color.FromArgb("#1C2B53");

            if (userSkills == null || userSkills.Count == 0)
            {
                SkillsFlexLayout.Children.Add(new Label
                {
                    Text = "No skills added yet",
                    TextColor = gray500,
                    FontSize = 13,
                    Margin = new Thickness(0, 5, 0, 5)
                });
                return;
            }

            foreach (var skillName in userSkills)
            {
                var chipBorder = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    StrokeThickness = 1,
                    Stroke = strokeColor,
                    BackgroundColor = chipColor,
                    Padding = new Thickness(12, 6),
                    Margin = new Thickness(0, 0, 8, 8)
                };

                var grid = new Grid
                {
                    ColumnDefinitions = 
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 8
                };

                var label = new Label
                {
                    Text = skillName,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = darkTextColor,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(label, 0);

                var deleteLabel = new Label
                {
                    Text = "\uf00d",
                    FontFamily = "FASolid",
                    TextColor = strokeColor,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(2, 0)
                };

                var nameCapture = skillName; // Capture variable
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await OnDeleteSkillTapped(nameCapture);
                deleteLabel.GestureRecognizers.Add(tapGesture);
                Grid.SetColumn(deleteLabel, 1);

                grid.Children.Add(label);
                grid.Children.Add(deleteLabel);
                chipBorder.Content = grid;

                SkillsFlexLayout.Children.Add(chipBorder);
            }
        }

        private async Task OnDeleteSkillTapped(string skillName)
        {
            if (_profile == null) return;

            bool confirm = await ShowConfirmAsync("Delete Skill", $"Are you sure you want to remove '{skillName}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                // Find all skill IDs currently mapped except this one
                var skillIdsToKeep = _profile.skills
                    .Where(name => name != skillName)
                    .Select(name => _allSkills.FirstOrDefault(s => s.skillName.Equals(name, StringComparison.OrdinalIgnoreCase))?.skillId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                var response = await _api.UpdateSeekerSkillsAsync(skillIdsToKeep);
                if (response.IsSuccessStatusCode)
                {
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to update skills.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            if (_profile == null) return;

            var firstName = FirstNameEntry.Text?.Trim() ?? "";
            var lastName = LastNameEntry.Text?.Trim() ?? "";
            var phone = PhoneNumberEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                await ShowAlertAsync("Required Fields", "First name and last name are required.", "OK");
                return;
            }

            SaveProfileBtn.IsEnabled = false;
            SaveProfileBtn.Text = "Saving...";

            try
            {
                var payload = new { FirstName = firstName, LastName = lastName, PhoneNumber = phone };
                var response = await _api.UpdateSeekerProfileAsync(payload);

                if (response.IsSuccessStatusCode)
                {
                    // Update preferences so the welcome label updates
                    Preferences.Set("UserName", $"{firstName} {lastName}".Trim());
                    await ShowAlertAsync("Success", "Profile updated successfully.", "OK");
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to update profile.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SaveProfileBtn.IsEnabled = true;
                SaveProfileBtn.Text = "Save Profile";
            }
        }

        private async void OnAddCustomSkillClicked(object sender, EventArgs e)
        {
            var skillName = CustomSkillEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(skillName)) return;

            try
            {
                var response = await _api.CreateCustomSkillAsync(skillName);
                if (response.IsSuccessStatusCode)
                {
                    CustomSkillEntry.Text = "";
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to add skill.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void OnChangePhotoClicked(object sender, EventArgs e)
        {
            FileResult? photo = null;
            try
            {
                // Directly open photo picker without action sheet
                photo = await MediaPicker.Default.PickPhotoAsync();

                if (photo != null)
                {
                    // Upload photo
                    var localPath = photo.FullPath;
                    var response = await _api.UploadProfilePhotoAsync(localPath);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowAlertAsync("Success", "Profile photo updated successfully.", "OK");
                        await LoadProfileData();
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Upload photo failed: {errorBody}");
                        await ShowAlertAsync("Upload Failed", "Failed to upload photo. Ensure it is a valid image type.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Photo selection/upload error: {ex.Message}");
                await ShowAlertAsync("Error", "An error occurred while setting profile picture.", "OK");
            }
        }

        private EducationDto? _currentEditingEdu;
        private ExperienceDto? _currentEditingExp;

        private void RenderEducationList(List<EducationDto> education)
        {
            EducationListLayout.Children.Clear();

            var primaryColor = App.Current.Resources.TryGetValue("Primary", out var priVal) ? (Color)priVal : Color.FromArgb("#6C63FF");
            var primaryDark = App.Current.Resources.TryGetValue("PrimaryDark", out var pdVal) ? (Color)pdVal : Color.FromArgb("#1C2B53");
            var gray200 = App.Current.Resources.TryGetValue("Gray200", out var g2Val) ? (Color)g2Val : Color.FromArgb("#E5E7EB");
            var gray500 = App.Current.Resources.TryGetValue("Gray500", out var g5Val) ? (Color)g5Val : Color.FromArgb("#8D94A8");

            if (education == null || education.Count == 0)
            {
                EducationListLayout.Children.Add(new Label
                {
                    Text = "No education details added yet.",
                    TextColor = gray500,
                    FontSize = 13,
                    Margin = new Thickness(0, 5)
                });
                return;
            }

            foreach (var edu in education)
            {
                var card = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    StrokeThickness = 1,
                    Stroke = gray200,
                    Padding = new Thickness(12),
                    BackgroundColor = Colors.White,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var grid = new Grid
                {
                    RowDefinitions = 
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    ColumnDefinitions = 
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    RowSpacing = 4
                };

                var titleLabel = new Label
                {
                    Text = $"{edu.degree} in {edu.fieldOfStudy}",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = primaryDark,
                    FontSize = 14
                };
                Grid.SetRow(titleLabel, 0);
                Grid.SetColumn(titleLabel, 0);

                var schoolLabel = new Label
                {
                    Text = edu.school,
                    TextColor = primaryColor,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                };
                Grid.SetRow(schoolLabel, 1);
                Grid.SetColumn(schoolLabel, 0);

                var dateStr = $"{edu.startDate?.ToString("MMMM yyyy") ?? "-"} to {edu.endDate?.ToString("MMMM yyyy") ?? "Present"}";
                var dateLabel = new Label
                {
                    Text = dateStr,
                    TextColor = gray500,
                    FontSize = 11
                };
                Grid.SetRow(dateLabel, 2);
                Grid.SetColumn(dateLabel, 0);

                // Edit/Delete Buttons
                var actions = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Start };
                Grid.SetRow(actions, 0);
                Grid.SetColumn(actions, 1);
                Grid.SetRowSpan(actions, 2);

                var editBtn = new Label { Text = "\uf044", FontFamily = "FASolid", FontSize = 14, TextColor = primaryColor, Padding = 6 };
                var editTap = new TapGestureRecognizer();
                editTap.Tapped += (s, e) => OnEditEducationClicked(edu);
                editBtn.GestureRecognizers.Add(editTap);

                var deleteBtn = new Label { Text = "\uf2ed", FontFamily = "FASolid", FontSize = 14, TextColor = Color.FromArgb("#EF4444"), Padding = 6 };
                var deleteTap = new TapGestureRecognizer();
                deleteTap.Tapped += async (s, e) => await OnDeleteEducationClicked(edu.educationId);
                deleteBtn.GestureRecognizers.Add(deleteTap);

                actions.Add(editBtn);
                actions.Add(deleteBtn);

                grid.Children.Add(titleLabel);
                grid.Children.Add(schoolLabel);
                grid.Children.Add(dateLabel);
                grid.Children.Add(actions);

                card.Content = grid;
                EducationListLayout.Children.Add(card);
            }
        }

        private void RenderExperienceList(List<ExperienceDto> experience)
        {
            ExperienceListLayout.Children.Clear();

            var primaryColor = App.Current.Resources.TryGetValue("Primary", out var priVal) ? (Color)priVal : Color.FromArgb("#6C63FF");
            var primaryDark = App.Current.Resources.TryGetValue("PrimaryDark", out var pdVal) ? (Color)pdVal : Color.FromArgb("#1C2B53");
            var gray200 = App.Current.Resources.TryGetValue("Gray200", out var g2Val) ? (Color)g2Val : Color.FromArgb("#E5E7EB");
            var gray500 = App.Current.Resources.TryGetValue("Gray500", out var g5Val) ? (Color)g5Val : Color.FromArgb("#8D94A8");

            if (experience == null || experience.Count == 0)
            {
                ExperienceListLayout.Children.Add(new Label
                {
                    Text = "No work experience details added yet.",
                    TextColor = gray500,
                    FontSize = 13,
                    Margin = new Thickness(0, 5)
                });
                return;
            }

            foreach (var exp in experience)
            {
                var card = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    StrokeThickness = 1,
                    Stroke = gray200,
                    Padding = new Thickness(12),
                    BackgroundColor = Colors.White,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var grid = new Grid
                {
                    RowDefinitions = 
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    ColumnDefinitions = 
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    RowSpacing = 4
                };

                var titleLabel = new Label
                {
                    Text = exp.jobTitle,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = primaryDark,
                    FontSize = 14
                };
                Grid.SetRow(titleLabel, 0);
                Grid.SetColumn(titleLabel, 0);

                var companyLabel = new Label
                {
                    Text = $"{exp.company} - {exp.location}",
                    TextColor = primaryColor,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                };
                Grid.SetRow(companyLabel, 1);
                Grid.SetColumn(companyLabel, 0);

                var dateStr = $"{exp.startDate?.ToString("MMMM yyyy") ?? "-"} to {(exp.isCurrentJob ? "Present" : exp.endDate?.ToString("MMMM yyyy") ?? "-")}";
                var dateLabel = new Label
                {
                    Text = dateStr,
                    TextColor = gray500,
                    FontSize = 11
                };
                Grid.SetRow(dateLabel, 2);
                Grid.SetColumn(dateLabel, 0);

                // Edit/Delete Buttons
                var actions = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Start };
                Grid.SetRow(actions, 0);
                Grid.SetColumn(actions, 1);
                Grid.SetRowSpan(actions, 2);

                var editBtn = new Label { Text = "\uf044", FontFamily = "FASolid", FontSize = 14, TextColor = primaryColor, Padding = 6 };
                var editTap = new TapGestureRecognizer();
                editTap.Tapped += (s, e) => OnEditExperienceClicked(exp);
                editBtn.GestureRecognizers.Add(editTap);

                var deleteBtn = new Label { Text = "\uf2ed", FontFamily = "FASolid", FontSize = 14, TextColor = Color.FromArgb("#EF4444"), Padding = 6 };
                var deleteTap = new TapGestureRecognizer();
                deleteTap.Tapped += async (s, e) => await OnDeleteExperienceClicked(exp.experienceId);
                deleteBtn.GestureRecognizers.Add(deleteTap);

                actions.Add(editBtn);
                actions.Add(deleteBtn);

                grid.Children.Add(titleLabel);
                grid.Children.Add(companyLabel);
                grid.Children.Add(dateLabel);
                grid.Children.Add(actions);

                card.Content = grid;
                ExperienceListLayout.Children.Add(card);
            }
        }

        // --- EDUCATION EVENT HANDLERS ---
        private void OnAddEducationClicked(object sender, EventArgs e)
        {
            _currentEditingEdu = null;
            EduFormTitleLabel.Text = "Add Education";
            EduSchoolEntry.Text = "";
            EduDegreeEntry.Text = "";
            EduFieldEntry.Text = "";
            EduStartDatePicker.Date = DateTime.Today.AddYears(-4);
            EduEndDatePicker.Date = DateTime.Today;
            EduDescEditor.Text = "";
            EducationFormOverlay.IsVisible = true;
        }

        private void OnEditEducationClicked(EducationDto edu)
        {
            _currentEditingEdu = edu;
            EduFormTitleLabel.Text = "Edit Education";
            EduSchoolEntry.Text = edu.school;
            EduDegreeEntry.Text = edu.degree;
            EduFieldEntry.Text = edu.fieldOfStudy;
            EduStartDatePicker.Date = edu.startDate ?? DateTime.Today;
            EduEndDatePicker.Date = edu.endDate ?? DateTime.Today;
            EduDescEditor.Text = edu.description ?? "";
            EducationFormOverlay.IsVisible = true;
        }

        private void OnCancelEduClicked(object sender, EventArgs e)
        {
            EducationFormOverlay.IsVisible = false;
            _currentEditingEdu = null;
        }

        private async void OnSaveEduClicked(object sender, EventArgs e)
        {
            var school = EduSchoolEntry.Text?.Trim() ?? "";
            var degree = EduDegreeEntry.Text?.Trim() ?? "";
            var field = EduFieldEntry.Text?.Trim() ?? "";
            var start = EduStartDatePicker.Date;
            var end = EduEndDatePicker.Date;
            var desc = EduDescEditor.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(school) || string.IsNullOrEmpty(degree))
            {
                await ShowAlertAsync("Validation Error", "School and degree are required.", "OK");
                return;
            }

            SaveEduBtn.IsEnabled = false;
            SaveEduBtn.Text = "Saving...";

            try
            {
                var payload = new EducationDto
                {
                    school = school,
                    degree = degree,
                    fieldOfStudy = field,
                    startDate = start,
                    endDate = end,
                    description = desc,
                    userId = _profile?.userId ?? 0
                };

                HttpResponseMessage response;
                if (_currentEditingEdu == null)
                {
                    response = await _api.CreateEducationAsync(payload);
                }
                else
                {
                    payload.educationId = _currentEditingEdu.educationId;
                    response = await _api.UpdateEducationAsync(_currentEditingEdu.educationId, payload);
                }

                if (response.IsSuccessStatusCode)
                {
                    EducationFormOverlay.IsVisible = false;
                    _currentEditingEdu = null;
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to save education detail.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SaveEduBtn.IsEnabled = true;
                SaveEduBtn.Text = "Save Education";
            }
        }

        private async Task OnDeleteEducationClicked(int id)
        {
            bool confirm = await ShowConfirmAsync("Delete Education", "Are you sure you want to remove this education detail?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var response = await _api.DeleteEducationAsync(id);
                if (response.IsSuccessStatusCode)
                {
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to delete education detail.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
        }

        // --- EXPERIENCE EVENT HANDLERS ---
        private void OnAddExperienceClicked(object sender, EventArgs e)
        {
            _currentEditingExp = null;
            ExpFormTitleLabel.Text = "Add Experience";
            ExpTitleEntry.Text = "";
            ExpCompanyEntry.Text = "";
            ExpLocationEntry.Text = "";
            ExpCurrentJobCheckBox.IsChecked = false;
            ExpStartDatePicker.Date = DateTime.Today.AddYears(-2);
            ExpEndDatePicker.Date = DateTime.Today;
            ExpEndDateLayout.IsVisible = true;
            ExpDescEditor.Text = "";
            ExperienceFormOverlay.IsVisible = true;
        }

        private void OnEditExperienceClicked(ExperienceDto exp)
        {
            _currentEditingExp = exp;
            ExpFormTitleLabel.Text = "Edit Experience";
            ExpTitleEntry.Text = exp.jobTitle;
            ExpCompanyEntry.Text = exp.company;
            ExpLocationEntry.Text = exp.location;
            ExpCurrentJobCheckBox.IsChecked = exp.isCurrentJob;
            ExpStartDatePicker.Date = exp.startDate ?? DateTime.Today;
            ExpEndDatePicker.Date = exp.endDate ?? DateTime.Today;
            ExpEndDateLayout.IsVisible = !exp.isCurrentJob;
            ExpDescEditor.Text = exp.description ?? "";
            ExperienceFormOverlay.IsVisible = true;
        }

        private void OnCancelExpClicked(object sender, EventArgs e)
        {
            ExperienceFormOverlay.IsVisible = false;
            _currentEditingExp = null;
        }

        private void OnCurrentJobCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            ExpEndDateLayout.IsVisible = !e.Value;
        }

        private async void OnSaveExpClicked(object sender, EventArgs e)
        {
            var title = ExpTitleEntry.Text?.Trim() ?? "";
            var company = ExpCompanyEntry.Text?.Trim() ?? "";
            var location = ExpLocationEntry.Text?.Trim() ?? "";
            var isCurrent = ExpCurrentJobCheckBox.IsChecked;
            var start = ExpStartDatePicker.Date;
            var end = isCurrent ? (DateTime?)null : ExpEndDatePicker.Date;
            var desc = ExpDescEditor.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(company))
            {
                await ShowAlertAsync("Validation Error", "Job title and company are required.", "OK");
                return;
            }

            SaveExpBtn.IsEnabled = false;
            SaveExpBtn.Text = "Saving...";

            try
            {
                var payload = new ExperienceDto
                {
                    jobTitle = title,
                    company = company,
                    location = location,
                    isCurrentJob = isCurrent,
                    startDate = start,
                    endDate = end,
                    description = desc,
                    userId = _profile?.userId ?? 0
                };

                HttpResponseMessage response;
                if (_currentEditingExp == null)
                {
                    response = await _api.CreateExperienceAsync(payload);
                }
                else
                {
                    payload.experienceId = _currentEditingExp.experienceId;
                    response = await _api.UpdateExperienceAsync(_currentEditingExp.experienceId, payload);
                }

                if (response.IsSuccessStatusCode)
                {
                    ExperienceFormOverlay.IsVisible = false;
                    _currentEditingExp = null;
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to save experience detail.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                SaveExpBtn.IsEnabled = true;
                SaveExpBtn.Text = "Save Experience";
            }
        }

        private async Task OnDeleteExperienceClicked(int id)
        {
            bool confirm = await ShowConfirmAsync("Delete Experience", "Are you sure you want to remove this experience detail?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var response = await _api.DeleteExperienceAsync(id);
                if (response.IsSuccessStatusCode)
                {
                    await LoadProfileData();
                }
                else
                {
                    await ShowAlertAsync("Error", "Failed to delete experience detail.", "OK");
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Connection error: {ex.Message}", "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await ShowConfirmAsync("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (answer)
            {
                await _api.LogoutAsync();
                await Shell.Current.GoToAsync("//LoginPage", false);
            }
        }
    }
}
