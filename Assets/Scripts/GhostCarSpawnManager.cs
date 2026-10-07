using Assets.GRL.Scripts.Managers;
using cryptokartz.Scripts.Car;
using Fusion;
using Unity.VisualScripting;
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
        _vid = Object.GetComponent<CarDataNetwork>().Vid;

        if (!Object.Runner.IsServer)
        {

            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _levelId = 1;
            //string objFind = $"TrackPlatformPlacementTool/TrackPlatformPlacementTarget/TrackPlatformContainer/RaceTrackShell{_levelId}";
            string objFind = $"RaceTrackShell{_levelId}";
            //_trackPlatform = GameObject.Find(objFind);
            _trackPlatform = FindInRunnerScene(objFind);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
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
