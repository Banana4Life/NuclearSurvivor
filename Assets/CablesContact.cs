using UnityEngine;

public class CablesContact : MonoBehaviour
{
    private Follower _fried;

    public bool Fry(Follower follower)
    {
        if (_fried)
        {
            return false;
        }

        _fried = follower;
        return true;
    }
}
