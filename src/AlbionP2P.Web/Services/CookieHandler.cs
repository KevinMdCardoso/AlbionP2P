using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AlbionP2P.Web.Services;

/// <summary>
/// Inclui cookies em todas as requisições cross-origin (necessário para cookie-auth no WASM).
/// Substitui o HttpClientHandler que NÃO é suportado na plataforma WebAssembly.
/// </summary>
internal sealed class CookieHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Equivalente a fetch(..., { credentials: 'include' }) no browser
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        return base.SendAsync(request, cancellationToken);
    }
}
