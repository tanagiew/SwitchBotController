using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwitchBotController.Core.Api;

public sealed class SwitchBotClient(HttpClient httpClient) : ISwitchBotClient
{
    private static readonly Uri ApiBaseAddress = new("https://api.switch-bot.com/v1.0/");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SwitchBotDeviceStatusResult> GetStatusAsync(
        string apiToken,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(ApiBaseAddress, $"devices/{Uri.EscapeDataString(deviceId)}/status"));
        request.Headers.TryAddWithoutValidation("Authorization", apiToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadEnvelopeAsync(response, cancellationToken);

        return new SwitchBotDeviceStatusResult(
            response.StatusCode,
            payload?.StatusCode,
            FirstNonEmpty(payload?.Body?.Power, payload?.Body?.PowerState),
            payload?.Body?.SlidePosition,
            payload?.Body?.Moving);
    }

    public async Task<SwitchBotCommandResult> SendCommandAsync(
        string apiToken,
        string deviceId,
        SwitchBotCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var commandName = command switch
        {
            SwitchBotCommand.TurnOn => "turnOn",
            SwitchBotCommand.TurnOff => "turnOff",
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(ApiBaseAddress, $"devices/{Uri.EscapeDataString(deviceId)}/commands"));
        request.Headers.TryAddWithoutValidation("Authorization", apiToken);
        request.Content = JsonContent.Create(new
        {
            command = commandName,
            parameter = "default",
            commandType = "command"
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await ReadEnvelopeAsync(response, cancellationToken);
        return new SwitchBotCommandResult(response.StatusCode, payload?.StatusCode);
    }

    private static async Task<ApiEnvelope?> ReadEnvelopeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<ApiEnvelope>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            // A malformed response is represented by a null payload.
            return null;
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed class ApiEnvelope
    {
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; init; }

        [JsonPropertyName("body")]
        public StatusBody? Body { get; init; }
    }

    private sealed class StatusBody
    {
        [JsonPropertyName("power")]
        public string? Power { get; init; }

        [JsonPropertyName("powerState")]
        public string? PowerState { get; init; }

        [JsonPropertyName("slidePosition")]
        public int? SlidePosition { get; init; }

        [JsonPropertyName("moving")]
        public bool? Moving { get; init; }
    }
}
