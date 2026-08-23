using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Configuration;
using SwitchBotController.Core.Models;

namespace SwitchBotController.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly SwitchBotConfigLoader _configLoader;
    private readonly ISwitchBotClient _switchBotClient;
    private string _configPath;
    private string _apiToken = string.Empty;
    private bool _isInitialized;

    [ObservableProperty]
    public partial bool HasDevices { get; set; }

    [ObservableProperty]
    public partial bool IsErrorStatus { get; set; }

    [ObservableProperty]
    public partial bool IsInfoStatus { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsInteractionEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "設定を読み込んでいます…";

    [ObservableProperty]
    public partial string DeviceSummary { get; set; } = "デバイスを読み込んでいます";

    [ObservableProperty]
    public partial string ConfigLocation { get; set; }

    public MainPageViewModel(
        SwitchBotConfigLoader configLoader,
        ISwitchBotClient switchBotClient,
        string configPath)
    {
        _configLoader = configLoader;
        _switchBotClient = switchBotClient;
        _configPath = configPath;
        ConfigLocation = configPath;
    }

    public ObservableCollection<DeviceControlViewModel> Devices { get; } = [];

    public Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        _isInitialized = true;
        return ReloadAsync();
    }

    public async Task<bool> ChangeConfigurationAsync(string configPath)
    {
        IsLoading = true;
        SetStatus("選択した設定を確認しています…");

        try
        {
            var normalizedPath = Path.GetFullPath(configPath);
            var configuration = await _configLoader.LoadAsync(normalizedPath);

            _configPath = normalizedPath;
            ConfigLocation = normalizedPath;
            ApplyConfiguration(configuration);
            SetStatus($"{Devices.Count} 台のデバイスを読み込みました");
            return true;
        }
        catch (Exception exception)
        {
            SetStatus(
                $"選択した設定は使用できません: {exception.Message}",
                isError: true);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ReportSettingsError(string message) => SetStatus(message, isError: true);

    partial void OnIsLoadingChanged(bool value) => IsSettingsInteractionEnabled = !value;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ReloadAsync()
    {
        IsLoading = true;
        SetStatus("設定を読み込んでいます…");

        try
        {
            var configuration = await _configLoader.LoadAsync(_configPath);
            ApplyConfiguration(configuration);
            SetStatus($"{Devices.Count} 台のデバイスを読み込みました");
        }
        catch (Exception exception)
        {
            _apiToken = string.Empty;
            Devices.Clear();
            HasDevices = false;
            DeviceSummary = "デバイスを読み込めませんでした";
            SetStatus($"設定エラー: {exception.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyConfiguration(SwitchBotConfiguration configuration)
    {
        _apiToken = configuration.ApiToken;

        Devices.Clear();
        foreach (var device in configuration.Devices)
        {
            Devices.Add(new DeviceControlViewModel(device, SendCommandAsync));
        }

        HasDevices = Devices.Count > 0;
        DeviceSummary = $"{Devices.Count} 台のデバイス";
    }

    private async Task SendCommandAsync(
        DeviceControlViewModel device,
        SwitchBotCommand command)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            SetStatus("APIトークンが読み込まれていません", isError: true);
            return;
        }

        var operation = command == SwitchBotCommand.TurnOn ? "ON" : "OFF";
        device.IsBusy = true;
        device.StatusText = $"{operation} を送信中…";
        SetStatus($"{device.Name} に {operation} を送信しています…");

        try
        {
            var result = await _switchBotClient.SendCommandAsync(
                _apiToken,
                device.DeviceId,
                command);

            if (result.IsSuccess)
            {
                device.StatusText = $"{operation} 完了 · HTTP {(int)result.StatusCode}";
                SetStatus($"{device.Name} を {operation} にしました");
            }
            else
            {
                device.StatusText = $"送信失敗 · HTTP {(int)result.StatusCode}";
                SetStatus(
                    $"{device.Name} の操作に失敗しました（HTTP {(int)result.StatusCode}）",
                    isError: true);
            }
        }
        catch (Exception exception)
        {
            device.StatusText = "通信エラー";
            SetStatus($"{device.Name} の通信エラー: {exception.Message}", isError: true);
        }
        finally
        {
            device.IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsErrorStatus = isError;
        IsInfoStatus = !isError;
    }
}
