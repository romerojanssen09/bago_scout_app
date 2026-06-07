namespace BagoScoutApp.Pages.Components;

public partial class Header : ContentView
{
	public Header()
	{
		InitializeComponent();
	}

	private void OnMenuClicked(object sender, EventArgs e)
	{
		ComponentStatic.MenuOverlay = !ComponentStatic.MenuOverlay;
	}
}
