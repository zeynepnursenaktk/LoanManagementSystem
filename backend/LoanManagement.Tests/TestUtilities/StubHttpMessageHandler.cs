using System.Net;

namespace LoanManagement.Tests.TestUtilities;

/// HttpClient testleri için minimal sahte mesaj işleyicisi.
/// Verilen `responder` delegesi üzerinden istek bazlı yanıt döner; çağrılan istekleri saklar.
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
    public List<HttpRequestMessage> Requests { get; } = new();
    public int CallCount => Requests.Count;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public static StubHttpMessageHandler ReturnJson(HttpStatusCode status, string json)
        => new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        }));

    public static StubHttpMessageHandler ThrowsTransient(int times, HttpResponseMessage successResponse)
    {
        int attempt = 0;
        return new((_, _) =>
        {
            if (Interlocked.Increment(ref attempt) <= times)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return Task.FromResult(successResponse);
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // İstek gövdesini önceden buffer'la — testlerin sonradan inceleyebilmesi için.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);
        }
        Requests.Add(request);
        return await _responder(request, cancellationToken).ConfigureAwait(false);
    }
}
