using BagoScoutApp.Models;
using System.Net.Http.Json;

namespace BagoScoutApp.Services
{
    public class ApiClient
    {
        static readonly System.Net.CookieContainer Cookies = new();
        static readonly HttpClientHandler Handler = CreateHandler();
        static readonly HttpClient SharedHttp = new(Handler) { BaseAddress = new Uri(GetBaseUrl()) };
        readonly HttpClient _http;
        public string BaseUrl { get; }

        static HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler 
            { 
                UseCookies = true, 
                CookieContainer = Cookies 
            };

#if DEBUG && ANDROID
            // For development only: bypass SSL certificate validation
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

            return handler;
        }

        static string GetBaseUrl()
        {
#if DEBUG
            // Use HTTP on port 5180 for local development to avoid self-signed HTTPS certificate errors
            return "http://192.168.1.10:5180";
#else
            // Use HTTPS on port 7030 as backend has HTTPS redirection enabled
            return ConfigurationService.Instance.ApiBaseUrl ?? "http://192.168.1.10:5180";

#endif
        }

        public ApiClient(string? baseUrl = null)
        {
            BaseUrl = baseUrl ?? SharedHttp.BaseAddress!.ToString().TrimEnd('/');
            if (!string.Equals(SharedHttp.BaseAddress!.ToString().TrimEnd('/'), BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                SharedHttp.BaseAddress = new Uri(BaseUrl);
            }
            _http = SharedHttp;
        }

        private async Task AddAuthHeader()
        {
            var token = await SecureStorage.GetAsync("AuthToken");
            _http.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        // --- AUTH ENDPOINTS ---

        public Task<CheckEmailResp?> CheckEmailAsync(string email) =>
            _http.GetFromJsonAsync<CheckEmailResp>($"/api/auth/check-email/{Uri.EscapeDataString(email)}");

        public Task<HttpResponseMessage> SendVerificationCodeAsync(string email, string name) =>
            _http.PostAsJsonAsync("/api/auth/send-verification-code", new { Email = email, Name = name });

        public Task<HttpResponseMessage> VerifyEmailAsync(string email, string code) =>
            _http.PostAsJsonAsync("/api/auth/verify-email", new { Email = email, Code = code });

        public Task<HttpResponseMessage> RegisterAsync(RegisterReq req) =>
            _http.PostAsJsonAsync("/api/auth/register", req);

        public Task<HttpResponseMessage> SendForgotPasswordOtpAsync(string email) =>
            _http.PostAsJsonAsync("/api/auth/forgot-password/request", new { Email = email });

        public Task<HttpResponseMessage> VerifyForgotPasswordOtpAsync(string email, string code) =>
            _http.PostAsJsonAsync("/api/auth/forgot-password/verify", new { Email = email, Code = code });

        public Task<HttpResponseMessage> ResetPasswordAsync(string email, string code, string password) =>
            _http.PostAsJsonAsync("/api/auth/forgot-password/reset", new { Email = email, Code = code, Password = password });

        public async Task<LoginResp?> LoginAsync(string email, string password, bool remember)
        {
            var fcmToken = Preferences.Get("FcmToken", "");
            var response = await _http.PostAsJsonAsync("/api/mobile/login", new { Email = email, Password = password, FcmToken = fcmToken });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResp>();
                if (result != null && result.success && !string.IsNullOrEmpty(result.token))
                {
                    await SecureStorage.SetAsync("AuthToken", result.token);
                    Preferences.Set("UserId", result.userId);
                    Preferences.Set("UserType", result.userType);
                    Preferences.Set("UserName", result.name);
                }
                return result;
            }
            return null;
        }

        public async Task<bool> ValidateTokenAsync()
        {
            var token = await SecureStorage.GetAsync("AuthToken");
            if (string.IsNullOrEmpty(token)) return false;

            var response = await _http.PostAsJsonAsync("/api/mobile/validate-token", new { Token = token });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResp>();
                if (result != null && result.success)
                {
                    Preferences.Set("UserId", result.userId);
                    Preferences.Set("UserType", result.userType);
                    Preferences.Set("UserName", result.name);
                    return true;
                }
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("AuthToken");
                if (!string.IsNullOrEmpty(token))
                {
                    await _http.PostAsJsonAsync("/api/mobile/logout", new { Token = token });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to notify backend of logout: {ex.Message}");
            }
            finally
            {
                SecureStorage.Remove("AuthToken");
                Preferences.Clear();
                AuthStorageService.ClearCredentials();
            }
        }

        public async Task<bool> RegisterFcmAsync(string token, string fcmToken)
        {
            var response = await _http.PostAsJsonAsync("/api/mobile/register-fcm", new { Token = token, FcmToken = fcmToken });
            return response.IsSuccessStatusCode;
        }

        // --- EMAILS ---
        public async Task<bool> SendEmailAsync(EmailRequest req)
        {
            await AddAuthHeader();
            var response = await _http.PostAsJsonAsync("/api/email/send", req);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<EmailHistoryDto>?> GetEmailHistoryAsync(int applicationId)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<EmailHistoryDto>>($"/api/email/application/{applicationId}");
        }

        // --- DASHBOARD ---
        public async Task<SeekerDashboardData?> GetSeekerDashboardAsync()
        {
            var token = await SecureStorage.GetAsync("AuthToken");
            return await _http.GetFromJsonAsync<SeekerDashboardData>($"/api/mobile/dashboard/seeker?token={token}");
        }

        // --- JOBS & APPLICATIONS ---
        public async Task<List<JobDto>?> GetEmployerJobsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<JobDto>>("/api/jobs/employer");
        }

        public async Task<List<JobDto>?> GetAllJobsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<JobDto>>("/api/jobs/all");
        }

        public async Task<JobDto?> GetJobByIdAsync(int id)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<JobDto>($"/api/jobs/{id}");
        }

        public async Task<HttpResponseMessage> CreateJobAsync(object job)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/jobs/create", job);
        }

        public async Task<HttpResponseMessage> UpdateJobAsync(int id, object job)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync($"/api/jobs/update/{id}", job);
        }

        public async Task<HttpResponseMessage> DeleteJobAsync(int id)
        {
            await AddAuthHeader();
            return await _http.DeleteAsync($"/api/jobs/delete/{id}");
        }

        public async Task<HttpResponseMessage> ApplyToJobAsync(int jobId, string coverLetter)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/jobs/apply", new { JobId = jobId, CoverLetter = coverLetter });
        }

        public async Task<List<ApplicationDto>?> GetJobApplicationsAsync(int jobId)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<ApplicationDto>>($"/api/jobs/{jobId}/applications");
        }

        public async Task<List<ApplicationDto>?> GetAllEmployerApplicationsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<ApplicationDto>>("/api/applications/employer");
        }

        public async Task<List<ApplicationDto>?> GetSeekerApplicationsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<ApplicationDto>>("/api/applications/seeker");
        }

        public async Task<HttpResponseMessage> UpdateApplicationStatusAsync(int id, string status)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync($"/api/applications/{id}/status", new { Status = status });
        }

        // --- MESSAGES ---
        public async Task<List<ConversationDto>?> GetConversationsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<ConversationDto>>("/api/messages/conversations");
        }

        public async Task<List<MessageDto>?> GetMessagesAsync(int otherUserId)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<MessageDto>>($"/api/messages/{otherUserId}");
        }

        public async Task<HttpResponseMessage> SendMessageAsync(int receiverId, string text)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/messages/send", new { ReceiverId = receiverId, MessageText = text });
        }

        // --- PROFILE ---
        public async Task<SeekerProfileDto?> GetSeekerProfileAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<SeekerProfileDto>("/api/profile/seeker");
        }

        public async Task<SeekerProfileDto?> GetSeekerProfileByIdAsync(int seekerId)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<SeekerProfileDto>($"/api/profile/seeker/{seekerId}");
        }

        public async Task<HttpResponseMessage> UpdateSeekerProfileAsync(object profile)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync("/api/profile/update", profile);
        }

        public async Task<HttpResponseMessage> UpdateSeekerSkillsAsync(List<int> skillIds)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync("/api/profile/skills", new { SkillIds = skillIds });
        }

        // --- EDUCATION ---
        public async Task<HttpResponseMessage> CreateEducationAsync(EducationDto edu)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/education/create", edu);
        }

        public async Task<HttpResponseMessage> UpdateEducationAsync(int id, EducationDto edu)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync($"/api/education/update/{id}", edu);
        }

        public async Task<HttpResponseMessage> DeleteEducationAsync(int id)
        {
            await AddAuthHeader();
            return await _http.DeleteAsync($"/api/education/delete/{id}");
        }

        // --- EXPERIENCE ---
        public async Task<HttpResponseMessage> CreateExperienceAsync(ExperienceDto exp)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/experience/create", exp);
        }

        public async Task<HttpResponseMessage> UpdateExperienceAsync(int id, ExperienceDto exp)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync($"/api/experience/update/{id}", exp);
        }

        public async Task<HttpResponseMessage> DeleteExperienceAsync(int id)
        {
            await AddAuthHeader();
            return await _http.DeleteAsync($"/api/experience/delete/{id}");
        }

        public async Task<List<SkillDto>?> GetAllSkillsAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<List<SkillDto>>("/api/skills/all");
        }

        public async Task<HttpResponseMessage> CreateCustomSkillAsync(string skillName)
        {
            await AddAuthHeader();
            return await _http.PostAsJsonAsync("/api/skills/create", new { SkillName = skillName });
        }

        public async Task<HttpResponseMessage> UploadProfilePhotoAsync(string photoPath)
        {
            await AddAuthHeader();
            using var form = new MultipartFormDataContent();
            if (File.Exists(photoPath))
            {
                var stream = File.OpenRead(photoPath);
                form.Add(new StreamContent(stream), "photo", Path.GetFileName(photoPath));
            }
            return await _http.PostAsync("/api/profile/upload-photo", form);
        }

        public async Task<CompanyProfileDto?> GetCompanyProfileAsync()
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<CompanyProfileDto>("/api/company/profile");
        }

        public async Task<CompanyProfileDto?> GetCompanyProfileByIdAsync(int employerId)
        {
            await AddAuthHeader();
            return await _http.GetFromJsonAsync<CompanyProfileDto>($"/api/company/profile/{employerId}");
        }

        public async Task<HttpResponseMessage> UpdateCompanyProfileAsync(object profile)
        {
            await AddAuthHeader();
            return await _http.PutAsJsonAsync("/api/company/update", profile);
        }

        public async Task<HttpResponseMessage> UploadCompanyLogoAsync(string logoPath)
        {
            await AddAuthHeader();
            using var form = new MultipartFormDataContent();
            if (File.Exists(logoPath))
            {
                var stream = File.OpenRead(logoPath);
                form.Add(new StreamContent(stream), "logo", Path.GetFileName(logoPath));
            }
            return await _http.PostAsync("/api/company/upload-logo", form);
        }

        // --- COMMON ---
        public async Task<HttpResponseMessage> UploadPhotosAsync(int userId, string? selfiePath, string? idPath)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(userId.ToString()), "UserId");
            if (!string.IsNullOrEmpty(selfiePath) && File.Exists(selfiePath))
            {
                var stream = File.OpenRead(selfiePath);
                form.Add(new StreamContent(stream), "SelfiePhoto", Path.GetFileName(selfiePath));
            }
            if (!string.IsNullOrEmpty(idPath) && File.Exists(idPath))
            {
                var stream = File.OpenRead(idPath);
                form.Add(new StreamContent(stream), "IdPhoto", Path.GetFileName(idPath));
            }
            return await _http.PostAsync("/api/auth/upload-photos", form);
        }

        public Task<HttpResponseMessage> SaveSkillsAsync(int userId, IEnumerable<string> skills) =>
            _http.PostAsJsonAsync("/api/auth/save-skills", new { UserId = userId, Skills = skills });
    }

    public class SeekerDashboardData
    {
        public int applicationsCount { get; set; }
        public List<JobDto>? recentJobs { get; set; }
    }

    public class JobDto
    {
        public int jobId { get; set; }
        public string title { get; set; } = "";
        public string description { get; set; } = "";
        public string company { get; set; } = "";
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public string address { get; set; } = "";
        public string location { get; set; } = "";
        public string salaryRange { get; set; } = "";
        public string jobType { get; set; } = "";
        public string experienceLevel { get; set; } = "";
        public string requirements { get; set; } = "";
        public bool isActive { get; set; }
        public DateTime createdAt { get; set; }
        
        public string distanceDisplay { get; set; } = "";
        public bool hasDistance => !string.IsNullOrEmpty(distanceDisplay);
    }

    public class ApplicationDto
    {
        public int applicationId { get; set; }
        public int jobId { get; set; }
        public string jobTitle { get; set; } = "";
        public string company { get; set; } = "";
        public string jobLocation { get; set; } = "";
        public int seekerId { get; set; }
        public string seekerName { get; set; } = "";
        public string seekerEmail { get; set; } = "";
        public string seekerPhone { get; set; } = "";
        public string seekerPhoto { get; set; } = "";
        public string coverLetter { get; set; } = "";
        public string status { get; set; } = "Pending";
        public DateTime appliedAt { get; set; }
        public int employerId { get; set; }
        public bool hasEmails { get; set; }
    }

    public class ConversationDto
    {
        public int userId { get; set; }
        public string firstName { get; set; } = "";
        public string lastName { get; set; } = "";
        public string selfiePhotoPath { get; set; } = "";
        public string selfiePhotoUrl { get; set; } = "";
        public string userType { get; set; } = "";
        public string lastMessage { get; set; } = "";
        public DateTime lastMessageTime { get; set; }
        public int unreadCount { get; set; }
        
        public string fullName => $"{firstName} {lastName}".Trim();
    }

    public class MessageDto
    {
        public int messageId { get; set; }
        public int senderId { get; set; }
        public int receiverId { get; set; }
        public string messageText { get; set; } = "";
        public DateTime sentAt { get; set; }
        public bool isRead { get; set; }
    }

    public class SeekerProfileDto
    {
        public int userId { get; set; }
        public string firstName { get; set; } = "";
        public string lastName { get; set; } = "";
        public string email { get; set; } = "";
        public string phoneNumber { get; set; } = "";
        public string selfiePhotoPath { get; set; } = "";
        public List<string> skills { get; set; } = new();
        public List<EducationDto> education { get; set; } = new();
        public List<ExperienceDto> experience { get; set; } = new();
    }

    public class EducationDto
    {
        public int educationId { get; set; }
        public int userId { get; set; }
        public string school { get; set; } = "";
        public string degree { get; set; } = "";
        public string fieldOfStudy { get; set; } = "";
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? description { get; set; }
    }

    public class ExperienceDto
    {
        public int experienceId { get; set; }
        public int userId { get; set; }
        public string jobTitle { get; set; } = "";
        public string company { get; set; } = "";
        public string location { get; set; } = "";
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public bool isCurrentJob { get; set; }
        public string? description { get; set; }
    }

    public class SkillDto
    {
        public int skillId { get; set; }
        public string skillName { get; set; } = "";
    }

    public class CompanyProfileDto
    {
        public int userId { get; set; }
        public string firstName { get; set; } = "";
        public string lastName { get; set; } = "";
        public string email { get; set; } = "";
        public string phoneNumber { get; set; } = "";
        public string companyName { get; set; } = "";
        public string companyDescription { get; set; } = "";
        public string companyWebsite { get; set; } = "";
        public string companyAddress { get; set; } = "";
        public double? companyLatitude { get; set; }
        public double? companyLongitude { get; set; }
        public string companyIndustry { get; set; } = "";
        public string companySize { get; set; } = "";
        public string companyLogoPath { get; set; } = "";
    }

    public class EmailRequest
    {
        public int applicationId { get; set; }
        public int receiverId { get; set; }
        public string to { get; set; } = string.Empty;
        public string subject { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }

    public class EmailHistoryDto
    {
        public int emailId { get; set; }
        public int applicationId { get; set; }
        public int senderId { get; set; }
        public int receiverId { get; set; }
        public string subject { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public DateTime sentAt { get; set; }
        public bool isRead { get; set; }
    }
}
