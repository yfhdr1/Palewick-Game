using UnityEngine;
using Photon.Pun;
public class HeartbeatSfx : MonoBehaviour
{
    public float maxDistance = 40f;
    public float minDistance = 2f;
    public float minBpm = 60f;
    public float maxBpm = 170f;
    private AudioSource heartbeatAudioSource;
    private EnemyAI[] enemies;
    private float searchTimer;
    private float beatTimer;
    private AudioClip heartbeatClip;
    private PhotonView pv;
    private void Start()
    {
        pv = GetComponentInParent<PhotonView>();
        if (pv != null && PhotonNetwork.InRoom && !pv.IsMine)
        {
            enabled = false;
            return;
        }
        Transform child = transform.Find("_HeartbeatAudio");
        if (child == null)
        {
            GameObject g = new GameObject("_HeartbeatAudio");
            g.transform.SetParent(transform, false);
            child = g.transform;
        }
        heartbeatAudioSource = child.GetComponent<AudioSource>();
        if (heartbeatAudioSource == null)
        {
            heartbeatAudioSource = child.gameObject.AddComponent<AudioSource>();
        }
        heartbeatAudioSource.spatialBlend = 0f;
        heartbeatAudioSource.playOnAwake = false;
        heartbeatAudioSource.loop = false;
        heartbeatClip = CreateHeartbeatPulseClip();
        FindEnemies();
    }
    private void Update()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            searchTimer = 1f;
            FindEnemies();
        }
        float closestDist = float.MaxValue;
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy)
                {
                    Vector3 p1 = transform.position;
                    Vector3 p2 = enemies[i].transform.position;
                    p1.y = 0f;
                    p2.y = 0f;
                    float dist = Vector3.Distance(p1, p2);
                    if (dist < closestDist) closestDist = dist;
                }
            }
        }
        if (closestDist > maxDistance)
        {
            beatTimer = 0f;
            return;
        }
        float t = Mathf.Clamp01(1f - ((closestDist - minDistance) / (maxDistance - minDistance)));
        float currentBpm = Mathf.Lerp(minBpm, maxBpm, t);
        float interval = 60f / currentBpm;
        float volume = Mathf.Lerp(0.5f, 1f, t);
        beatTimer += Time.deltaTime;
        if (beatTimer >= interval)
        {
            beatTimer = 0f;
            heartbeatAudioSource.PlayOneShot(heartbeatClip, volume);
        }
    }
    private void FindEnemies()
    {
        enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include);
    }
    private AudioClip CreateHeartbeatPulseClip()
    {
        int sampleRate = 44100;
        float duration = 0.2f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / sampleRate;
            float env1 = Mathf.Exp(-28f * time);
            float pulse1 = (Mathf.Sin(2f * Mathf.PI * 110f * time) + Mathf.Sin(2f * Mathf.PI * 55f * time) * 1.4f) * env1;
            float pulse2 = 0f;
            if (time > 0.07f)
            {
                float t2 = time - 0.07f;
                float env2 = Mathf.Exp(-32f * t2);
                pulse2 = (Mathf.Sin(2f * Mathf.PI * 90f * t2) + Mathf.Sin(2f * Mathf.PI * 45f * t2) * 1.4f) * env2;
            }
            data[i] = Mathf.Clamp((pulse1 + pulse2 * 0.8f) * 2.5f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("HeartbeatPulseClip", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    private void OnDestroy()
    {
        if (heartbeatClip != null)
        {
            Destroy(heartbeatClip);
        }
    }
}