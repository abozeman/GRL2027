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
    public int _levelId;
    public int _colorId;
    public string _vid;
    GameObject _trackPlatform;
    GameObject _masterTelemetrySubsciber;


    public override void Spawned()
    {

        if (Object.Runner.IsServer)
        {
            //GetComponent<CarInputManagerLive>().enabled = true;
            //GetComponent<CarControlDataLivePublisher>().enabled = false;

            _levelId = Object.GetComponent<CarDataNetwork>().LevelId;
            _colorId = Object.GetComponent<CarDataNetwork>().ColorId;
            _vid = Object.GetComponent<CarDataNetwork>().Vid;

            //Find the Track in this Session and attach the car to it
            string track = $"RaceTrackShell1";
            _trackPlatform = FindInRunnerScene(track);
            transform.SetParent(_trackPlatform.transform);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

            //find the MasterTelemetry Subscriber and register the car with it
            string subscriber = $"MasterTelemetryProvider";
            _masterTelemetrySubsciber = FindInRunnerScene(subscriber);
            _masterTelemetrySubsciber.GetComponent<MasterTelemetrySubscriber>().AddCar(_vid, transform.gameObject);
        }
        else
        {
            //GetComponent<CarInputManagerLive>().enabled = false;
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
