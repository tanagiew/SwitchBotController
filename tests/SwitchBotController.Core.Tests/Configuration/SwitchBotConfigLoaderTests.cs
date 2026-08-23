using SwitchBotController.Core.Configuration;

namespace SwitchBotController.Core.Tests.Configuration;

public sealed class SwitchBotConfigLoaderTests
{
    [Fact]
    public async Task LoadAsync_LoadsAndTrimsValidConfiguration()
    {
        var path = await CreateTemporaryConfigAsync("""
            {
              "api_token": " test-token ",
              "devices": [
                { "name": " Desk light ", "ble_mac": " AA11 " },
                { "name": "", "ble_mac": "ignored" }
              ]
            }
            """);

        try
        {
            var result = await new SwitchBotConfigLoader().LoadAsync(path);

            Assert.Equal("test-token", result.ApiToken);
            var device = Assert.Single(result.Devices);
            Assert.Equal("Desk light", device.Name);
            Assert.Equal("AA11", device.DeviceId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_LoadsLegacyApiKeyAndDeviceIdConfiguration()
    {
        var path = await CreateTemporaryConfigAsync("""
            {
              "api_key": " legacy-token ",
              "devices": [{ "name": " Legacy device ", "device_id": " DEVICE-1 " }]
            }
            """);

        try
        {
            var result = await new SwitchBotConfigLoader().LoadAsync(path);

            Assert.Equal("legacy-token", result.ApiToken);
            var device = Assert.Single(result.Devices);
            Assert.Equal("Legacy device", device.Name);
            Assert.Equal("DEVICE-1", device.DeviceId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsEmptyToken()
    {
        var path = await CreateTemporaryConfigAsync("""
            { "api_token": " ", "devices": [{ "name": "Light", "ble_mac": "AA11" }] }
            """);

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new SwitchBotConfigLoader().LoadAsync(path));

            Assert.Contains("api_token", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsConfigurationWithoutValidDevices()
    {
        var path = await CreateTemporaryConfigAsync("""
            { "api_token": "test-token", "devices": [{ "name": "", "ble_mac": "" }] }
            """);

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => new SwitchBotConfigLoader().LoadAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> CreateTemporaryConfigAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"switchbot-controller-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
