namespace BagoScoutApp.Models
{
    public record RegisterReq(string FirstName, string LastName, string Email, string Password, string UserType);
    
    public class CheckEmailResp 
    { 
        public bool exists { get; set; } 
    }
    
    public class LoginResp 
    { 
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public string userType { get; set; } = string.Empty;
        public int userId { get; set; }
        public string name { get; set; } = string.Empty;
        public string? token { get; set; }
    }
}
