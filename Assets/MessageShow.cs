using System.Collections;
using UnityEngine;

public class MessageShow : MonoBehaviour
{
    public float distance = 3f;
    public float heightOffset = 0f;
    public float fadeTime = 1f;
    public float showTime = 8f;
    public AudioClip voiceClip;
    [Range(0, 1)] public float voiceVolume = 0.9f;

    CanvasGroup group;
    bool shownOnce;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (Time.timeSinceLevelLoad < 2f)
        {
            gameObject.SetActive(false);
            return;
        }
        if (shownOnce) return;
        shownOnce = true;

        var cam = Camera.main;
        if (cam)
        {
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(fwd);
            if (voiceClip)
                AudioSource.PlayClipAtPoint(voiceClip, cam.transform.position, voiceVolume);
        }
        StartCoroutine(Show());
    }

    IEnumerator Show()
    {
        float hold = showTime;
        if (voiceClip) hold = Mathf.Max(showTime, voiceClip.length + 1f);

        group.alpha = 0f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = t / fadeTime;
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSeconds(hold);

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        group.alpha = 0f;
        gameObject.SetActive(false);
    }
}