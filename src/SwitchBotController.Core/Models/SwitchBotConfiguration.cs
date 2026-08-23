namespace SwitchBotController.Core.Models;

public sealed record SwitchBotConfiguration(
    string ApiToken,
    IReadOnlyList<Device> Devices);
