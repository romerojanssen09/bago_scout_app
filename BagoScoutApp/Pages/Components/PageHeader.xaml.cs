namespace BagoScoutApp.Pages.Components;

public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(PageHeader), string.Empty, propertyChanged: OnTitleChanged);

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(PageHeader), string.Empty, propertyChanged: OnDescriptionChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public PageHeader()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (PageHeader)bindable;
        control.TitleLabel.Text = newValue as string;
    }

    private static void OnDescriptionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (PageHeader)bindable;
        control.DescriptionLabel.Text = newValue as string;
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", false);
    }
}
