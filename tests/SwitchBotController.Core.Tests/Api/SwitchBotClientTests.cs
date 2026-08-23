using System.Net;
using SwitchBotController.Core.Api;

namespace SwitchBotController.Core.Tests.Api;

public sealed class SwitchBotClientTests
{
    [Fact]
    public async Task GetStatusAsync_ReadsPowerWithoutSendingACommand()
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? capturedToken = null;
        var handler = new RecordingHandler(request =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            capturedToken = request.Headers.GetValues("Authorization").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    { "statusCode": 100, "body": { "power": "on" } }
                    """)
            });
        });
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.GetStatusAsync("secret-token", "AA:BB");

        Assert.True(result.IsSuccess);
        Assert.Equal("on", result.Power);
        Assert.Null(result.Position);
        Assert.Equal(HttpMethod.Get, capturedMethod);
        Assert.Equal("https://api.switch-bot.com/v1.0/devices/AA%3ABB/status", capturedUri?.AbsoluteUri);
        Assert.Equal("secret-token", capturedToken);
    }

    [Fact]
    public async Task GetStatusAsync_ReadsPositionAndMovementWithoutDeviceTypeBranching()
    {
        var handler = new RecordingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "statusCode": 100,
                      "body": { "slidePosition": 42, "moving": true }
                    }
                    """)
            }));
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.GetStatusAsync("secret-token", "DEVICE-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Position);
        Assert.True(result.IsMoving);
        Assert.Null(result.Power);
    }

    [Fact]
    public async Task GetStatusAsync_ReadsPowerStateFallback()
    {
        var handler = new RecordingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    { "statusCode": 100, "body": { "powerState": "OFF" } }
                    """)
            }));
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.GetStatusAsync("secret-token", "DEVICE-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("OFF", result.Power);
    }

    [Fact]
    public async Task GetStatusAsync_IsUnsuccessfulForApiError()
    {
        var handler = new RecordingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    { "statusCode": 190, "body": {} }
                    """)
            }));
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.GetStatusAsync("secret-token", "DEVICE-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(190, result.ApiStatusCode);
    }

    [Fact]
    public async Task GetStatusAsync_IsUnsuccessfulForMalformedResponse()
    {
        var handler = new RecordingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            }));
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.GetStatusAsync("secret-token", "DEVICE-1");

        Assert.False(result.IsSuccess);
    }

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
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    { "statusCode": 100, "body": {} }
                    """)
            };
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

    [Fact]
    public async Task SendCommandAsync_IsUnsuccessfulForApiError()
    {
        var handler = new RecordingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    { "statusCode": 190, "body": {} }
                    """)
            }));
        var client = new SwitchBotClient(new HttpClient(handler));

        var result = await client.SendCommandAsync(
            "secret-token",
            "DEVICE-1",
            SwitchBotCommand.TurnOn);

        Assert.False(result.IsSuccess);
        Assert.Equal(190, result.ApiStatusCode);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }
}
