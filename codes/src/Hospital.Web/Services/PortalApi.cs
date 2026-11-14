using System.Security.Claims;
using Hospital.Client;

namespace Hospital.Web.Services;

public sealed class PortalApi(HospitalApiClient client, IHttpContextAccessor context)
{
    public Task<T> GetAsync<T>(string path) => client.SendAsync<T>(HttpMethod.Get, path,
        token: context.HttpContext?.User.FindFirstValue("access_token"), cancellationToken: context.HttpContext?.RequestAborted ?? default);
    public Task<T> SendAsync<T>(HttpMethod method, string path, object body) => client.SendAsync<T>(method, path, body,
        context.HttpContext?.User.FindFirstValue("access_token"), context.HttpContext?.RequestAborted ?? default);
}
