using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SweepMotion : MonoBehaviour
{
    [Header("Character")]
    public Animator animator;
    public UpperBodySway sway;

    [Header("Scene UI")]
    public GameObject messagePanel;

    [Header("Audio")]
    public AudioClip footstepsClip;
    public AudioClip sweepingClip;

    [Range(0f, 1f)]
    public float footstepsVolume = 0.4f;

    [Range(0f, 1f)]
    public float sweepingVolume = 0.6f;

    [Header("Broom")]
    public Transform broom;
    public float broomAngle = 0f;
    public float broomFrequency = 3f;
    public float sweepDuration = 8f;
    public bool sweepAtStart = true;

    [Header("Movement")]
    public float standDistance = 1.5f;
    public float walkSpeed = 1.4f;
    public float turnSpeed = 5f;
    public float reactDelay = 1.5f;

    [Header("Cleaning")]
    public float cleanerDelay = 0.5f;
    public float trashDisappearTime = 0.5f;

    Vector3 startPos;
    Quaternion startRot;
    Quaternion broomStartRotation;

    AudioSource steps;
    AudioSource sfx;

    bool isCleaning;
    bool sweeping;
    float sweepTime;
    public bool IsSweeping => sweeping;

    HashSet<GameObject> cleaningTargets =
        new HashSet<GameObject>();

    
    HashSet<GameObject> cleanedTrash =
        new HashSet<GameObject>();

    void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (!sway)
            sway = GetComponentInChildren<UpperBodySway>();

        startPos = transform.position;
        startRot = transform.rotation;

        steps = gameObject.AddComponent<AudioSource>();
        steps.clip = footstepsClip;
        steps.loop = true;
        steps.playOnAwake = false;
        steps.volume = footstepsVolume;
        steps.spatialBlend = 1f;
        steps.minDistance = 2f;
        steps.maxDistance = 25f;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.clip = sweepingClip;
        sfx.loop = true;
        sfx.playOnAwake = false;
        sfx.volume = sweepingVolume;
        sfx.spatialBlend = 1f;
        sfx.minDistance = 2f;
        sfx.maxDistance = 25f;

        if (broom)
            broomStartRotation = broom.localRotation;
    }

    void Start()
    {
        if (sweepAtStart)
            SetSweeping(true);
    }

    void Update()
    {
        if (sweeping && broom && broomAngle != 0f)
        {
            sweepTime += Time.deltaTime;

            float a = Mathf.Sin(
                sweepTime * broomFrequency
            ) * broomAngle;

            broom.localRotation =
                broomStartRotation *
                Quaternion.Euler(0f, a, 0f);
        }
    }

    void SetSweeping(bool on)
    {
        sweeping = on;

        if (sway)
            sway.active = on;

        if (sfx && sweepingClip)
        {
            if (on && !sfx.isPlaying)
                sfx.Play();

            if (!on && sfx.isPlaying)
                sfx.Stop();
        }

        if (!on && broom)
            broom.localRotation = broomStartRotation;
    }

    void SetWalking(bool walking)
    {
        if (animator && HasParam(animator, "Walking"))
            animator.SetBool("Walking", walking);

        if (!footstepsClip)
            return;

        if (walking && !steps.isPlaying)
            steps.Play();

        if (!walking && steps.isPlaying)
            steps.Stop();
    }

    bool HasParam(Animator a, string n)
    {
        foreach (var p in a.parameters)
        {
            if (p.name == n &&
                p.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }

    public void OnLitterDropped(List<GameObject> items)
    {
        if (isCleaning || items == null || items.Count == 0)
            return;

        StartCoroutine(
            CleaningRoutine(new List<GameObject>(items))
        );
    }

    public void TryCleanTrash(GameObject trash)
    {
        if (!isCleaning || !sweeping || !trash)
            return;

        if (!cleaningTargets.Contains(trash))
            return;

        if (cleanedTrash.Contains(trash))
            return;

        cleanedTrash.Add(trash);
        cleaningTargets.Remove(trash);

        Collider[] colliders =
            trash.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.enabled = false;

        Rigidbody rb = trash.GetComponent<Rigidbody>();

        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        StartCoroutine(ShrinkAndDestroy(trash));
    }

    
IEnumerator CleaningRoutine(List<GameObject> items)
{
    isCleaning = true;

    cleaningTargets.Clear();
    cleanedTrash.Clear();

    if (messagePanel)
        messagePanel.SetActive(false);

    Vector3 center = Vector3.zero;
    int count = 0;

    foreach (var item in items)
    {
        if (!item)
            continue;

        cleaningTargets.Add(item);
        center += item.transform.position;
        count++;
    }

    if (count == 0)
    {
        isCleaning = false;
        yield break;
    }

    center /= count;

    yield return new WaitForSeconds(reactDelay);

    Vector3 away = startPos - center;
    away.y = 0f;

    if (away.sqrMagnitude < 0.01f)
        away = Vector3.back;

    Vector3 stand = center + away.normalized * standDistance;
    stand.y = transform.position.y;

    SetSweeping(false);
    yield return new WaitForSeconds(0.4f);

    SetWalking(true);
    yield return WalkTo(stand);
    SetWalking(false);

    Vector3 look = center - transform.position;
    look.y = 0f;

    if (look.sqrMagnitude > 0.01f)
        yield return TurnTo(Quaternion.LookRotation(look));

    yield return new WaitForSeconds(cleanerDelay);

    SetSweeping(true);


    List<GameObject> orderedItems = new List<GameObject>();

    foreach (var item in items)
    {
        if (item)
            orderedItems.Add(item);
    }

    foreach (var item in orderedItems)
    {
        if (!item)
            continue;

        cleaningTargets.Remove(item);
        cleanedTrash.Add(item);

        yield return StartCoroutine(ShrinkAndDestroy(item));

        yield return new WaitForSeconds(0.35f);
    }

    SetSweeping(false);

    yield return new WaitForSeconds(0.5f);

    SetWalking(true);
    yield return WalkTo(startPos);
    SetWalking(false);

    yield return TurnTo(startRot);

    if (messagePanel)
        messagePanel.SetActive(true);

    if (sweepAtStart)
        SetSweeping(true);

    isCleaning = false;
}


    IEnumerator ShrinkAndDestroy(GameObject o)
    {
        if (!o)
            yield break;

        Vector3 originalScale = o.transform.localScale;

        float duration = Mathf.Max(0.01f, trashDisappearTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!o)
                yield break;

            float t = elapsed / duration;

            o.transform.localScale =
                Vector3.Lerp(originalScale, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (o)
            Destroy(o);
    }

    IEnumerator TurnTo(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = target;
    }

    IEnumerator WalkTo(Vector3 destination)
    {
        while (true)
        {
            Vector3 d = destination - transform.position;
            d.y = 0f;

            if (d.magnitude < 0.2f)
                break;

            Quaternion look = Quaternion.LookRotation(d);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                look,
                turnSpeed * Time.deltaTime
            );

            transform.position +=
                d.normalized * walkSpeed * Time.deltaTime;

            yield return null;
        }

        Vector3 p = transform.position;
        p.x = destination.x;
        p.z = destination.z;

        transform.position = p;
    }
}
