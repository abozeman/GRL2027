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
    GameObject _masterTelemetrySubsciber;


    public override void Spawned()
    {

        if (Object.Runner.IsServer)
        {
            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _vid = Object.GetComponent<CarDataNetwork>().Vid;

            //Find the Track in this Session and attach the car to it
            string objFind = $"RaceTrackShell1";
            _trackPlatform = FindInRunnerScene(objFind);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

            //find the MasterTelemetry Subscriber and register the car with it
            string subscriber = $"MasterTelemetryProvider";
            _masterTelemetrySubsciber = FindInRunnerScene(subscriber);
            _masterTelemetrySubsciber.GetComponent<MasterTelemetrySubscriber>().AddCar(_vid, transform.gameObject);
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
