using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakApi.Controllers
{
    [ApiController]
    [Route("proxy")]
    public class ProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("{*path}")]
        public async Task<IActionResult> Get(string path)
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
                !authHeader.ToString().StartsWith("Basic "))
            {
                return Unauthorized();
            }

            var encoded = authHeader.ToString()["Basic ".Length..];
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var idx = decoded.IndexOf(':');
            if (idx == -1)
            {
                return Unauthorized();
            }

            // Power BI only needs to send *some* Basic credentials to satisfy its
            // connector requirements. The proxy does not validate the specific
            // username or password, it merely ensures the header is present so the
            // request can be authenticated with Keycloak.

            var token = await GetAccessTokenAsync();
            if (token == null)
            {
                return StatusCode(500, "Failed to obtain Keycloak token");
            }

            var baseUrl = _configuration["Proxy:BackendBaseUrl"] ?? string.Empty;
            var forwardUrl = baseUrl.TrimEnd('/') + "/" + path;

            var forwardRequest = new HttpRequestMessage(HttpMethod.Get, forwardUrl);
            forwardRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var client = _httpClientFactory.CreateClient();
            var resp = await client.SendAsync(forwardRequest);
            var content = await resp.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)resp.StatusCode,
                Content = content,
                ContentType = resp.Content.Headers.ContentType?.ToString() ?? "text/plain"
            };
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            var authority = _configuration["Keycloak:Authority"];
            var clientId = _configuration["Keycloak:ClientId"];
            var clientSecret = _configuration["Keycloak:ClientSecret"];
            if (authority == null || clientId == null || clientSecret == null)
            {
                return null;
            }

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{authority}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                })
            };

            var client = _httpClientFactory.CreateClient();
            var resp = await client.SendAsync(tokenRequest);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("access_token", out var token))
            {
                return token.GetString();
            }
            return null;
        }
    }
}
