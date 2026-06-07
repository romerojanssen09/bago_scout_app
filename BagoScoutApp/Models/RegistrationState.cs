namespace BagoScoutApp.Models
{
    public static class RegistrationState
    {
        public static string UserType { get; set; } = "";
        public static string FirstName { get; set; } = "";
        public static string LastName { get; set; } = "";
        public static string Email { get; set; } = "";
        public static string Password { get; set; } = "";
        public static int UserId { get; set; }
        public static string? SelfiePath { get; set; }
        public static string? IdPath { get; set; }
        public static List<string> Skills { get; } = new();
        public static void Reset()
        {
            UserType = FirstName = LastName = Email = Password = "";
            UserId = 0;
            SelfiePath = IdPath = null;
            Skills.Clear();
        }
    }
}
