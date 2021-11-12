using UnityEngine;

public class MeteorSfx : MonoBehaviour
{
    public AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnParticleCollision(GameObject other)
    {
        Debug.Log("Boom!");
        audioSource.PlayOneShot(audioSource.clip);
    }
}
