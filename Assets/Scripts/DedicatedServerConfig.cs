using System;
using System.Collections.Generic;
using Fusion;

namespace Assets.CryptoKartz.Scripts.Utils
{
    public class DedicatedServerConfig
    {

        public string SessionName { get; set; }
        public string Region { get; set; }
        public string Lobby { get; set; }
        public ushort Port { get; set; } = 0;
        public ushort PublicPort { get; set; }
        public string PublicIP { get; set; }
        public int SceneId { get; set; }
        public Dictionary<string, SessionProperty> SessionProperties { get; private set; } = new Dictionary<string, SessionProperty>();

        public DedicatedServerConfig() { }

        public static DedicatedServerConfig AgentResolve(StartGameConfig agentConfig)
        {

            var config = new DedicatedServerConfig();
            config.SessionName = agentConfig.sessionName;
            config.Lobby = agentConfig.customLobby;
            config.Port = 0;

            config.SessionProperties.Add("type", agentConfig.raceType);
            config.SessionProperties.Add("RacePlatformLevel", agentConfig.level);
            config.SessionProperties.Add("trackid", agentConfig.trackId);

            config.SceneId = (int)SceneDefs.ERLGame;

            return config;
        }

        /// <summary>
        /// Converts to the string.
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {

            var properties = string.Empty;

            foreach (var item in SessionProperties)
            {
                properties += $"{item.Key}={item.Value}, ";
            }

            return $"[{nameof(DedicatedServerConfig)}]: " +
              $"{nameof(SessionName)}={SessionName}, " +
              $"{nameof(Region)}={Region}, " +
              $"{nameof(Lobby)}={Lobby}, " +
              $"{nameof(Port)}={Port}, " +
              $"{nameof(PublicIP)}={PublicIP}, " +
              $"{nameof(PublicPort)}={PublicPort}, " +
              $"{nameof(SessionProperties)}={properties}]";
        }


        

    }
}
