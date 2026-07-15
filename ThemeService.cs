namespace CEMETRIX.Web.Services;

public class ThemeService
{
    public string Theme { get; private set; } = "light";

    public event Action? OnChange;

    public void SetTheme(string theme)
    {
        Theme = theme;
        OnChange?.Invoke();
    }
}
