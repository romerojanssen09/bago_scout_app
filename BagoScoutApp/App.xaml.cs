namespace BagoScoutApp
{
    public partial class App : Application
    {
        public static string? PendingNavigationRoute { get; set; }

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
