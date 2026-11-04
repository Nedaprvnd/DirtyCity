using UnityEngine;

public class TruckScenario : MonoBehaviour
{
    public Transform bag;
    public AudioClip engineClip;
    [Range(0, 1)] public float engineVolume = 0.4f;
    public float engineFadeStart = 6f;
    public float engineFadeTime = 3f;
    public Vector3 moveDirection = new Vector3(-1, 0, 0);
    public float speed = 3f;
    public float dropAfter = 4f;
    public float stopAfter = 10f;

    float timer;
    bool dropped;
    AudioSource engine;

    void Start()
    {
        if (engineClip)
        {
            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = engineClip;
            engine.loop = true;
            engine.playOnAwake = false;
            engine.volume = engineVolume;
            engine.spatialBlend = 1f;
            engine.minDistance = 3f;
            engine.maxDistance = 40f;
            engine.Play();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < stopAfter)
            transform.Translate(moveDirection.normalized * speed * Time.deltaTime, Space.World);

        if (engine && timer >= engineFadeStart)
        {
            float k = Mathf.Clamp01((timer - engineFadeStart) / engineFadeTime);
            engine.volume = engineVolume * (1f - k);
            if (k >= 1f && engine.isPlaying) engine.Stop();
        }

        if (!dropped && timer >= dropAfter)
        {
            dropped = true;
            DropBag();
        }
    }

    void DropBag()
    {
        bag.SetParent(null);
        var rb = bag.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(-moveDirection.normalized * 1f + Vector3.up * 1f, ForceMode.VelocityChange);

        var burst = bag.GetComponent<BagBurst>();
        if (burst) burst.Arm();
    }
}