using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Diagnostics;

namespace Assets.Networking.DataBase.RedisProvider
{
    public class RedisProviderService
    {
        public IDatabase DB { get; private set; }

        private string redisKey;

        public event Action Inited;

        public bool inited = false;

        public RedisProviderService()
        {
            redisKey = "GangBot";
            Connect();
        }

        public bool Connect()
        {

            var task = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "xxx.cloud.redislabs.com:13635" },
                Password = "xxxxxxx"
            });
            var db = task?.GetDatabase();

            if (!db.IsConnected(redisKey))
            {
                return false;

            }

            DB = db;

            var pong = db.Ping();
            Debug.WriteLine(pong);
            inited = true;
            Inited?.Invoke();
            return true;
        }

        public string GetValue(string key)
        {
            if (DB.HashExists(redisKey, key))
            {
                var res = DB.HashGet("GangBot", key);

                return res;

            }
            else
            {
                return null;
            }
        }

        public void SetValue(string key, string value)
        {
            DB.HashSet(redisKey, new HashEntry[] { new HashEntry(key, value) });
        }
    }
}
