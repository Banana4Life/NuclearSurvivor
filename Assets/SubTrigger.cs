using UnityEngine;

public class SubTrigger : MonoBehaviour
{
    private LevelLoaderTrigger mainTrigger;

    public void Init(LevelLoaderTrigger mainTrigger, Vector3 pos)
    {
        this.mainTrigger = mainTrigger;
        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        mainTrigger.triggerEnter(other);
        
        
    }

}
