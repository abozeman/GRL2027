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


    public override void Spawned()
    {

        if (Object.Runner.IsServer)
        {
            //GetComponent<CarInputManagerLive>().enabled = true;
            //GetComponent<CarTelemetrySubscriber>().enabled = false;
            //GetComponent<CarControlDataLivePublisher>().enabled = false;

            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _vid = Object.GetComponent<CarDataNetwork>().Vid;
            //string objFind = $"TrackPlatformPlacementTool/TrackPlatformPlacementTarget/TrackPlatformContainer/RaceTrackShell{_levelId}";
            //string objFind = $"PlatformSurface/PlatformContainer/RaceTrackShell1";
            string objFind = $"RaceTrackShell1";
            //string objFind = $"RaceTrackShell{_levelId}";
            //string objFind = $"RaceTrackShell1";
            _trackPlatform = FindInRunnerScene(objFind);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            //_trackPlatform.GetComponent<TrackDefinitionManager>().TrackId = );
        }
        else
        {
            //GetComponent<CarInputManagerLive>().enabled = false;
            //GetComponent<CarTelemetrySubscriber>().enabled = true;
            //GetComponent<CarControlDataLivePublisher>().enabled = true;
        }
    }

    private GameObject FindInRunnerScene(string objectName)
    {
        foreach (var rootObj in gameObject.scene.GetRootGameObjects())
        {
            Transform[] transforms = rootObj.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.name == objectName) return t.gameObject;
            }
        }
        return null;
    }
}
