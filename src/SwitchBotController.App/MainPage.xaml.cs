using Microsoft.UI.Xaml.Controls;
using SwitchBotController.App.Configuration;
using SwitchBotController.App.ViewModels;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Configuration;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SwitchBotController.App;

/// <summary>
/// The main content page displayed inside the application window.
/// </summary>
public sealed partial class MainPage : Page
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = new MainPageViewModel(
            new SwitchBotConfigLoader(),
            new SwitchBotClient(HttpClient),
            ConfigurationPathResolver.Resolve());
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }
}
