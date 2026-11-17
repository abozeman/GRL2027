using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LobbyTrackPlacementManager : MonoBehaviour
{
    public event Action OnPlacementSpotSelected;
    public enum PlacementSortMode
    {
        LargestArea,
        ClosestDistance,
        OptimalHybrid // Balances large area with close proximity
    }

    [Header("MRUK & Preview Setup")]
    public GameObject trackPreviewPrefab;
    public MRUKAnchor.SceneLabels validLabels = MRUKAnchor.SceneLabels.TABLE | MRUKAnchor.SceneLabels.FLOOR;
    public float surfaceClearanceDistance = 0.05f;
    public float previewScale = 0.25f;

    [Header("Sorting Logic")]
    public PlacementSortMode currentSortMode = PlacementSortMode.OptimalHybrid;

    [Tooltip("Maximum angle (degrees) off-center to be considered 'in view'")]
    public float maxFieldOfViewAngle = 75f;

    private List<GameObject> _spawnedPreviews = new List<GameObject>();
    private List<Transform> _candidateAnchors = new List<Transform>();
    private int _selectedIndex = -1;

    // Helper class to store anchor metrics for sorting
    private class TrackSpotCandidate
    {
        public MRUKAnchor Anchor;
        public Vector3 SurfacePos;
        public Vector3 Normal;
        public float Area;
        public float DistanceToUser;
        public float HybridScore;
    }

    private void OnEnable()
    {
        if (MRUK.Instance != null)
            MRUK.Instance.RoomCreatedEvent.AddListener(PopulateValidTrackSpots);
    }

    private void OnDisable()
    {
        if (MRUK.Instance != null)
            MRUK.Instance.RoomCreatedEvent.RemoveListener(PopulateValidTrackSpots);
    }

    public void PopulateValidTrackSpots(MRUKRoom room)
    {
        ClearPreviews();
        if (room == null) room = MRUK.Instance.GetCurrentRoom();
        if (room == null) return;

        var prefabBounds = Utilities.GetPrefabBounds(trackPreviewPrefab);
        float baseOffset = -prefabBounds?.min.y ?? 0.0f;
        float centerOffset = prefabBounds?.center.y ?? 0.0f;
        LabelFilter filter = LabelFilter.FromEnum(validLabels);

        Transform headset = Camera.main.transform;
        List<TrackSpotCandidate> validCandidates = new List<TrackSpotCandidate>();

        // 1. GATHER AND MEASURE ALL VALID ANCHORS
        foreach (MRUKAnchor anchor in room.Anchors)
        {
            if (!filter.PassesFilter(anchor.Label)) continue;

            Vector3 surfacePos = anchor.GetAnchorCenter();
            Vector3 surfaceNormal = anchor.transform.up;

            if (anchor.VolumeBounds.HasValue)
            {
                surfacePos += surfaceNormal * (anchor.VolumeBounds.Value.size.y * 0.5f);
            }

            // Field of View Check - Discard surfaces completely behind the user
            Vector3 directionToSurface = (surfacePos - headset.position).normalized;
            if (Vector3.Angle(headset.forward, directionToSurface) > maxFieldOfViewAngle)
            {
                continue;
            }

            Vector3 spawnPosition = surfacePos + surfaceNormal * baseOffset;
            Vector3 center = spawnPosition + surfaceNormal * centerOffset;

            // Clearance checks
            if (!room.IsPositionInRoom(center)) continue;
            if (room.IsPositionInSceneVolume(center)) continue;
            if (room.Raycast(new Ray(surfacePos + surfaceNormal * 0.01f, surfaceNormal), surfaceClearanceDistance, out _)) continue;

            // Calculate Area
            float area = 0f;
            if (anchor.PlaneRect.HasValue)
                area = anchor.PlaneRect.Value.width * anchor.PlaneRect.Value.height;
            else if (anchor.VolumeBounds.HasValue)
                area = anchor.VolumeBounds.Value.size.x * anchor.VolumeBounds.Value.size.z;

            // Calculate Distance
            float distance = Vector3.Distance(headset.position, surfacePos);

            validCandidates.Add(new TrackSpotCandidate
            {
                Anchor = anchor,
                SurfacePos = spawnPosition,
                Normal = surfaceNormal,
                Area = area,
                DistanceToUser = distance
            });
        }

        if (validCandidates.Count == 0) return;

        // 2. SORT THE CANDIDATES
        validCandidates = SortCandidates(validCandidates);

        // 3. SPAWN PREVIEWS IN SORTED ORDER
        for (int i = 0; i < validCandidates.Count; i++)
        {
            var candidate = validCandidates[i];

            GameObject anchorTarget = new GameObject($"TrackAnchorSpot_{candidate.Anchor.Label}_{i}");
            anchorTarget.transform.SetParent(candidate.Anchor.transform, true);
            anchorTarget.transform.position = candidate.SurfacePos;
            //anchorTarget.transform.rotation = Quaternion.LookRotation(candidate.Anchor.transform.forward, candidate.Normal);

            GameObject previewObj = Instantiate(trackPreviewPrefab, anchorTarget.transform.position, anchorTarget.transform.rotation);
            previewObj.transform.localScale = Vector3.one * previewScale;
            previewObj.transform.SetParent(room.transform, true);

            var selector = previewObj.AddComponent<TrackSpotPreviewItem>();
            selector.Initialize(i, this);

            _candidateAnchors.Add(anchorTarget.transform);
            _spawnedPreviews.Add(previewObj);
        }

        // Auto-select the #1 best option based on the sort
        SelectPlacementSpot(0);
    }

    private List<TrackSpotCandidate> SortCandidates(List<TrackSpotCandidate> candidates)
    {
        switch (currentSortMode)
        {
            case PlacementSortMode.LargestArea:
                // Descending by Area
                return candidates.OrderByDescending(c => c.Area).ToList();

            case PlacementSortMode.ClosestDistance:
                // Ascending by Distance
                return candidates.OrderBy(c => c.DistanceToUser).ToList();

            case PlacementSortMode.OptimalHybrid:
                // Hybrid Score: We want high area and low distance. 
                // Normalize area and distance to combine them fairly.
                float maxArea = candidates.Max(c => c.Area);
                float maxDist = candidates.Max(c => c.DistanceToUser);

                // Prevent divide by zero
                if (maxArea == 0) maxArea = 1;
                if (maxDist == 0) maxDist = 1;

                foreach (var c in candidates)
                {
                    float normalizedArea = c.Area / maxArea; // 1.0 is biggest
                    float normalizedDist = 1.0f - (c.DistanceToUser / maxDist); // 1.0 is closest

                    // You can weight these. Here, they are weighted equally (50/50).
                    c.HybridScore = (normalizedArea * 0.5f) + (normalizedDist * 0.5f);
                }

                // Descending by Hybrid Score
                return candidates.OrderByDescending(c => c.HybridScore).ToList();

            default:
                return candidates;
        }
    }

    public void SelectPlacementSpot(int index)
    {
        if (index < 0 || index >= _candidateAnchors.Count) return;
        _selectedIndex = index;

        for (int i = 0; i < _spawnedPreviews.Count; i++)
        {
            var highlight = _spawnedPreviews[i].GetComponent<PreviewHighlight>();
            if (highlight != null) highlight.ToggleHighlight(i == _selectedIndex);
        }

        if (LocalXRPersistence.Instance != null)
        {
            LocalXRPersistence.Instance.SetTargetTrackAnchor(_candidateAnchors[_selectedIndex]);
        }

        OnPlacementSpotSelected?.Invoke();
        //HidePreviewsForGameTransition();
    }

    public void HidePreviewsForGameTransition()
    {
        foreach (var preview in _spawnedPreviews)
            if (preview != null) preview.SetActive(false);
    }

    private void ClearPreviews()
    {
        foreach (var preview in _spawnedPreviews)
            if (preview != null) Destroy(preview);
        _spawnedPreviews.Clear();
        _candidateAnchors.Clear();
        _selectedIndex = -1;
    }
}