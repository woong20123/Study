namespace StockTarget.Mobile;

public partial class App : Application
{
    public App()
    {
        Localization.L.ApplySavedLanguage();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
