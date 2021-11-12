using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer musicMixer;
    public AudioMixer sfxMixer;
    public AudioSource menuSfx;
    
    public void SetMusicLevel(float slider)
    {
        musicMixer.SetFloat("volume", Mathf.Log10(slider) * 20);
    }

    public void SetSFXLevel(float slider)
    {
        sfxMixer.SetFloat("volume", Mathf.Log10(slider) * 20);
        if (!menuSfx.isPlaying)
        {
            menuSfx.Play();
        }
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }
}
