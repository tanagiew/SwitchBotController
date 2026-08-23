using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Configuration;
using SwitchBotController.Core.Models;

namespace SwitchBotController.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private const int MaxConcurrentStatusRequests = 4;
    private const int StatusStartObservationCount = 3;
    private const int StatusCompletionPollCount = 25;
    private static readonly TimeSpan InitialCommandStatusDelay = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan CommandStatusPollInterval = TimeSpan.FromSeconds(2);

    private readonly SwitchBotConfigLoader _configLoader;
    private readonly ISwitchBotClient _switchBotClient;
    private CancellationTokenSource _configurationCancellation = new();
    private string _configPath;
    private string _apiToken = string.Empty;
    private bool _isInitialized;

    [ObservableProperty]
    public partial bool HasDevices { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsInteractionEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "設定を読み込んでいます…";

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial string DeviceSummary { get; set; } = "デバイス";

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
        var operation = BeginConfigurationOperation();
        var cancellationToken = operation.Token;
        IsLoading = true;
        SetStatus("選択した設定を確認しています…");

        try
        {
            var normalizedPath = Path.GetFullPath(configPath);
            var configuration = await _configLoader.LoadAsync(
                normalizedPath,
                cancellationToken);

            _configPath = normalizedPath;
            ConfigLocation = normalizedPath;
            var statusFailureCount = await ApplyConfigurationAsync(
                configuration,
                cancellationToken);
            SetLoadedStatus(statusFailureCount);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            SetStatus(
                $"選択した設定は使用できません: {exception.Message}",
                InfoBarSeverity.Error);
            return false;
        }
        finally
        {
            if (ReferenceEquals(_configurationCancellation, operation))
            {
                IsLoading = false;
            }
        }
    }

    public void ReportSettingsError(string message) =>
        SetStatus(message, InfoBarSeverity.Error);

    partial void OnIsLoadingChanged(bool value) => IsSettingsInteractionEnabled = !value;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ReloadAsync()
    {
        var operation = BeginConfigurationOperation();
        var cancellationToken = operation.Token;
        IsLoading = true;
        SetStatus("設定を読み込んでいます…");

        try
        {
            var configuration = await _configLoader.LoadAsync(
                _configPath,
                cancellationToken);
            var statusFailureCount = await ApplyConfigurationAsync(
                configuration,
                cancellationToken);
            SetLoadedStatus(statusFailureCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A newer reload or configuration change owns the UI state.
        }
        catch (Exception exception)
        {
            _apiToken = string.Empty;
            Devices.Clear();
            HasDevices = false;
            DeviceSummary = "デバイス";
            SetStatus($"設定エラー: {exception.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            if (ReferenceEquals(_configurationCancellation, operation))
            {
                IsLoading = false;
            }
        }
    }

    private async Task<int> ApplyConfigurationAsync(
        SwitchBotConfiguration configuration,
        CancellationToken cancellationToken)
    {
        _apiToken = configuration.ApiToken;

        Devices.Clear();
        foreach (var device in configuration.Devices)
        {
            Devices.Add(new DeviceControlViewModel(device, SendCommandAsync));
        }

        HasDevices = Devices.Count > 0;
        DeviceSummary = $"デバイス（{Devices.Count}）";

        using var gate = new SemaphoreSlim(MaxConcurrentStatusRequests);
        var refreshTasks = Devices.Select(async device =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await RefreshDeviceStatusAsync(
                    device,
                    configuration.ApiToken,
                    cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        var results = await Task.WhenAll(refreshTasks);
        return results.Count(succeeded => !succeeded);
    }

    private async Task SendCommandAsync(
        DeviceControlViewModel device,
        SwitchBotCommand command)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            SetStatus("APIトークンが読み込まれていません", InfoBarSeverity.Error);
            return;
        }

        var operation = command == SwitchBotCommand.TurnOn ? "ON" : "OFF";
        var apiToken = _apiToken;
        var cancellationToken = _configurationCancellation.Token;
        device.IsBusy = true;
        device.StatusText = $"{operation} を送信中…";
        SetStatus($"{device.Name} に {operation} を送信しています…");

        try
        {
            var result = await _switchBotClient.SendCommandAsync(
                apiToken,
                device.DeviceId,
                command,
                cancellationToken);

            if (result.IsSuccess)
            {
                SetStatus(
                    $"{device.Name} に {operation} を送信しました",
                    InfoBarSeverity.Success);
                await RefreshStatusAfterCommandAsync(
                    device,
                    apiToken,
                    fallbackText: $"{operation} 完了 · 状態未確認",
                    cancellationToken);
            }
            else
            {
                var failureDetails = FormatCommandFailure(result);
                device.StatusText = $"送信失敗 · {failureDetails}";
                SetStatus(
                    $"{device.Name} の操作に失敗しました（{failureDetails}）",
                    InfoBarSeverity.Error);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            device.StatusText = "操作を中断しました";
        }
        catch (Exception exception)
        {
            device.StatusText = "通信エラー";
            SetStatus(
                $"{device.Name} の通信エラー: {exception.Message}",
                InfoBarSeverity.Error);
        }
        finally
        {
            device.IsBusy = false;
        }
    }

    private async Task<bool> RefreshDeviceStatusAsync(
        DeviceControlViewModel device,
        string apiToken,
        CancellationToken cancellationToken,
        bool updateBusyState = true,
        string fallbackText = "状態を取得できません")
    {
        if (updateBusyState)
        {
            device.IsBusy = true;
        }

        device.StatusText = "状態を確認中…";

        try
        {
            var result = await _switchBotClient.GetStatusAsync(
                apiToken,
                device.DeviceId,
                cancellationToken);

            ApplyDeviceStatus(device, result, fallbackText);
            return result.IsSuccess;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            device.StatusText = fallbackText;
            return false;
        }
        finally
        {
            if (updateBusyState)
            {
                device.IsBusy = false;
            }
        }
    }

    private async Task RefreshStatusAfterCommandAsync(
        DeviceControlViewModel device,
        string apiToken,
        string fallbackText,
        CancellationToken cancellationToken)
    {
        var positionBeforeCommand = device.LastKnownPosition;
        var previousPosition = positionBeforeCommand;
        var observedActivity = false;
        var stablePositionCount = 0;

        await Task.Delay(InitialCommandStatusDelay, cancellationToken);

        for (var pollIndex = 0; pollIndex < StatusCompletionPollCount; pollIndex++)
        {
            SwitchBotDeviceStatusResult result;
            try
            {
                result = await _switchBotClient.GetStatusAsync(
                    apiToken,
                    device.DeviceId,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                device.StatusText = fallbackText;
                return;
            }

            ApplyDeviceStatus(device, result, fallbackText);
            if (!result.IsSuccess || result.Position is null)
            {
                return;
            }

            var positionChanged = result.Position != positionBeforeCommand;
            observedActivity |= result.IsMoving is true || positionChanged;

            if (result.IsMoving is false && observedActivity)
            {
                return;
            }

            if (result.IsMoving is null && observedActivity)
            {
                stablePositionCount = result.Position == previousPosition
                    ? stablePositionCount + 1
                    : 0;
                if (stablePositionCount >= 2)
                {
                    return;
                }
            }

            if (!observedActivity && pollIndex + 1 >= StatusStartObservationCount)
            {
                return;
            }

            previousPosition = result.Position;
            await Task.Delay(CommandStatusPollInterval, cancellationToken);
        }
    }

    private CancellationTokenSource BeginConfigurationOperation()
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _configurationCancellation, next);
        previous.Cancel();
        previous.Dispose();
        return next;
    }

    private static void ApplyDeviceStatus(
        DeviceControlViewModel device,
        SwitchBotDeviceStatusResult result,
        string fallbackText)
    {
        if (!result.IsSuccess)
        {
            device.StatusText = fallbackText;
            return;
        }

        device.StatusText = FormatStatusSummary(result);
        if (result.Position is not null)
        {
            device.LastKnownPosition = result.Position;
        }
    }

    private void SetLoadedStatus(int statusFailureCount)
    {
        var message = statusFailureCount == 0
            ? $"{Devices.Count} 台のデバイスを読み込みました"
            : $"{Devices.Count} 台を読み込みました（{statusFailureCount} 台は状態を取得できません）";
        SetStatus(message);
    }

    private static string FormatStatusSummary(SwitchBotDeviceStatusResult result)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.Power))
        {
            details.Add($"電源 {result.Power.ToUpperInvariant()}");
        }

        if (result.Position is not null)
        {
            details.Add($"位置 {result.Position}%");
        }

        if (result.IsMoving is true)
        {
            details.Add("動作中");
        }

        return details.Count > 0 ? string.Join(" · ", details) : "状態取得済み";
    }

    private static string FormatCommandFailure(SwitchBotCommandResult result) =>
        result.ApiStatusCode is int apiStatusCode
            ? $"HTTP {(int)result.StatusCode} / API {apiStatusCode}"
            : $"HTTP {(int)result.StatusCode}";

    private void SetStatus(
        string message,
        InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        StatusMessage = message;
        StatusSeverity = severity;
    }
}
