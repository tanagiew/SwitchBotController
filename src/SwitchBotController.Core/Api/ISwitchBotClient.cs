namespace SwitchBotController.Core.Api;

public interface ISwitchBotClient
{
    Task<SwitchBotDeviceStatusResult> GetStatusAsync(
        string apiToken,
        string deviceId,
        CancellationToken cancellationToken = default);

    Task<SwitchBotCommandResult> SendCommandAsync(
        string apiToken,
        string deviceId,
        SwitchBotCommand command,
        CancellationToken cancellationToken = default);
}
