using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
    }

    void Update()
    {
        transform.forward = _mainCam.transform.forward;
    }
}
