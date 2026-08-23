using System.Net;
using SwitchBotController.Core.Api;

namespace SwitchBotController.Core.Tests.Api;

public sealed class SwitchBotClientTests
{
    [Theory]
    [InlineData(SwitchBotCommand.TurnOn, "turnOn")]
    [InlineData(SwitchBotCommand.TurnOff, "turnOff")]
    public async Task SendCommandAsync_SendsCompatibleV1Request(
        SwitchBotCommand command,
        string expectedCommand)
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? capturedToken = null;
        string? capturedBody = null;
        var handler = new RecordingHandler(async request =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            capturedToken = request.Headers.GetValues("Authorization").Single();
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.SendCommandAsync("secret-token", "AA:BB", command);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal("https://api.switch-bot.com/v1.0/devices/AA%3ABB/commands", capturedUri?.AbsoluteUri);
        Assert.Equal("secret-token", capturedToken);
        Assert.Contains($"\"command\":\"{expectedCommand}\"", capturedBody);
        Assert.Contains("\"parameter\":\"default\"", capturedBody);
        Assert.Contains("\"commandType\":\"command\"", capturedBody);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }
}
