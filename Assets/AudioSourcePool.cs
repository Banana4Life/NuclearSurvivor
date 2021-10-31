using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioSourcePool : MonoBehaviour
{
    public ClipGroups[] clipGroups;

    public AudioMixerGroup mainMixerGroup;
    
    private Queue<AudioSource> free = new();
    private List<AudioSource> live = new();

    private GameObject pool;
    private void Start()
    {
        pool = new GameObject("AudioSourcePool");
        pool.transform.parent = transform;
    }

    void Update()
    {
        var freed = live.Where(a => !a.isPlaying).ToList();
        foreach (var audioSource in freed)
        {
            live.Remove(audioSource);
            free.Enqueue(audioSource);
            audioSource.gameObject.name = "AudioSource (Free)";
        }
    }

    public AudioSource PlaySound(ClipGroup clipGroup, Vector3 pos)
    {
        if (!free.TryDequeue(out AudioSource source))
        {
            var go = new GameObject("AudioSource");
            go.transform.parent = pool.transform;
            source = go.AddComponent<AudioSource>();
        }
        
        live.Add(source);

        var audioClipsList = clipGroups.Where(cl => cl.clipGroup == clipGroup).SelectMany(cl => cl.clips).ToList();
        var mixer = clipGroups.First(cl => cl.clipGroup == clipGroup).mixer;
        if (audioClipsList.Count == 0)
        {
            throw new Exception("Missing Sound for ClipGroup " + clipGroup);
        }
        source.PlayOneShot(audioClipsList[Random.Range(0, audioClipsList.Count)]);
        source.gameObject.name = "AudioSource " + clipGroup;
        source.transform.position = pos;
        source.outputAudioMixerGroup = mixer == null ? mainMixerGroup : mixer;
        return source;
    }
    
    
}

[Serializable]
public struct ClipGroups
{
    public ClipGroup clipGroup;
    public AudioMixerGroup mixer;
    public AudioClip[] clips;
}

public enum ClipGroup
{
    PEW1,
    DOUBLE_PEW,
    PROJ_HIT,
    DEATH
}
