using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace BagoScoutApp.Pages.Components;

public class BasePage : ContentPage
{
    public ICommand BackCommand { get; }
    public ICommand ClearBackStackCommand { get; }

    private Grid? _rootGrid;
    private Grid? _overlayGrid;
    private ActivityIndicator? _loadingIndicator;
    private Label? _loadingLabel;
    
    private VerticalStackLayout? _alertDialog;
    private Label? _dialogTitle;
    private Label? _dialogMessage;
    private Button? _dialogConfirmBtn;
    private Button? _dialogCancelBtn;
    
    private TaskCompletionSource<bool>? _alertTcs;

    public BasePage()
    {
        Shell.SetPresentationMode(this, PresentationMode.NotAnimated);

        // Go back one page (no animation)
        BackCommand = new Command(async () =>
        {
            await Shell.Current.GoToAsync("..", false);
        });

        // Clear all previous pages and return to root
        ClearBackStackCommand = new Command(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PopToRootAsync(animated: false);
            });
        });

        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            Command = BackCommand
        });
    }

    private void EnsureOverlayInitialized()
    {
        if (_rootGrid != null) return;

        var originalContent = this.Content;
        if (originalContent == null) return;

        // Create root Grid
        _rootGrid = new Grid();
        
        // Replace page's content with our root Grid
        this.Content = _rootGrid;
        _rootGrid.Children.Add(originalContent);

        // Create the semi-transparent black overlay Grid (hidden by default)
        _overlayGrid = new Grid
        {
            BackgroundColor = Color.FromArgb("#CC111827"), // Dark charcoal with high opacity (80%) for premium look
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        // Center container
        var dialogContainer = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(24),
            WidthRequest = 320,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow { Brush = Brush.Black, Radius = 15, Opacity = 0.2f }
        };

        var contentStack = new VerticalStackLayout { Spacing = 16 };
        
        // Loading Section
        var loadingStack = new VerticalStackLayout
        {
            Spacing = 16,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        _loadingIndicator = new ActivityIndicator 
        { 
            IsRunning = true, 
            Color = Color.FromArgb("#6C63FF"), 
            HeightRequest = 45, 
            WidthRequest = 45,
            HorizontalOptions = LayoutOptions.Center
        };
        _loadingLabel = new Label 
        { 
            Text = "Loading...", 
            HorizontalTextAlignment = TextAlignment.Center, 
            FontSize = 15, 
            TextColor = Color.FromArgb("#1C2B53"),
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };
        loadingStack.Children.Add(_loadingIndicator);
        loadingStack.Children.Add(_loadingLabel);
        contentStack.Children.Add(loadingStack);

        // Alert Section
        _alertDialog = new VerticalStackLayout { Spacing = 16, IsVisible = false };
        _dialogTitle = new Label { Text = "Alert", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1C2B53") };
        _dialogMessage = new Label { Text = "", FontSize = 14, TextColor = Color.FromArgb("#8D94A8") };
        
        var buttonsGrid = new Grid 
        { 
            ColumnDefinitions = new ColumnDefinitionCollection 
            { 
                new ColumnDefinition { Width = GridLength.Star }, 
                new ColumnDefinition { Width = GridLength.Star } 
            }, 
            ColumnSpacing = 12 
        };
        
        _dialogCancelBtn = new Button
        {
            Text = "Cancel",
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#1C2B53"),
            CornerRadius = 8,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 40
        };
        _dialogCancelBtn.Clicked += (s, e) => HandleBtnClicked(false);
        Grid.SetColumn(_dialogCancelBtn, 0);
        
        _dialogConfirmBtn = new Button
        {
            Text = "OK",
            BackgroundColor = Color.FromArgb("#6C63FF"),
            TextColor = Colors.White,
            CornerRadius = 8,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 40
        };
        _dialogConfirmBtn.Clicked += (s, e) => HandleBtnClicked(true);
        Grid.SetColumn(_dialogConfirmBtn, 1);
        
        buttonsGrid.Children.Add(_dialogCancelBtn);
        buttonsGrid.Children.Add(_dialogConfirmBtn);
        
        _alertDialog.Children.Add(_dialogTitle);
        _alertDialog.Children.Add(_dialogMessage);
        _alertDialog.Children.Add(buttonsGrid);
        contentStack.Children.Add(_alertDialog);

        dialogContainer.Content = contentStack;
        _overlayGrid.Children.Add(dialogContainer);
        _rootGrid.Children.Add(_overlayGrid);
    }

    private void HandleBtnClicked(bool confirm)
    {
        if (_alertTcs != null)
        {
            var tcs = _alertTcs;
            _alertTcs = null;
            
            // Hide overlay
            if (_overlayGrid != null) _overlayGrid.IsVisible = false;
            
            tcs.SetResult(confirm);
        }
    }

    public void ShowLoading(string message = "Loading...")
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EnsureOverlayInitialized();
            if (_overlayGrid == null || _loadingLabel == null || _alertDialog == null) return;
            
            // Find and hide alert elements, show loading
            _alertDialog.IsVisible = false;
            
            var loadingStack = _loadingIndicator?.Parent as VisualElement;
            if (loadingStack != null) loadingStack.IsVisible = true;
            
            _loadingLabel.Text = message;
            _overlayGrid.IsVisible = true;
        });
    }

    public void HideLoading()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_overlayGrid == null) return;
            
            // Only hide if loading is currently displayed (not alert/confirm)
            var loadingStack = _loadingIndicator?.Parent as VisualElement;
            if (loadingStack != null && loadingStack.IsVisible)
            {
                _overlayGrid.IsVisible = false;
            }
        });
    }

    public Task ShowAlertAsync(string title, string message, string buttonText = "OK")
    {
        var tcs = new TaskCompletionSource<bool>();
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EnsureOverlayInitialized();
            if (_overlayGrid == null || _alertDialog == null || _dialogTitle == null || _dialogMessage == null || _dialogConfirmBtn == null || _dialogCancelBtn == null)
            {
                tcs.SetResult(false);
                return;
            }
            
            _alertTcs = tcs;
            
            // Hide loading, show alert
            var loadingStack = _loadingIndicator?.Parent as VisualElement;
            if (loadingStack != null) loadingStack.IsVisible = false;
            
            _alertDialog.IsVisible = true;
            _dialogTitle.Text = title;
            _dialogMessage.Text = message;
            
            // Show only Confirm button
            _dialogCancelBtn.IsVisible = false;
            Grid.SetColumnSpan(_dialogConfirmBtn, 2);
            Grid.SetColumn(_dialogConfirmBtn, 0);
            _dialogConfirmBtn.Text = buttonText;
            
            _overlayGrid.IsVisible = true;
        });
        
        return tcs.Task;
    }

    public Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "No")
    {
        var tcs = new TaskCompletionSource<bool>();
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EnsureOverlayInitialized();
            if (_overlayGrid == null || _alertDialog == null || _dialogTitle == null || _dialogMessage == null || _dialogConfirmBtn == null || _dialogCancelBtn == null)
            {
                tcs.SetResult(false);
                return;
            }
            
            _alertTcs = tcs;
            
            // Hide loading, show alert
            var loadingStack = _loadingIndicator?.Parent as VisualElement;
            if (loadingStack != null) loadingStack.IsVisible = false;
            
            _alertDialog.IsVisible = true;
            _dialogTitle.Text = title;
            _dialogMessage.Text = message;
            
            // Show both buttons
            _dialogCancelBtn.IsVisible = true;
            _dialogCancelBtn.Text = cancelText;
            
            Grid.SetColumnSpan(_dialogConfirmBtn, 1);
            Grid.SetColumn(_dialogConfirmBtn, 1);
            _dialogConfirmBtn.Text = confirmText;
            
            _overlayGrid.IsVisible = true;
        });
        
        return tcs.Task;
    }

    public Task<string> ShowActionSheetAsync(string title, string cancelText, params string[] options)
    {
        var tcs = new TaskCompletionSource<string>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            EnsureOverlayInitialized();
            if (_rootGrid == null)
            {
                tcs.SetResult(cancelText);
                return;
            }

            var actionOverlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#CC111827"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var dialogContainer = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(24),
                WidthRequest = 320,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Shadow = new Shadow { Brush = Brush.Black, Radius = 15, Opacity = 0.2f }
            };

            var contentStack = new VerticalStackLayout { Spacing = 16 };

            // Title
            var titleLabel = new Label
            {
                Text = title,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1C2B53"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            contentStack.Children.Add(titleLabel);

            // Options Stack
            var optionsStack = new VerticalStackLayout { Spacing = 10 };
            foreach (var option in options)
            {
                var btn = new Button
                {
                    Text = option,
                    BackgroundColor = Color.FromArgb("#F3F4F6"),
                    TextColor = Color.FromArgb("#1C2B53"),
                    CornerRadius = 8,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 44
                };
                btn.Clicked += (s, e) =>
                {
                    _rootGrid.Children.Remove(actionOverlay);
                    tcs.SetResult(option);
                };
                optionsStack.Children.Add(btn);
            }
            contentStack.Children.Add(optionsStack);

            // Separator
            var separator = new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#E5E7EB"),
                Margin = new Thickness(0, 4, 0, 4)
            };
            contentStack.Children.Add(separator);

            // Cancel Button
            var cancelBtn = new Button
            {
                Text = cancelText,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Color.FromArgb("#4B5563"),
                CornerRadius = 8,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 44
            };
            cancelBtn.Clicked += (s, e) =>
            {
                _rootGrid.Children.Remove(actionOverlay);
                tcs.SetResult(cancelText);
            };
            contentStack.Children.Add(cancelBtn);

            dialogContainer.Content = contentStack;
            actionOverlay.Children.Add(dialogContainer);
            _rootGrid.Children.Add(actionOverlay);
        });

        return tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..", false);
            });
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
