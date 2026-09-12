using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITrackVisualsManager
{
    void OnTrackVisualsReady(TrackDefinition trackDefinition, int trackLevelId);
}
