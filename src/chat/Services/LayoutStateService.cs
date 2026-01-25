namespace src.Services;

/// <summary>
/// Service to manage layout state that needs to be shared across render mode boundaries.
/// Scoped per-circuit for Blazor Server.
/// </summary>
public interface ILayoutStateService
{
    bool DrawerOpen { get; set; }
    event Action? OnChange;
    void ToggleDrawer();
}

public class LayoutStateService : ILayoutStateService
{
    private bool _drawerOpen = true;

    public bool DrawerOpen
    {
        get => _drawerOpen;
        set
        {
            if (_drawerOpen != value)
            {
                _drawerOpen = value;
                NotifyStateChanged();
            }
        }
    }

    public event Action? OnChange;

    public void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
