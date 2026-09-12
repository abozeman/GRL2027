using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestClient.Core;
using RestClient.Core.Models;
using UnityEngine;

namespace RestClient.Scripts.Clients
{
    public class RestClientTrackAPI : Fusion.NetworkBehaviour
    {
        [SerializeField]
        private string baseUrl = "http://192.168.2.49:8001";
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
        public void GetTrackDefinition(string track_id)
        {
            TrackDefinition trackDefinition = new();

            // setup the request header
            RequestHeader header = new RequestHeader
            {
                Key = "Content-Type",
                Value = "application/json"
            };

            var fileName = track_id + ".json";

            // send a post request
            StartCoroutine(RestWebClient.Instance.HttpPost($"{baseUrl}/getTrackDefinition",
            JsonUtility.ToJson(new GetTrackDefinitionsRequest { trackId = fileName }),
                (r) => OnGetTrackDefinitionRequestComplete(r), new List<RequestHeader> { header }));
        }

        public void Start()
        {
        }

        private void OnGetTrackDefinitionRequestComplete(Response response)
        {

            try
            {
                //Debug.Log($"OnGetTrackDefinitionRequestComplete Status Code: {response.StatusCode}");
                Debug.Log($"OnGetTrackDefinitionRequestComplete Data: {response.Data}");
               //Debug.Log($"OnGetTrackDefinitionRequestComplete Error: {response.Error}");

                var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Data);

                //Debug.Log($"OnGetTrackDefinitionRequestComplete rawData: + {rawData["message"]}");
                TrackDef = JsonConvert.DeserializeObject<TrackDefinition>(rawData["message"]);
                //TrackDefNet = JsonConvert.DeserializeObject<TrackDefinitionNetworked>(rawData["message"]);
                NotifyGetTrackDefinitionCompleteListener(TrackDef);
            }
            catch (Exception e)
            {
                Debug.Log($"OnGetTrackDefinitionRequestComplete: {e.Message}");
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