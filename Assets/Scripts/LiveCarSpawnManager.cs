using Assets.GRL.Scripts;
using Assets.GRL.Scripts.Managers;
using cryptokartz.Scripts.Car;
using cryptokartz.Scripts.Player;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.Unicode;

public class LiveCarSpawnManager : NetworkBehaviour
{
    int _levelId;
    int _colorId;
    string _vid;
    GameObject _trackPlatform;
    public bool isTestMode { get; set; } = true;


    public override void Spawned()
    {
        if (!Runner.IsServer  || isTestMode)
        {
            //GetComponent<CarInputManagerLive>().enabled = true;
            //GetComponent<CarTelemetrySubscriber>().enabled = false;
            //GetComponent<CarControlDataLivePublisher>().enabled = false;

            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _vid = Object.GetComponent<CarDataNetwork>().Vid;
            //string objFind = $"TrackPlatformPlacementTool/TrackPlatformPlacementTarget/TrackPlatformContainer/RaceTrackShell{_levelId}";
            string objFind = $"PlatformSurface/PlatformContainer/RaceTrackShell1";
            //string objFind = $"RaceTrackShell{_levelId}";
            //string objFind = $"RaceTrackShell1";
            _trackPlatform = GameObject.Find(objFind);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
        }
        else
        {
            //GetComponent<CarInputManagerLive>().enabled = false;
            //GetComponent<CarTelemetrySubscriber>().enabled = true;
            //GetComponent<CarControlDataLivePublisher>().enabled = true;
        }
    }
}
