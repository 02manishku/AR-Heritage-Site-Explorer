using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Attach this to the same GameObject as ARPlacementManager or any manager object.
/// The placement manager will call SetPlacedObject(...) to pass the instantiated object to this script.
/// This script implements:
///  - One-finger drag on a plane to move the object.
///  - Two-finger pinch to scale.
///  - Two-finger twist (rotation) to rotate.
/// It also re-anchors object on move so ARAnchorManager can keep it stable.
/// </summary>
public class ARObjectManipulator : MonoBehaviour
{
    GameObject _placedObject;
    ARRaycastManager _raycastManager;
    ARAnchorManager _anchorManager;
    ARAnchor _currentAnchor;

    // scale limits
    public float minScale = 0.05f;
    public float maxScale = 5.0f;

    // smoothing
    public float moveSmooth = 10f;
    Vector3 _targetPosition;
    Quaternion _targetRotation;
    float _targetScale;

    static List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    public void SetPlacedObject(GameObject obj, ARRaycastManager raycastManager, ARAnchorManager anchorManager)
    {
        _placedObject = obj;
        _raycastManager = raycastManager;
        _anchorManager = anchorManager;
        _currentAnchor = obj.GetComponentInParent<ARAnchor>(); // may be null
        _targetPosition = _placedObject.transform.position;
        _targetRotation = _placedObject.transform.rotation;
        _targetScale = _placedObject.transform.localScale.x;
    }

    public void ClearPlacedObject()
    {
        _placedObject = null;
        _currentAnchor = null;
    }

    void Update()
    {
        if (_placedObject == null) return;
        if (Input.touchCount == 0)
        {
            // smooth-transform towards targets
            _placedObject.transform.position = Vector3.Lerp(_placedObject.transform.position, _targetPosition, Time.deltaTime * moveSmooth);
            _placedObject.transform.rotation = Quaternion.Slerp(_placedObject.transform.rotation, _targetRotation, Time.deltaTime * moveSmooth);
            _placedObject.transform.localScale = Vector3.Lerp(_placedObject.transform.localScale, Vector3.one * _targetScale, Time.deltaTime * moveSmooth);
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // single-finger drag: move the object along detected planes
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Began || t.phase == TouchPhase.Stationary)
            {
                if (_raycastManager.Raycast(t.position, _hits, TrackableType.Planes))
                {
                    var hitPose = _hits[0].pose;
                    _targetPosition = hitPose.position;

                    // if we have an ARAnchorManager, re-create anchor to attach object to the plane for better stability
                    if (_anchorManager != null)
                    {
                        ReanchorAtPose(hitPose);
                    }
                }
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // SCALE (pinch)
            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
                float curDist = (t0.position - t1.position).magnitude;
                float delta = curDist - prevDist;
                float scaleFactor = 1 + (delta * 0.0015f); // tweak sensitivity
                _targetScale = Mathf.Clamp(_targetScale * scaleFactor, minScale, maxScale);
            }

            // ROTATE (twist) - compute angle delta between two touches
            Vector2 prevDir = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            Vector2 curDir = (t0.position - t1.position);
            if (prevDir.sqrMagnitude > 0.0f && curDir.sqrMagnitude > 0.0f)
            {
                float anglePrev = Mathf.Atan2(prevDir.y, prevDir.x) * Mathf.Rad2Deg;
                float angleCur = Mathf.Atan2(curDir.y, curDir.x) * Mathf.Rad2Deg;
                float angleDelta = angleCur - anglePrev;

                // rotate around up axis (Y)
                _targetRotation = Quaternion.Euler(0f, _targetRotation.eulerAngles.y - angleDelta, 0f);
            }
        }

        // apply smoothing each frame (also applied in zero-touch branch)
        _placedObject.transform.position = Vector3.Lerp(_placedObject.transform.position, _targetPosition, Time.deltaTime * moveSmooth);
        _placedObject.transform.rotation = Quaternion.Slerp(_placedObject.transform.rotation, _targetRotation, Time.deltaTime * moveSmooth);
        _placedObject.transform.localScale = Vector3.Lerp(_placedObject.transform.localScale, Vector3.one * _targetScale, Time.deltaTime * moveSmooth);
    }

    void ReanchorAtPose(Pose pose)
    {
        // if current anchor exists and is near the new pose, keep it; otherwise recreate
        if (_currentAnchor != null)
        {
            float distance = Vector3.Distance(_currentAnchor.transform.position, pose.position);
            if (distance < 0.05f) // small threshold: keep same anchor
            {
                _targetPosition = pose.position;
                return;
            }
            else
            {
                Destroy(_currentAnchor.gameObject);
                _currentAnchor = null;
            }
        }

        // Try to attach to plane if present in raycast hits (the calling code already has hits)
        if (_anchorManager != null)
        {
            ARAnchor newAnchor = _anchorManager.AddAnchor(pose);
            if (newAnchor != null)
            {
                // reparent placed object under anchor to maintain tracking
                _placedObject.transform.SetParent(newAnchor.transform, true);
                _currentAnchor = newAnchor;
            }
        }

        _targetPosition = pose.position;
    }
}
