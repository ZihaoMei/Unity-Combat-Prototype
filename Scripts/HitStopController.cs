using UnityEngine;
using System.Collections;

public class HitStopController : MonoBehaviour
{
    private static HitStopController instance;

    void Awake() { instance = this; }

    public static void TriggerHitStop(float duration, float scale)
    {
        instance.StartCoroutine(instance.DoHitStop(duration, scale));
    }

    private IEnumerator DoHitStop(float duration, float scale)
    {
        Time.timeScale = scale; 
        yield return new WaitForSecondsRealtime(duration); 
        Time.timeScale = 1.0f; 
    }
}