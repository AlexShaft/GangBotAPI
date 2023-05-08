using Assets.Networking.DataBase.RedisProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GangBotAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "DiscordAllowlist")]
    [Route("[controller]")]
    public class Redis : ControllerBase
    {

        private readonly ILogger<Redis> _logger;
        private readonly RedisProviderService redisProvider;

        public Redis(ILogger<Redis> logger, RedisProviderService redisProvider)
        {
            _logger = logger;
            this.redisProvider = redisProvider;
        }

        [Authorize(Policy = "DiscordAllowlist")]
        [HttpGet(Name = "GetKey")]
        public string GetKey(string key)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return redisProvider.GetValue(key);
        }

        [Authorize(Policy = "DiscordAllowlist")]
        [HttpPost(Name = "SetKey")]
        public void SetKey(string key, string val)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            redisProvider.SetValue(key, val);
        }
    }
}