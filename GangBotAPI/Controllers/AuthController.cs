using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace GangBotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("discord")]
        public async Task<IActionResult> Discord([FromQuery] string code, [FromQuery] string redirectUri)
        {
            // Exchange the authorization code for an access token and user information
            var accessToken = await ExchangeCodeForAccessToken(code, redirectUri);
            var userInfo = await GetDiscordUserInfo(accessToken);

            // Create a JWT with the user information and return it to the client
            var jwt = CreateJwt(userInfo);
            return Ok(new { token = jwt, userName = userInfo["username"].ToString() });
        }

        private async Task<string> ExchangeCodeForAccessToken(string code, string redirectUri)
        {
            using var httpClient = new HttpClient();

            // Prepare the request data
            var requestData = new Dictionary<string, string>
    {
        {"client_id", _configuration["Discord:ClientId"]},
        {"client_secret", _configuration["Discord:ClientSecret"]},
        {"grant_type", "authorization_code"},
        {"code", code},
        {"redirect_uri", redirectUri},
        {"scope", "identify, guilds"}
    };

            // Send a POST request to Discord's token endpoint
            var response = await httpClient.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(requestData));

            // Ensure the response is successful and parse the JSON
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var parsedResponse = JObject.Parse(jsonResponse);

            // Return the access token
            return parsedResponse["access_token"].ToString();
        }

        private async Task<JObject> GetDiscordUserInfo(string accessToken)
        {
            using var httpClient = new HttpClient();

            // Set the Authorization header with the access token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Send a GET request to Discord's user info endpoint
            var response = await httpClient.GetAsync("https://discord.com/api/users/@me");

            // Ensure the response is successful and parse the JSON
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var userInfo = JObject.Parse(jsonResponse);

            return userInfo;
        }

        public class DiscordGuild
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("permissions")]
            public int Permissions { get; set; }
        }

        private string CreateJwt(JObject userInfo)
        {
            var userId = userInfo["id"].ToString();
            var username = userInfo["username"].ToString();

            var claims = new[]
            {
        new Claim("DiscordUserId", userId),
        new Claim("username", username),
    };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
