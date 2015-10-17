using UnityEngine;

public class SoundManager : MonoBehaviour {

	public AudioClip glyphAudioClip;
	AudioSource glyph;

	void Awake()
	{
		glyph = AddAudio(glyphAudioClip);
	}

	AudioSource AddAudio(AudioClip audioClip)
	{
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.clip = audioClip;
		return audioSource;
	}

	public void PlayGlyph()
	{
		glyph.Play();
	}
}
