using System.Net;
using SwitchBotController.App.ViewModels;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Configuration;

namespace SwitchBotController.App.Tests.ViewModels;

public sealed class MainPageViewModelTests
{
    [Fact]
    public async Task ChangeConfigurationAsync_IsolatesStatusFailurePerDevice()
    {
        using var configuration = await TemporaryConfiguration.CreateAsync(
            ("Working", "DEVICE-1"),
            ("Unavailable", "DEVICE-2"));
        var client = new FakeSwitchBotClient
        {
            StatusResponder = (deviceId, _) => deviceId == "DEVICE-2"
                ? Task.FromException<SwitchBotDeviceStatusResult>(
                    new HttpRequestException("offline"))
                : Task.FromResult(SuccessfulStatus(power: "on"))
        };
        var viewModel = new MainPageViewModel(
            new SwitchBotConfigLoader(),
            client,
            configuration.Path);

        var changed = await viewModel.ChangeConfigurationAsync(configuration.Path);

        Assert.True(changed);
        Assert.Equal(2, viewModel.Devices.Count);
        Assert.Equal("電源 ON", viewModel.Devices[0].StatusText);
        Assert.Equal("状態を取得できません", viewModel.Devices[1].StatusText);
        Assert.Contains("1 台は状態を取得できません", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ChangeConfigurationAsync_CancelsPreviousCommandStatusPolling()
    {
        using var firstConfiguration = await TemporaryConfiguration.CreateAsync(
            ("First", "DEVICE-1"));
        using var secondConfiguration = await TemporaryConfiguration.CreateAsync(
            ("Second", "DEVICE-2"));
        var client = new FakeSwitchBotClient
        {
            StatusResponder = (_, _) => Task.FromResult(
                SuccessfulStatus(position: 0, isMoving: false))
        };
        var viewModel = new MainPageViewModel(
            new SwitchBotConfigLoader(),
            client,
            firstConfiguration.Path);
        Assert.True(await viewModel.ChangeConfigurationAsync(firstConfiguration.Path));
        var previousDevice = viewModel.Devices.Single();

        var commandTask = previousDevice.TurnOnCommand.ExecuteAsync(null);
        await Task.Yield();
        Assert.True(previousDevice.IsBusy);

        Assert.True(await viewModel.ChangeConfigurationAsync(secondConfiguration.Path));
        var completedTask = await Task.WhenAny(commandTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(commandTask, completedTask);
        await commandTask;
        Assert.False(previousDevice.IsBusy);
        Assert.Equal("操作を中断しました", previousDevice.StatusText);
        Assert.Equal("Second", viewModel.Devices.Single().Name);
    }

    [Fact]
    public async Task ChangeConfigurationAsync_LimitsConcurrentStatusRequests()
    {
        using var configuration = await TemporaryConfiguration.CreateAsync(
            ("Device 1", "DEVICE-1"),
            ("Device 2", "DEVICE-2"),
            ("Device 3", "DEVICE-3"),
            ("Device 4", "DEVICE-4"),
            ("Device 5", "DEVICE-5"),
            ("Device 6", "DEVICE-6"));
        var releaseRequests = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstBatchStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var startedRequestCount = 0;
        var concurrentRequestCount = 0;
        var maximumConcurrentRequestCount = 0;
        var concurrencyLock = new object();
        var client = new FakeSwitchBotClient
        {
            StatusResponder = async (_, cancellationToken) =>
            {
                var concurrent = Interlocked.Increment(ref concurrentRequestCount);
                lock (concurrencyLock)
                {
                    maximumConcurrentRequestCount = Math.Max(
                        maximumConcurrentRequestCount,
                        concurrent);
                }

                if (Interlocked.Increment(ref startedRequestCount) == 4)
                {
                    firstBatchStarted.TrySetResult(true);
                }

                try
                {
                    await releaseRequests.Task.WaitAsync(cancellationToken);
                    return SuccessfulStatus(power: "on");
                }
                finally
                {
                    Interlocked.Decrement(ref concurrentRequestCount);
                }
            }
        };
        var viewModel = new MainPageViewModel(
            new SwitchBotConfigLoader(),
            client,
            configuration.Path);

        var changeTask = viewModel.ChangeConfigurationAsync(configuration.Path);
        await firstBatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(4, Volatile.Read(ref startedRequestCount));
        Assert.Equal(4, maximumConcurrentRequestCount);
        releaseRequests.TrySetResult(true);
        Assert.True(await changeTask);
        Assert.Equal(6, Volatile.Read(ref startedRequestCount));
        Assert.Equal(4, maximumConcurrentRequestCount);
    }

    private static SwitchBotDeviceStatusResult SuccessfulStatus(
        string? power = null,
        int? position = null,
        bool? isMoving = null) =>
        new(HttpStatusCode.OK, 100, power, position, isMoving);

    private sealed class FakeSwitchBotClient : ISwitchBotClient
    {
        public Func<string, CancellationToken, Task<SwitchBotDeviceStatusResult>>
            StatusResponder { get; init; } = (_, _) =>
                Task.FromResult(SuccessfulStatus(power: "off"));

        public Task<SwitchBotDeviceStatusResult> GetStatusAsync(
            string apiToken,
            string deviceId,
            CancellationToken cancellationToken = default) =>
            StatusResponder(deviceId, cancellationToken);

        public Task<SwitchBotCommandResult> SendCommandAsync(
            string apiToken,
            string deviceId,
            SwitchBotCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SwitchBotCommandResult(HttpStatusCode.OK, 100));
    }

    private sealed class TemporaryConfiguration : IDisposable
    {
        private TemporaryConfiguration(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        private string Directory { get; }

        public string Path { get; }

        public static async Task<TemporaryConfiguration> CreateAsync(
            params (string Name, string DeviceId)[] devices)
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"switchbot-controller-app-tests-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "config.json");
            var entries = string.Join(
                ",",
                devices.Select(device =>
                    $$"""{"name":"{{device.Name}}","device_id":"{{device.DeviceId}}"}"""));
            await File.WriteAllTextAsync(
                path,
                $$"""{"api_token":"test-token","devices":[{{entries}}]}""");
            return new TemporaryConfiguration(directory, path);
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
