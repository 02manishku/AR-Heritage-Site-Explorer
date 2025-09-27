using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    [Header("Placement")]
    public GameObject placementIndicator;    // optional
    public GameObject placeablePrefab;       // assign your Peace_Pagoda prefab

    [Header("Audio (narration)")]
    public AudioClip narrationClip;          // assign audio file
    public bool playOnMove = false;          // restart narration if object moved after initial placement

    [Header("Spatial / Distance Settings")]
    [Tooltip("Distance at which audio is at full volume (meters).")]
    public float minDistance = 0.5f;
    [Tooltip("Distance beyond which audio will auto-pause (meters).")]
    public float maxDistance = 20f;
    [Tooltip("Master volume (0..1)")]
    [Range(0f, 1f)]
    public float narrationVolume = 1f;
    [Tooltip("Fade time when pausing/resuming (seconds)")]
    public float fadeTime = 0.4f;
    public bool autoPauseByDistance = true;  // toggle auto pause/resume

    // internal
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private GameObject spawnedObject;
    private AudioSource spawnedAudioSource;
    private ARAnchor currentAnchor; // <-- hold the anchor for stability

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // fade state
    float targetVolume = 0f;
    float currentFadeVelocity = 0f;
    bool isFading = false;

    void Awake()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
        anchorManager = FindObjectOfType<ARAnchorManager>();

        if (raycastManager == null)
            Debug.LogError("[ARPlacementManager] No ARRaycastManager found in scene! Add ARRaycastManager to XR Origin.");
        if (anchorManager == null)
            Debug.LogWarning("[ARPlacementManager] No ARAnchorManager found in scene. Anchors will fallback to AddAnchor which may be less stable on some devices.");
    }

    void Update()
    {
        if (raycastManager == null) return;

        UpdatePlacementPose();
        UpdatePlacementIndicator();

        // handle touch placement / move
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!placementPoseIsValid)
            {
                Debug.Log("[ARPlacementManager] Tap but no valid placement pose.");
                return;
            }

            if (placeablePrefab == null)
            {
                Debug.LogError("[ARPlacementManager] placeablePrefab is NOT assigned in inspector!");
                return;
            }

            if (spawnedObject == null)
            {
                SpawnPlacedObjectWithAnchor();
            }
            else
            {
                // Move existing object: recreate anchor at new pose and reparent object to it
                ReanchorAndMove(placementPose);
                Debug.Log("[ARPlacementManager] Re-anchored and moved object to " + placementPose.position);

                RestartNarration();
               }
        }

        // If audio exists, handle distance-based auto pause/resume + fade
        if (spawnedAudioSource != null && narrationClip != null)
        {
            HandleDistanceAndFading();
        }
    }

    void SpawnPlacedObjectWithAnchor()
    {
        // Create anchor then instantiate or parent prefab under it
        ARAnchor anchor = CreateAnchorAtPose(placementPose);
        if (anchor != null)
        {
            currentAnchor = anchor;
            spawnedObject = Instantiate(placeablePrefab, anchor.transform);
            spawnedObject.transform.localPosition = Vector3.zero;
            spawnedObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // fallback: instantiate without anchor (less stable)
            spawnedObject = Instantiate(placeablePrefab, placementPose.position, placementPose.rotation);
        }

        Debug.Log("[ARPlacementManager] Spawned object at " + (currentAnchor != null ? currentAnchor.transform.position : placementPose.position));
        SetupAndPlayNarration();
    }

    ARAnchor CreateAnchorAtPose(Pose pose)
    {
        if (anchorManager == null)
        {
            // try fallback AddAnchor via ARAnchorManager if missing, or return null
            Debug.LogWarning("[ARPlacementManager] AnchorManager missing — attempting AddAnchor fallback.");
            return null;
        }

        // If the latest raycast hit has a plane trackable, attach to plane for better stability
        // Note: we used hits in UpdatePlacementPose; here we will raycast again to be safe.
        hits.Clear();
        if (raycastManager.Raycast(Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f)), hits, TrackableType.PlaneWithinPolygon))
        {
            var hit = hits[0];
            var trackableId = hit.trackableId;
            var plane = anchorManager.subsystem == null ? null : FindPlaneById(trackableId);
            if (plane != null)
            {
                // Attach anchor to that plane
                var attachedAnchor = anchorManager.AttachAnchor(plane, pose);
                if (attachedAnchor != null)
                    return attachedAnchor;
            }
        }

        // Fallback: simply add an anchor (not plane-attached)
        ARAnchor fallback = anchorManager.AddAnchor(pose);
        return fallback;
    }

    // Helper to find ARPlane by id (safe null checks)
    ARPlane FindPlaneById(TrackableId id)
    {
        var planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null) return null;
        ARPlane plane;
        planeManager.trackables.TryGetTrackable(id, out plane);
        return plane;
    }

    void ReanchorAndMove(Pose newPose)
    {
        // Destroy previous anchor if exists
        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);
            currentAnchor = null;
        }

        // Create new anchor at new pose
        ARAnchor newAnchor = CreateAnchorAtPose(newPose);
        if (newAnchor != null)
        {
            currentAnchor = newAnchor;
            // reparent spawned object under new anchor while preserving local transform
            spawnedObject.transform.SetParent(newAnchor.transform, true);
            // snap to anchor origin (optional)
            spawnedObject.transform.localPosition = Vector3.zero;
            spawnedObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // fallback: move transform directly (should be rare)
            spawnedObject.transform.SetPositionAndRotation(newPose.position, newPose.rotation);
        }
    }

    void SetupAndPlayNarration()
    {
        if (narrationClip == null) return;

        // Try to reuse existing AudioSource if prefab had one
        spawnedAudioSource = spawnedObject.GetComponent<AudioSource>();
        if (spawnedAudioSource == null)
        {
            spawnedAudioSource = spawnedObject.AddComponent<AudioSource>();
        }

        // Configure spatial audio settings
        spawnedAudioSource.clip = narrationClip;
        spawnedAudioSource.spatialBlend = 1.0f;            // 3D sound
        spawnedAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        spawnedAudioSource.minDistance = Mathf.Max(0.01f, minDistance);
        spawnedAudioSource.maxDistance = Mathf.Max(spawnedAudioSource.minDistance + 0.1f, maxDistance);
        spawnedAudioSource.playOnAwake = false;
        spawnedAudioSource.loop = false;
        spawnedAudioSource.volume = 0f;                    // start silent for fade-in
        spawnedAudioSource.dopplerLevel = 0f;
        spawnedAudioSource.priority = 128;

        targetVolume = narrationVolume;
        StartFadeIn();

        spawnedAudioSource.Play();
        Debug.Log("[ARPlacementManager] Playing narration (spatial).");
    }

    void RestartNarration()
    {
        if (spawnedAudioSource == null || narrationClip == null) return;

        spawnedAudioSource.Stop();
        spawnedAudioSource.time = 0f;
        spawnedAudioSource.Play();
        targetVolume = narrationVolume;
        StartFadeIn();
        Debug.Log("[ARPlacementManager] Restarted narration on move.");
    }

    void StartFadeIn()
    {
        isFading = true;
        targetVolume = narrationVolume;
    }

    void StartFadeOut()
    {
        isFading = true;
        targetVolume = 0f;
    }

    void HandleDistanceAndFading()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float dist = Vector3.Distance(cam.transform.position, spawnedObject.transform.position);

        if (autoPauseByDistance)
        {
            if (dist > maxDistance)
            {
                if (spawnedAudioSource.isPlaying && targetVolume != 0f)
                {
                    StartFadeOut();
                }
            }
            else
            {
                if (!spawnedAudioSource.isPlaying)
                {
                    spawnedAudioSource.Play();
                }
                targetVolume = narrationVolume;
                isFading = true;
            }
        }

        if (isFading)
        {
            if (fadeTime <= 0.0001f)
            {
                spawnedAudioSource.volume = targetVolume;
                isFading = false;
            }
            else
            {
                float newVol = Mathf.SmoothDamp(spawnedAudioSource.volume, targetVolume, ref currentFadeVelocity, fadeTime);
                spawnedAudioSource.volume = newVol;

                if (Mathf.Abs(spawnedAudioSource.volume - targetVolume) < 0.005f)
                {
                    spawnedAudioSource.volume = targetVolume;
                    isFading = false;
                    if (Mathf.Approximately(targetVolume, 0f) && spawnedAudioSource.isPlaying)
                    {
                        spawnedAudioSource.Pause();
                        Debug.Log("[ARPlacementManager] Narration paused due to distance.");
                    }
                }
            }
        }

        spawnedAudioSource.minDistance = Mathf.Max(0.01f, minDistance);
        spawnedAudioSource.maxDistance = Mathf.Max(spawnedAudioSource.minDistance + 0.1f, maxDistance);
    }

    void UpdatePlacementIndicator()
    {
        if (placementIndicator == null) return;

        if (placementPoseIsValid)
        {
            if (!placementIndicator.activeInHierarchy) placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            if (placementIndicator.activeInHierarchy) placementIndicator.SetActive(false);
        }
    }

    void UpdatePlacementPose()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            placementPoseIsValid = false;
            return;
        }

        Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        hits.Clear();
        raycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;
            // make it face camera horizontally
            Vector3 cameraForward = cam.transform.forward;
            Vector3 cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            if (cameraBearing.sqrMagnitude > 0.001f)
                placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }
}
