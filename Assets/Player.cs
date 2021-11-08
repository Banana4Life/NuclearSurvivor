using UnityEngine;

public class Player : MonoBehaviour
{
    public IrradiationEffect radiationLight;

    public GameObject followerPrefab;

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
        Instantiate(followerPrefab, transform.parent).GetComponent<Follower>().Init(this);
    }
}
