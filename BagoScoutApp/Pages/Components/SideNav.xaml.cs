using BagoScoutApp.Pages.Register;

namespace BagoScoutApp.Pages.Components;

public partial class SideNav : ContentView
{
	public SideNav()
	{
		InitializeComponent();
		
		// Subscribe to menu overlay changes
		ComponentStatic.MenuOverlayChanged += OnMenuOverlayChanged;
	}

	private void OnMenuOverlayChanged(object sender, bool isVisible)
	{
		MenuOverlay.IsVisible = isVisible;
	}

	private void OnCloseMenuClicked(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
	}

	private void OnOverlayTapped(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
	}

	private void OnMenuContentTapped(object sender, EventArgs e)
	{
		// Prevent closing when tapping menu content
	}

	private async void OnHomeMenuTapped(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
		await Task.Delay(300); // Wait for menu to close
		ComponentStatic.TriggerScrollToTop();
	}

	private async void OnAboutMenuTapped(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
		await Task.Delay(300); // Wait for menu to close
		ComponentStatic.TriggerScrollToAbout();
	}

	private async void OnContactMenuTapped(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
		await Task.Delay(300); // Wait for menu to close
		ComponentStatic.TriggerScrollToContact();
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
		await Shell.Current.GoToAsync("//LoginPage", false);
	}

	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = false;
		await Shell.Current.GoToAsync(nameof(RegisterTypePage), false);
	}
}
