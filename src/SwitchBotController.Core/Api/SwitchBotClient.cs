using System.Net.Http.Json;

namespace SwitchBotController.Core.Api;

public sealed class SwitchBotClient(HttpClient httpClient) : ISwitchBotClient
{
    private static readonly Uri ApiBaseAddress = new("https://api.switch-bot.com/v1.0/");

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
        return new SwitchBotCommandResult(response.StatusCode);
    }
}
