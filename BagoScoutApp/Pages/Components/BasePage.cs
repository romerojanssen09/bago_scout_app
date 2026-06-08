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
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(24, 20),
            WidthRequest = 320,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow { Brush = Color.FromArgb("#64748B"), Radius = 20, Opacity = 0.15f, Offset = new Point(0, 8) }
        };

        var contentStack = new VerticalStackLayout { Spacing = 18 };
        
        // Loading Section
        var loadingStack = new VerticalStackLayout
        {
            Spacing = 18,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        _loadingIndicator = new ActivityIndicator 
        { 
            IsRunning = true, 
            Color = Color.FromArgb("#6C63FF"), 
            HeightRequest = 50, 
            WidthRequest = 50,
            HorizontalOptions = LayoutOptions.Center
        };
        _loadingLabel = new Label 
        { 
            Text = "Loading...", 
            HorizontalTextAlignment = TextAlignment.Center, 
            FontSize = 16, 
            TextColor = Color.FromArgb("#0F172A"),
            FontFamily = "Inter",
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };
        loadingStack.Children.Add(_loadingIndicator);
        loadingStack.Children.Add(_loadingLabel);
        contentStack.Children.Add(loadingStack);

        // Alert Section
        _alertDialog = new VerticalStackLayout { Spacing = 18, IsVisible = false };
        _dialogTitle = new Label 
        { 
            Text = "Alert", 
            FontSize = 20, 
            FontFamily = "Inter",
            FontAttributes = FontAttributes.Bold, 
            TextColor = Color.FromArgb("#0F172A") 
        };
        _dialogMessage = new Label 
        { 
            Text = "", 
            FontSize = 15, 
            FontFamily = "Inter",
            TextColor = Color.FromArgb("#64748B"),
            LineHeight = 1.5
        };
        
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
            BackgroundColor = Color.FromArgb("#F1F5F9"),
            TextColor = Color.FromArgb("#0F172A"),
            FontFamily = "Inter",
            CornerRadius = 12,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };
        _dialogCancelBtn.Clicked += (s, e) => HandleBtnClicked(false);
        Grid.SetColumn(_dialogCancelBtn, 0);
        
        _dialogConfirmBtn = new Button
        {
            Text = "OK",
            BackgroundColor = Color.FromArgb("#6C63FF"),
            TextColor = Colors.White,
            FontFamily = "Inter",
            CornerRadius = 12,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
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
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(24, 20),
                WidthRequest = 320,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Shadow = new Shadow { Brush = Color.FromArgb("#64748B"), Radius = 20, Opacity = 0.15f, Offset = new Point(0, 8) }
            };

            var contentStack = new VerticalStackLayout { Spacing = 18 };

            // Title
            var titleLabel = new Label
            {
                Text = title,
                FontSize = 20,
                FontFamily = "Inter",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A"),
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
                    BackgroundColor = Color.FromArgb("#F1F5F9"),
                    TextColor = Color.FromArgb("#0F172A"),
                    FontFamily = "Inter",
                    CornerRadius = 12,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 48
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
                Margin = new Thickness(0, 6, 0, 6)
            };
            contentStack.Children.Add(separator);

            // Cancel Button
            var cancelBtn = new Button
            {
                Text = cancelText,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Color.FromArgb("#64748B"),
                FontFamily = "Inter",
                CornerRadius = 12,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48
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

    public Task<string> ShowSelectOptionAsync(string title, string cancelText, string[] options, int selectedIndex = -1)
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

            var selectOverlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#CC111827"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var dialogContainer = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(24, 20),
                WidthRequest = 340,
                MaximumHeightRequest = 700,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Shadow = new Shadow { Brush = Color.FromArgb("#64748B"), Radius = 20, Opacity = 0.15f, Offset = new Point(0, 8) }
            };

            var contentStack = new VerticalStackLayout { Spacing = 18 };

            // Title
            var titleLabel = new Label
            {
                Text = title,
                FontSize = 20,
                FontFamily = "Inter",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            contentStack.Children.Add(titleLabel);

            // ScrollView for options
            var scrollView = new ScrollView
            {
                MaximumHeightRequest = 480
            };

            var optionsStack = new VerticalStackLayout { Spacing = 8 };
            
            string? currentSelection = selectedIndex >= 0 && selectedIndex < options.Length ? options[selectedIndex] : null;
            var radioButtons = new List<(Border border, Label checkIcon, Label textLabel, string value)>();

            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var isSelected = i == selectedIndex;

                var optionBorder = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    StrokeThickness = 2,
                    Stroke = isSelected ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB"),
                    BackgroundColor = isSelected ? Color.FromArgb("#F5F3FF") : Colors.White,
                    Padding = new Thickness(16, 14),
                    HeightRequest = 52
                };

                var optionGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 12
                };

                var optionLabel = new Label
                {
                    Text = option,
                    FontFamily = "Inter",
                    FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#0F172A"),
                    VerticalOptions = LayoutOptions.Center
                };

                var checkIcon = new Label
                {
                    Text = isSelected ? "\uf00c" : "",
                    FontFamily = "FASolid",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#6C63FF"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End
                };

                Grid.SetColumn(optionLabel, 0);
                Grid.SetColumn(checkIcon, 1);
                optionGrid.Children.Add(optionLabel);
                optionGrid.Children.Add(checkIcon);

                optionBorder.Content = optionGrid;

                radioButtons.Add((optionBorder, checkIcon, optionLabel, option));

                // Tap gesture for selection
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    // Update all radio buttons
                    foreach (var (border, icon, label, val) in radioButtons)
                    {
                        var isNowSelected = val == option;
                        border.Stroke = isNowSelected ? Color.FromArgb("#6C63FF") : Color.FromArgb("#E5E7EB");
                        border.BackgroundColor = isNowSelected ? Color.FromArgb("#F5F3FF") : Colors.White;
                        icon.Text = isNowSelected ? "\uf00c" : "";
                        label.FontAttributes = isNowSelected ? FontAttributes.Bold : FontAttributes.None;
                        icon.TextColor = isNowSelected ? Color.FromArgb("#6C63FF") : Color.FromArgb("#CBD5E1");
                        label.FontAttributes = isNowSelected ? FontAttributes.Bold : FontAttributes.None;
                    }
                    currentSelection = option;
                };
                optionBorder.GestureRecognizers.Add(tapGesture);

                optionsStack.Children.Add(optionBorder);
            }

            scrollView.Content = optionsStack;
            contentStack.Children.Add(scrollView);

            // Buttons Grid
            var buttonsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12
            };

            // Cancel Button
            var cancelBtn = new Button
            {
                Text = cancelText,
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                TextColor = Color.FromArgb("#0F172A"),
                FontFamily = "Inter",
                CornerRadius = 12,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48
            };
            cancelBtn.Clicked += (s, e) =>
            {
                _rootGrid.Children.Remove(selectOverlay);
                tcs.SetResult(cancelText);
            };
            Grid.SetColumn(cancelBtn, 0);

            // Confirm Button
            var confirmBtn = new Button
            {
                Text = "Confirm",
                BackgroundColor = Color.FromArgb("#6C63FF"),
                TextColor = Colors.White,
                FontFamily = "Inter",
                CornerRadius = 12,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48
            };
            confirmBtn.Clicked += (s, e) =>
            {
                _rootGrid.Children.Remove(selectOverlay);
                tcs.SetResult(currentSelection ?? cancelText);
            };
            Grid.SetColumn(confirmBtn, 1);

            buttonsGrid.Children.Add(cancelBtn);
            buttonsGrid.Children.Add(confirmBtn);
            contentStack.Children.Add(buttonsGrid);

            dialogContainer.Content = contentStack;
            selectOverlay.Children.Add(dialogContainer);
            _rootGrid.Children.Add(selectOverlay);
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
