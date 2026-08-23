using System.Net;

namespace SwitchBotController.Core.Api;

public sealed record SwitchBotCommandResult(HttpStatusCode StatusCode)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;
}
