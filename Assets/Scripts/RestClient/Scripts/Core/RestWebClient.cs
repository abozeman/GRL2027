using System.Collections;
using System.Collections.Generic;
using RestClient.Core.Models;
using RestClient.Core.Singletons;
using UnityEngine;
using UnityEngine.Networking;
 
namespace RestClient.Core
{
    public class RestWebClient : Singleton<RestWebClient>
    {
        private const string defaultContentType = "application/json";
        /// <summary>
        /// Http the get.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An IEnumerator</returns>
        public IEnumerator HttpGet(string url, System.Action<Response> callback)
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                
                if(webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                    });
                }
                
                if(webRequest.isDone)
                {
                    string data = System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data);
                    Debug.Log("Data: " + data);
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                        Data = data
                    });
                }
            }
        }

        /// <summary>
        /// Https the delete.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An IEnumerator</returns>
        public IEnumerator HttpDelete(string url, System.Action<Response> callback)
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Delete(url))
            {
                yield return webRequest.SendWebRequest();

                if(webRequest.isNetworkError){
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error
                    });
                }
                
                if(webRequest.isDone)
                {
                    callback(new Response {
                        StatusCode = webRequest.responseCode
                    });
                }
            }
        }

        /// <summary>
        /// Http the post.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="body">The body.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="headers">The headers.</param>
        /// <returns>An IEnumerator</returns>
        public IEnumerator HttpPost(string url, string body, System.Action<Response> callback, IEnumerable<RequestHeader> headers = null)
        {
            using(UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, body))
            {
                if(headers != null)
                {
                    foreach (RequestHeader header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }

                webRequest.uploadHandler.contentType = defaultContentType;
                webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));

                yield return webRequest.SendWebRequest();

                if(webRequest.isNetworkError)
                {
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error
                    });
                }
                
                if(webRequest.isDone)
                {
                    string data = System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data);
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                        Data = data
                    });
                }
            }
        }

        /// <summary>
        /// Https the put.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="body">The body.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="headers">The headers.</param>
        /// <returns>An IEnumerator</returns>
        public IEnumerator HttpPut(string url, string body, System.Action<Response> callback, IEnumerable<RequestHeader> headers = null)
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Put(url, body))
            {
                if(headers != null)
                {
                    foreach (RequestHeader header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }

                webRequest.uploadHandler.contentType = defaultContentType;
                webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));

                yield return webRequest.SendWebRequest();

                if(webRequest.isNetworkError)
                {
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                    });
                }
                
                if(webRequest.isDone)
                {
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                    });
                }
            }
        }

        /// <summary>
        /// Http the head.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An IEnumerator</returns>
        public IEnumerator HttpHead(string url, System.Action<Response> callback)
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Head(url))
            {
                yield return webRequest.SendWebRequest();
                
                if(webRequest.isNetworkError){
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                    });
                }
                
                if(webRequest.isDone)
                {
                    var responseHeaders = webRequest.GetResponseHeaders();
                    callback(new Response {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                        Headers = responseHeaders
                    });
                }
            }
        }
    }
}