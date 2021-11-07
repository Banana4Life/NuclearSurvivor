using UnityEngine;

public class Player : MonoBehaviour
{
    public IrradiationEffect radiationLight;

    public float irradiated;

    private void Update()
    {
        irradiated -= Time.deltaTime;
        if (irradiated < 0)
        {
            radiationLight.Deactivate();
        }
    }

    public void SetIrradiated()
    {
        irradiated = 15f;
        radiationLight.Activate();
    }
}
