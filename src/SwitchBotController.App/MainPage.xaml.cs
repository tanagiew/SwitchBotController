using Microsoft.UI.Xaml.Controls;
using SwitchBotController.App.Configuration;
using SwitchBotController.App.ViewModels;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Configuration;
using Windows.Storage.Pickers;

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

    private readonly ConfigurationPathStore _configurationPathStore;
    private readonly string _defaultConfigurationPath;

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        _configurationPathStore = new ConfigurationPathStore(
            ConfigurationPathResolver.SettingsFilePath);
        _defaultConfigurationPath = ConfigurationPathResolver.Resolve();
        var initialConfigurationPath =
            _configurationPathStore.Load() ?? _defaultConfigurationPath;

        ViewModel = new MainPageViewModel(
            new SwitchBotConfigLoader(),
            new SwitchBotClient(HttpClient),
            initialConfigurationPath);
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void OnChooseConfigClicked(
        object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);

        var file = await picker.PickSingleFileAsync();
        if (file is null || !await ViewModel.ChangeConfigurationAsync(file.Path))
        {
            return;
        }

        try
        {
            await _configurationPathStore.SaveAsync(file.Path);
            SettingsButton.Flyout?.Hide();
        }
        catch (Exception exception)
        {
            ViewModel.ReportSettingsError(
                $"設定ファイルの場所を保存できませんでした: {exception.Message}");
        }
    }

    private async void OnResetConfigClicked(
        object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!await ViewModel.ChangeConfigurationAsync(_defaultConfigurationPath))
        {
            return;
        }

        try
        {
            _configurationPathStore.Reset();
            SettingsButton.Flyout?.Hide();
        }
        catch (Exception exception)
        {
            ViewModel.ReportSettingsError(
                $"保存した設定ファイルの場所を解除できませんでした: {exception.Message}");
        }
    }
}
