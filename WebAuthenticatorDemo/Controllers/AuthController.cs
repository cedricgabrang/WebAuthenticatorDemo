using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace WebAuthenticatorDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        const string callbackScheme = "myapp";

        [HttpGet("authenticate")]
        public async Task Authenticate()
        {

            var result = await Request.HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

            if (!result.Succeeded
                || result?.Principal == null
                || !result.Principal.Identities.Any(id => id.IsAuthenticated)
                || string.IsNullOrEmpty(result.Properties.GetTokenValue("access_token")))
            {
                // Not authenticated, challenge
                await Request.HttpContext.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);
            }
            else
            {
                var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var name = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;

                // Get parameters to send back to the callback
                var parameters = new Dictionary<string, string>
                    {
                        { "access_token", result.Properties.GetTokenValue("access_token") ?? string.Empty },
                        { "refresh_token", result.Properties.GetTokenValue("refresh_token") ?? string.Empty },
                        { "expires_in", (result.Properties.ExpiresUtc?.ToUnixTimeSeconds() ?? -1).ToString() },
                        { "email", email ?? string.Empty },
                        { "name", name ?? string.Empty }
                    };

                // Build the result url
                var url = callbackScheme + "://#" + string.Join("&",
                    parameters.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "-1")
                    .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

                // Redirect to final url
                Request.HttpContext.Response.Redirect(url);
            }
        }
    }
}
