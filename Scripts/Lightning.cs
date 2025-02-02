// Adeer Khan 20244046
// Followed Tutorial: Creating Lightning in your Unity Projects (https://www.youtube.com/watch?v=hCP5w5vTsDc)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Lightning : MonoBehaviour {


    public float offmin = 10f;
    public float offMax = 60f;
    public float onMin = 0.25f;
    public float onMax = 0.8f;

    public float lightIntensity = 5f;
    public bool startLightning = false;

    public Light light;
    public AudioClip[] thunderSounds;
    public AudioSource audioSource;





	void Start () {
        StartCoroutine("LightningFX"); 
	}
	
    IEnumerator LightningFX()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(offmin, offMax));
            light.enabled = true;

            
            light.color = new Color(1f, 1f, 1f);
            light.intensity = lightIntensity; 
            StartCoroutine("SoundFX");

            yield return new WaitForSeconds(Random.Range(0.05f, 0.25f));

            for (float t = 0f; t < 1f; t += Time.deltaTime / Random.Range(0.25f, 0.5f))
            {
                light.intensity = Mathf.Lerp(lightIntensity, 1f, t);
                yield return null;
            }

            light.enabled = false;
        }
    }


    IEnumerator SoundFX()
    {
        yield return new WaitForSeconds(Random.Range(.25f, 1.75f));
        audioSource.PlayOneShot(thunderSounds[Random.Range(0, thunderSounds.Length)]); 
    }

    void Update()
    {
    if (startLightning)
    {
        startLightning = false;
        StartCoroutine("LightningFX");
    }
    }

}
