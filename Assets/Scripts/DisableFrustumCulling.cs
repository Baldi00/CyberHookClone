using UnityEngine;

public class DisableFrustumCulling : MonoBehaviour
{
    private Camera thisCamera;
    public void Start()
    {
        thisCamera = GetComponent<Camera>();
        thisCamera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                            thisCamera.worldToCameraMatrix;
    }
}