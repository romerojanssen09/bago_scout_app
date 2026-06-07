using Microsoft.Maui.Controls;
using System;

namespace BagoScoutApp.Pages.Components
{
    public partial class CustomSwitch : ContentView
    {
        public static readonly BindableProperty IsToggledProperty =
            BindableProperty.Create(
                nameof(IsToggled),
                typeof(bool),
                typeof(CustomSwitch),
                default(bool),
                BindingMode.TwoWay,
                propertyChanged: OnIsToggledChanged);

        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        public event EventHandler<ToggledEventArgs>? Toggled;

        public CustomSwitch()
        {
            InitializeComponent();
        }

        private static void OnIsToggledChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomSwitch customSwitch)
            {
                customSwitch.AnimateSwitch((bool)newValue);
                customSwitch.Toggled?.Invoke(customSwitch, new ToggledEventArgs((bool)newValue));
            }
        }

        private void OnTapped(object sender, EventArgs e)
        {
            IsToggled = !IsToggled;
        }

        private void AnimateSwitch(bool isToggled)
        {
            // Calculate target translation. 
            // Width of track is 50, margin is 2, thumb is 22. Max travel is 50 - 22 - 4 = 24.
            double targetX = isToggled ? 24 : 0;
            Color targetBgColor = isToggled ? Color.FromArgb("#6C63FF") : Color.FromArgb("#D1D5DB");

            // Animation
            var thumbAnim = new Animation(v => ThumbBorder.TranslationX = v, ThumbBorder.TranslationX, targetX, Easing.CubicOut);
            thumbAnim.Commit(this, "ThumbMove", 16, 250);
            
            TrackBorder.BackgroundColor = targetBgColor;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Align visually to current state
            ThumbBorder.TranslationX = IsToggled ? 24 : 0;
            TrackBorder.BackgroundColor = IsToggled ? Color.FromArgb("#6C63FF") : Color.FromArgb("#D1D5DB");
        }
    }
}
