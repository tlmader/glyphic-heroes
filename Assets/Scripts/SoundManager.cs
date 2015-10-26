using UnityEngine;

public class SoundManager : MonoBehaviour {

	public AudioClip startAudioClip, attackAudioClip, defendAudioClip, gameOverAudioClip;
	AudioSource start, attack, defend, gameOver;

	void Awake()
	{
		start = AddAudio(startAudioClip);
		attack = AddAudio(attackAudioClip);
		defend = AddAudio(defendAudioClip);
		gameOver = AddAudio(gameOverAudioClip);
	}

	AudioSource AddAudio(AudioClip audioClip)
	{
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.clip = audioClip;
		return audioSource;
	}

	public void PlayStart()
	{
		start.Play();
	}

	public void PlayAttack()
	{
		attack.Play();
	}

	public void PlayDefend()
	{
		defend.Play();
	}

	public void PlayGameOver()
	{
		gameOver.Play();
	}
}
