using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace ECommerce.Shared.Core.Http;

public sealed class ForwardAuthorizationHeaderHandler(
    IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    private const string AuthorizationHeaderName = "Authorization";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = httpContextAccessor.HttpContext?
            .Request
            .Headers[AuthorizationHeaderName]
            .ToString();

        if (!string.IsNullOrWhiteSpace(authorization) &&
            AuthenticationHeaderValue.TryParse(authorization, out var authorizationHeader))
        {
            request.Headers.Authorization = authorizationHeader;
        }

        return base.SendAsync(request, cancellationToken);
    }
}