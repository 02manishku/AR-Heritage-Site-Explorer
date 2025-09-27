using UnityEngine;

public class TouchManipulation : MonoBehaviour {

    float prevDistance = 0f;
    float prevAngle = 0f;
    bool manipulating = false;

    void Update(){
        if(transform == null) return;

        if(Input.touchCount == 1){
            Touch t = Input.GetTouch(0);
            if(t.phase == TouchPhase.Moved){
                // Move the object along camera plane based on touch delta
                Vector3 delta = new Vector3(t.deltaPosition.x, t.deltaPosition.y, 0f);
                // scale movement by a factor depending on distance from camera
                float factor = 0.0015f * (Vector3.Distance(Camera.main.transform.position, transform.position));
                Vector3 worldDelta = Camera.main.transform.TransformDirection(delta * factor);
                transform.position += worldDelta;
            }
        } else if(Input.touchCount == 2){
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // Pinch (scale)
            float curDistance = Vector2.Distance(t0.position, t1.position);
            if(prevDistance > 0f){
                float diff = curDistance - prevDistance;
                float scaleFactor = 1f + diff * 0.001f;
                transform.localScale *= scaleFactor;
                // clamp scale
                float minS = 0.05f; float maxS = 10f;
                transform.localScale = Vector3.Max(Vector3.one * minS, Vector3.Min(transform.localScale, Vector3.one * maxS));
            }
            prevDistance = curDistance;

            // Rotate around Y axis based on angle between touches
            Vector2 dir = t1.position - t0.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if(prevAngle != 0f){
                float deltaAngle = Mathf.DeltaAngle(prevAngle, angle);
                transform.Rotate(Vector3.up, -deltaAngle, Space.World);
            }
            prevAngle = angle;
        } else {
            prevDistance = 0f;
            prevAngle = 0f;
        }
    }
}