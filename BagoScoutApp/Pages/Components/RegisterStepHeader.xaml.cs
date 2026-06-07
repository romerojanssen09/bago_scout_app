namespace BagoScoutApp.Pages.Components;

public partial class RegisterStepHeader : ContentView
{
    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(RegisterStepHeader), "Step", propertyChanged: OnTitleTextChanged);

    public static readonly BindableProperty CurrentStepProperty =
        BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(RegisterStepHeader), 1, propertyChanged: OnCurrentStepChanged);

    public static readonly BindableProperty TotalStepsProperty =
        BindableProperty.Create(nameof(TotalSteps), typeof(int), typeof(RegisterStepHeader), 4, propertyChanged: OnTotalStepsChanged);

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public int TotalSteps
    {
        get => (int)GetValue(TotalStepsProperty);
        set => SetValue(TotalStepsProperty, value);
    }

    public event EventHandler BackTapped;

    public RegisterStepHeader()
    {
        InitializeComponent();
    }

    private static void OnTitleTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (RegisterStepHeader)bindable;
        control.StepTitle.Text = newValue as string ?? "Step";
    }

    private static void OnCurrentStepChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (RegisterStepHeader)bindable;
        control.UpdateStepCounter();
    }

    private static void OnTotalStepsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (RegisterStepHeader)bindable;
        control.UpdateStepCounter();
    }

    private void UpdateStepCounter()
    {
        StepCounter.Text = $"{CurrentStep}/{TotalSteps}";
    }

    private void OnBackTapped(object sender, EventArgs e)
    {
        BackTapped?.Invoke(this, EventArgs.Empty);
    }
}
