using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BagBurst : MonoBehaviour
{
    [Header("Litter")]
    public GameObject[] litterPrefabs;
    public SweepMotion cleaner;
    public Transform truckRoot;

    [Header("Audio")]
    public AudioClip impactClip;
    public AudioClip burstClip;

    [Range(0f, 1f)]
    public float impactVolume = 0.8f;

    [Range(0f, 1f)]
    public float burstVolume = 0.5f;

    [Header("Burst Settings")]
    public int litterCount = 8;
    public float litterScale = 1f;
    public float spreadRadius = 0.8f;
    public float hopHeight = 0.2f;
    public float hopTime = 0.35f;
    public float cleanerDelay = 1.5f;

    private bool armed;
    private bool burst;
    private Coroutine armRoutine;

    public void Arm()
    {
        if (armed || burst)
            return;

        if (armRoutine != null)
            StopCoroutine(armRoutine);

        armRoutine = StartCoroutine(ArmLater());
    }

    IEnumerator ArmLater()
    {
        yield return new WaitForSeconds(0.3f);

        armed = true;
        armRoutine = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!armed || burst)
            return;

        if (truckRoot &&
            (collision.transform == truckRoot ||
             collision.transform.IsChildOf(truckRoot)))
        {
            return;
        }

        burst = true;

        if (impactClip)
        {
            AudioSource.PlayClipAtPoint(
                impactClip,
                transform.position,
                impactVolume
            );
        }

        StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        List<GameObject> litterItems = new List<GameObject>();

        
        Rigidbody bagRb = GetComponent<Rigidbody>();

        if (bagRb)
        {
            bagRb.velocity = Vector3.zero;
            bagRb.angularVelocity = Vector3.zero;
            bagRb.isKinematic = true;
        }

        Vector3 center = transform.position;

        Vector3 originalScale = transform.localScale;

        Vector3 flattenedScale = new Vector3(
            originalScale.x * 1.4f,
            originalScale.y * 0.35f,
            originalScale.z * 1.4f
        );

        float flattenDuration = 0.25f;
        float elapsed = 0f;

        while (elapsed < flattenDuration)
        {
            float t = elapsed / flattenDuration;

            transform.localScale = Vector3.Lerp(
                originalScale,
                flattenedScale,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = flattenedScale;

        if (burstClip)
        {
            AudioSource.PlayClipAtPoint(
                burstClip,
                center,
                burstVolume
            );
        }

        if (
            litterPrefabs.Length > 0 &&
            litterCount > 0)
        {
            for (int i = 0; i < litterCount; i++)
            {
                GameObject prefab =
                    litterPrefabs[i % litterPrefabs.Length];

                if (!prefab)
                    continue;

                float angle = 360f / litterCount * i;

                float radius = spreadRadius *
                    (0.5f + 0.25f * (i % 3));

                Vector3 offset =
                    Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward * radius;

                Vector3 target = center + offset;

                if (Physics.Raycast(
                    target + Vector3.up * 2f,
                    Vector3.down,
                    out RaycastHit hit,
                    6f))
                {
                    target = hit.point + Vector3.up * 0.02f;
                }

                Vector3 spawnPosition =
                    center + Vector3.up * 0.15f;

                GameObject litter = Instantiate(
                    prefab,
                    spawnPosition,
                    Quaternion.Euler(0f, angle * 1.7f, 0f)
                );

                litter.tag = "Trash";

                litter.transform.localScale *= litterScale;
                
                Rigidbody litterRb =
                    litter.GetComponent<Rigidbody>();

                if (litterRb)
                {
                    litterRb.velocity = Vector3.zero;
                    litterRb.angularVelocity = Vector3.zero;
                    litterRb.isKinematic = true;
                }

                litterItems.Add(litter);

                StartCoroutine(
                    Hop(litter.transform, spawnPosition, target)
                );
            }
        }

        yield return new WaitForSeconds(hopTime + cleanerDelay);

gameObject.tag = "Trash";
litterItems.Add(gameObject);

        if (cleaner && litterItems.Count > 0)
        {
            cleaner.OnLitterDropped(litterItems);
        }

    }

    IEnumerator Hop(Transform litter, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < hopTime)
        {
            if (!litter)
                yield break;

            float t = hopTime > 0f
                ? elapsed / hopTime
                : 1f;

            Vector3 position = Vector3.Lerp(start, end, t);

            position.y += Mathf.Sin(t * Mathf.PI) * hopHeight;

            litter.position = position;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (litter)
            litter.position = end;
    }
}