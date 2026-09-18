using Assets.GRL.Scripts.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestClient.Core;
using RestClient.Core.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestClient.Scripts.Clients
{
    public class RestClientTrackAPI : Fusion.NetworkBehaviour
    {
        [SerializeField]
        private string baseUrl = "http://192.168.2.49:8001";
        //public TrackDefinitionV1 TrackDefV1 { get; private set; }
        public TrackDefinition TrackDef { get; private set; }

        private readonly List<ITrackAPI> m_getTrackDefinitionCompleteListener = new();

        /// <summary>
        /// Register get track definition complete listener.
        /// </summary>
        /// <param name="listener">The listener.</param>
        public void RegisterGetTrackDefinitionCompleteListener(ITrackAPI listener)
        {
            m_getTrackDefinitionCompleteListener.Add(listener);
        }

        /// <summary>
        /// Unregister get track definition complete listener.
        /// </summary>
        /// <param name="listener">The listener.</param>
        public void UnregisterGetTrackDefinitionCompleteListener(ITrackAPI listener)
        {
            _ = m_getTrackDefinitionCompleteListener.Remove(listener);
        }

        /// <summary>
        /// Get track definition.
        /// </summary>
        /// <param name="track_id">The track id.</param>
        public void GetTrackDefinition(string _track_id)
        {
            // setup the request header
            RequestHeader header = new RequestHeader
            {
                Key = "Content-Type",
                Value = "application/json"
            };

            // send a post request
            StartCoroutine(RestWebClient.Instance.HttpPost($"{baseUrl}/getTrackDefinition",
                JsonUtility.ToJson(new GetTrackDefinitionsRequest { trackId = _track_id }),
                (r) => OnGetTrackDefinitionRequestComplete(r), new List<RequestHeader> { header }));
        }

        public void Start()
        {
        }

        private void OnGetTrackDefinitionRequestComplete(Response response)
        {
            try
            {
                Debug.Log($"OnGetTrackDefinitionRequestComplete Data: {response.Data}");

                if (string.IsNullOrEmpty(response.Data))
                {
                    Debug.LogError("OnGetTrackDefinitionRequestComplete: Received empty response data.");
                    return;
                }

                // Parse response wrapper dynamically via JObject
                JObject envelope = JObject.Parse(response.Data);

                // Extract 'message' or fallback to alternative envelope keys if needed
                JToken messageToken = envelope["message"] ?? envelope["data"] ?? envelope["result"];

                if (messageToken == null)
                {
                    Debug.LogError("OnGetTrackDefinitionRequestComplete: Could not find 'message' property in API response.");
                    return;
                }

                string trackJsonString;

                // Handle both raw nested JSON object AND escaped string cases cleanly
                if (messageToken.Type == JTokenType.String)
                {
                    trackJsonString = messageToken.ToString();
                }
                else
                {
                    trackJsonString = messageToken.ToString(Formatting.None);
                }

                // Parse the track definition using our standardized parser
                TrackDef = TrackDefinition.Parse(trackJsonString);

                NotifyGetTrackDefinitionCompleteListener(TrackDef);
            }
            catch (Exception e)
            {
                Debug.LogError($"OnGetTrackDefinitionRequestComplete Exception: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        private void NotifyGetTrackDefinitionCompleteListener(TrackDefinition trackDefinition)
        {
            foreach (var listener in m_getTrackDefinitionCompleteListener)
            {
                listener.OnTrackDefinitionReceived(trackDefinition);
            }
        }

        public class GetTrackDefinitionsRequest
        {
            public string trackId;
        }
    }
}