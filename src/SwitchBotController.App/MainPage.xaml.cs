using System.Diagnostics;
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

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        _configurationPathStore = new ConfigurationPathStore(
            ConfigurationPathResolver.SettingsFilePath);
        var initialConfigurationPath =
            _configurationPathStore.Load() ?? ConfigurationPathResolver.Resolve();

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

    private void OnOpenConfigClicked(
        object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(ViewModel.ConfigLocation))
            {
                ViewModel.ReportSettingsError(
                    $"設定ファイルが見つかりません: {ViewModel.ConfigLocation}");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = ViewModel.ConfigLocation,
                UseShellExecute = true
            });
            SettingsButton.Flyout?.Hide();
        }
        catch (Exception exception)
        {
            ViewModel.ReportSettingsError(
                $"設定ファイルを開けませんでした: {exception.Message}");
        }
    }

}
