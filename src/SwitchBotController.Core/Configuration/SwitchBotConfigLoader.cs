using System.Text.Json;
using System.Text.Json.Serialization;
using SwitchBotController.Core.Models;

namespace SwitchBotController.Core.Configuration;

public sealed class SwitchBotConfigLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SwitchBotConfiguration> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Config not found: {Path.GetFullPath(path)}", path);
        }

        await using var stream = File.OpenRead(path);
        var source = await JsonSerializer.DeserializeAsync<ConfigurationFile>(
            stream,
            SerializerOptions,
            cancellationToken);

        if (source is null)
        {
            throw new InvalidDataException("config.json is empty or invalid");
        }

        var apiToken = FirstNonEmpty(source.ApiToken, source.ApiKey);
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new InvalidDataException("api_token is empty in config.json");
        }

        var devices = source.Devices?
            .Where(device => device is not null)
            .Select(device => new Device(
                device!.Name?.Trim() ?? string.Empty,
                FirstNonEmpty(device.DeviceId, device.BleMac) ?? string.Empty))
            .Where(device => device.Name.Length > 0 && device.DeviceId.Length > 0)
            .ToArray() ?? [];

        if (devices.Length == 0)
        {
            throw new InvalidDataException("No valid devices in config.json");
        }

        return new SwitchBotConfiguration(apiToken, devices);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed class ConfigurationFile
    {
        [JsonPropertyName("api_token")]
        public string? ApiToken { get; init; }

        [JsonPropertyName("api_key")]
        public string? ApiKey { get; init; }

        [JsonPropertyName("devices")]
        public List<DeviceEntry?>? Devices { get; init; }
    }

    private sealed class DeviceEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("ble_mac")]
        public string? BleMac { get; init; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; init; }
    }
}
