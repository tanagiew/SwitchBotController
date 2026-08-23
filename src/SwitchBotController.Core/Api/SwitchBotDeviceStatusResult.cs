using System.Net;

namespace SwitchBotController.Core.Api;

public sealed record SwitchBotDeviceStatusResult(
    HttpStatusCode StatusCode,
    int? ApiStatusCode,
    string? Power,
    int? Position,
    bool? IsMoving)
{
    public bool IsSuccess =>
        (int)StatusCode is >= 200 and <= 299 && ApiStatusCode == 100;
}
