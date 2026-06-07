using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BagoScoutApp.Pages.Components;

public static class ComponentStatic
{
    private static bool _menuOverlay = false;
    
    public static event EventHandler<bool> MenuOverlayChanged;
    public static event EventHandler ScrollToTop;
    public static event EventHandler ScrollToAbout;
    public static event EventHandler ScrollToContact;

    public static bool MenuOverlay
    {
        get => _menuOverlay;
        set
        {
            if (_menuOverlay != value)
            {
                _menuOverlay = value;
                MenuOverlayChanged?.Invoke(null, value);
            }
        }
    }

    public static void TriggerScrollToTop()
    {
        ScrollToTop?.Invoke(null, EventArgs.Empty);
    }

    public static void TriggerScrollToAbout()
    {
        ScrollToAbout?.Invoke(null, EventArgs.Empty);
    }

    public static void TriggerScrollToContact()
    {
        ScrollToContact?.Invoke(null, EventArgs.Empty);
    }
}
