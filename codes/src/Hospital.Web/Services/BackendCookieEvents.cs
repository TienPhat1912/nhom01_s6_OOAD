using System.Net;
using System.Security.Claims;
using Hospital.Application.DTOs.Auth;
using Hospital.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Hospital.Web.Services;

public sealed class BackendCookieEvents(HospitalApiClient api) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var token = context.Principal?.FindFirstValue("access_token");
        if (string.IsNullOrEmpty(token)) { context.RejectPrincipal(); return; }
        try
        {
            var profile = await api.SendAsync<UserProfileDto>(HttpMethod.Get, "api/auth/profile", token: token,
                cancellationToken: context.HttpContext.RequestAborted);
            context.HttpContext.Items["Profile"] = profile;
        }
        catch (HospitalApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (HospitalApiException ex)
        {
            // Retain the session during an outage; do not serve cached protected data.
            context.HttpContext.Items["ApiFailure"] = ex;
        }
    }
}
