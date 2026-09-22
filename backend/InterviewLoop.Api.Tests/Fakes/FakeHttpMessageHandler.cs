namespace InterviewLoop.Api.Tests.Fakes;

/// <summary>
/// Intercepts outgoing HttpClient requests with a caller-supplied responder, so
/// GeminiGradingServiceTests can simulate any Gemini response (success, 429, 503, malformed
/// body) without a real network call. Plug into an HttpClient via
/// <c>new HttpClient(new FakeHttpMessageHandler(...))</c>.
/// </summary>
public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));
}
