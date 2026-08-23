using System.Net;

namespace SwitchBotController.Core.Api;

public sealed record SwitchBotCommandResult(
    HttpStatusCode StatusCode,
    int? ApiStatusCode)
{
    public bool IsSuccess =>
        (int)StatusCode is >= 200 and <= 299 && ApiStatusCode == 100;
}
