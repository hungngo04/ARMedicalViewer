using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class HandMenuSoundPlayer : MonoBehaviour
{
    AudioSource m_AudioSource;

    void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (m_AudioSource != null)
            m_AudioSource.Play();
    }

}
