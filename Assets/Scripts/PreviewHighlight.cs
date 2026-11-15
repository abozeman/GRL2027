using UnityEngine;
using System.Collections.Generic;

public class PreviewHighlight : MonoBehaviour
{
    [Tooltip("HDR Color for the selected 'crazy green' glow")]
    [ColorUsage(true, true)]
    public Color activeGlowColor = new Color(0f, 1f, 0.2f, 1f) * 2f;

    [Tooltip("Dim/transparent color when not selected")]
    [ColorUsage(true, true)]
    public Color inactiveColor = new Color(0f, 0.3f, 0.1f, 0.4f);

    private List<Material> _materials = new List<Material>();

    private void Awake()
    {
        // Automatically finds the MeshRenderer on your child cube
        var cubeRenderer = GetComponentInChildren<MeshRenderer>();
        if (cubeRenderer != null)
        {
            _materials.AddRange(cubeRenderer.materials);
        }

        // Default to unselected state on spawn
        ToggleHighlight(false);
    }

    public void ToggleHighlight(bool isSelected)
    {
        foreach (var mat in _materials)
        {
            if (isSelected)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", activeGlowColor);
                mat.color = activeGlowColor; // Overrides base albedo color
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.color = inactiveColor;
            }
        }
    }
}