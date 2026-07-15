namespace CEMETRIX.Web.Services;



/// <summary>

/// Cross-component state for layout, notifications, and list refresh after mutations.

/// </summary>

public class AppStateService

{

    /// <summary>Desktop: narrow icon-only sidebar. Mobile: drawer open when true.</summary>

    public bool SidebarCollapsed { get; private set; }



    public event Action? DataChanged;

    public event Action? LayoutChanged;



    public void ToggleSidebar()

    {

        SidebarCollapsed = !SidebarCollapsed;

        LayoutChanged?.Invoke();

    }



    public void SetSidebarCollapsed(bool collapsed)

    {

        if (SidebarCollapsed == collapsed) return;

        SidebarCollapsed = collapsed;

        LayoutChanged?.Invoke();

    }



    /// <summary>Close mobile drawer after navigation (mobile uses collapsed=true as open).</summary>

    public void CloseMobileDrawer()

    {

        if (!SidebarCollapsed) return;

        SidebarCollapsed = false;

        LayoutChanged?.Invoke();

    }



    public void NotifyDataChanged() => DataChanged?.Invoke();

}

