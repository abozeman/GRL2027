using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEventManager : NetworkBehaviour
{
    [Networked] public NetworkBool EventActive { get; set; }
    private ChangeDetector _changes;
    private MeshRenderer[] _meshRenderers;


    public override void Spawned()
    {
        base.Spawned();
        EventActive = false;
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        _meshRenderers = transform.GetComponentsInChildren<MeshRenderer>();

    }

    public override void Render()
    {
        foreach (string propertyName in _changes.DetectChanges(this))
        {
            switch (propertyName)
            {
                case nameof(EventActive):
                    if (EventActive)
                    {
                        StartCoroutine(ShowAndHide(5.0f));
                    }

                    break;
            }
        }
    }

    IEnumerator ShowAndHide(float delay)
    {
        MeshRendereEnabledStateChange(_meshRenderers, true);
        yield return new WaitForSeconds(delay);
        MeshRendereEnabledStateChange(_meshRenderers, false);
        EventActive = false;
        yield return new WaitForSeconds(delay - 2);


    }

    void MeshRendereEnabledStateChange(MeshRenderer[] meshRendererArray, bool state)
    {
        foreach (var meshRenderer in meshRendererArray)
        {
            meshRenderer.enabled = state;
        }
    }
}
