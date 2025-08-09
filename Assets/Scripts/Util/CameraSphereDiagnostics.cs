using UnityEngine;

public class CameraSphereDiagnostics : MonoBehaviour
{
    public Camera targetCamera;            // Drag Main Camera here
    public MeshRenderer sphereRenderer;    // Drag the 360 sphere's MeshRenderer here

    void Update()
    {
        string log = "[Diagnostics] ";

        if (targetCamera == null)
        {
            Debug.LogError("[Diagnostics] Target Camera is NOT assigned!");
            return;
        }
        else
        {
            log += $"Cam Pos: {targetCamera.transform.position} | Rot: {targetCamera.transform.eulerAngles} | ";
        }

        if (sphereRenderer == null)
        {
            Debug.LogError("[Diagnostics] Sphere Renderer is NOT assigned!");
            return;
        }
        else
        {
            Vector3 sphereCenter = sphereRenderer.bounds.center;
            float dist = Vector3.Distance(targetCamera.transform.position, sphereCenter);
            bool inFrustum = GeometryUtility.TestPlanesAABB(
                GeometryUtility.CalculateFrustumPlanes(targetCamera),
                sphereRenderer.bounds
            );
            bool layerVisible = (targetCamera.cullingMask & (1 << sphereRenderer.gameObject.layer)) != 0;

            log += $"Sphere Center: {sphereCenter} | Dist: {dist:F2} units | " +
                   $"In View Frustum: {inFrustum} | Layer Visible: {layerVisible} | " +
                   $"Sphere Scale: {sphereRenderer.transform.localScale}";
        }

        Debug.Log(log);
    }
}
