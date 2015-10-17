using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {

	public AudioClip glyphAudioClip;
	AudioSource glyph;

	void Awake()
	{
		glyph = AddAudio(glyphAudioClip);
	}

	AudioSource AddAudio(AudioClip audioClip)
	{
		AudioSource audioSource = this.addgameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.clip = audioClip;
		return audioSource;
	}

	public void PlayGlyph()
	{
		glyph.Play();
	}
}
