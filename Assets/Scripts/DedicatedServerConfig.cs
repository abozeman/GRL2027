using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
        private static SceneRef sRef;


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

            sRef = GetSceneRefFromPath("Assets/Scenes/GRLGame.unity");


            config.SceneId = sRef.AsIndex;

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

        public static SceneRef GetSceneRefFromPath(string scenePath)
        {
            // 1. Resolve the Unity build index from the specific asset path
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            // 2. Ensure the scene actually exists in the Build Settings
            if (buildIndex >= 0)
            {
                // 3. Convert the valid build index into a Fusion SceneRef
                return SceneRef.FromIndex(buildIndex);
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to create SceneRef: '{scenePath}' is not in the Build Settings.");
                return default;
            }
        }




    }
}
