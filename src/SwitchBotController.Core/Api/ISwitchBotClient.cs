namespace SwitchBotController.Core.Api;

public interface ISwitchBotClient
{
    Task<SwitchBotCommandResult> SendCommandAsync(
        string apiToken,
        string deviceId,
        SwitchBotCommand command,
        CancellationToken cancellationToken = default);
}
