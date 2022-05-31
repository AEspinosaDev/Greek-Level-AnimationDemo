using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformsBehaviour : MonoBehaviour
{
    [SerializeField] float m_TimeToWait;
    [SerializeField] List<GameObject> vfx;
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioSource sfx2;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitUntilActivate(m_TimeToWait));
    }

    IEnumerator WaitUntilActivate(float time)
    {
        yield return new WaitForSeconds(time);
        foreach (var item in vfx)
        {
            item.SetActive(true);
            sfx.Play();
            item.GetComponent<AudioSource>().Play();
            item.GetComponent<AudioSource>().volume = 2;
        }
        StartCoroutine(Activate(3f));
    }
    IEnumerator Activate(float time)
    {
        yield return new WaitForSeconds(time);
        GetComponent<Animator>().SetTrigger("ActivatePlatforms");

    }
    public static IEnumerator StartFade(float duration, float targetVolume,AudioSource sfx)
    {
        float currentTime = 0;
        float currentVol =sfx.volume;
        currentVol = Mathf.Pow(10, currentVol / 20);
        float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            sfx.volume = newVol;
            yield return null;
        }
        yield break;
    }
    public void StopSound()
    {
        StartCoroutine(StartFade(5, 0, sfx));
        sfx2.Play();
    }
}
