using Assets.GRL.Scripts.Managers;
using cryptokartz.Scripts.Car;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using static Unity.Collections.Unicode;

public class GhostCarSpawnManager : NetworkBehaviour
{

    int _levelId;
    int _colorId;
    string _vid;
    GameObject _trackPlatform;

    public override void Spawned()
    {
        if (!Runner.IsServer)
        {
            GetComponent<CarTelemetrySubscriber>().enabled = false;

            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _vid = Object.GetComponent<CarDataNetwork>().Vid;
            string objFind = $"TrackPlatformPlacementTool/TrackPlatformPlacementTarget/TrackPlatformContainer/RaceTrackShell{_levelId}";
            _trackPlatform = GameObject.Find(objFind);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
        }
        else
        {
            _vid = Object.GetComponent<CarDataNetwork>().Vid;
            var sub = GetComponent<CarTelemetrySubscriber>();
            sub.vid = _vid;
            sub.enabled = true;
        }
    }
}
