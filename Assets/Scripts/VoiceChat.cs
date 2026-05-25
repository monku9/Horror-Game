using UnityEngine;
using Mirror;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : NetworkBehaviour
{
    [Header("Settings")]
    public KeyCode pushToTalkKey = KeyCode.V;
    public bool pushToTalk = true;

    private AudioSource audioSource;
    private AudioClip micClip;
    private string micDevice;
    private int lastSamplePos;
    private float timer;

    private const int SampleRate = 16000;
    private const float SendInterval = 0.05f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 25f;

        if (!isLocalPlayer) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone found!");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, 1, SampleRate);
        Debug.Log($"Mic started: {micDevice}");
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (!NetworkClient.ready) return;
        if (micClip == null) return;

        if (pushToTalk && !Input.GetKey(pushToTalkKey)) return;

        timer += Time.deltaTime;
        if (timer < SendInterval) return;
        timer = 0f;

        int currentPos = Microphone.GetPosition(micDevice);

        if (currentPos < lastSamplePos)
            lastSamplePos = 0;

        int sampleCount = currentPos - lastSamplePos;
        if (sampleCount <= 0) return;

        float[] samples = new float[sampleCount];
        micClip.GetData(samples, lastSamplePos);
        lastSamplePos = currentPos;

        if (GetVolume(samples) > 0.02f)
            CmdSendVoice(samples);
    }

    [Command]
    void CmdSendVoice(float[] samples)
    {
        RpcReceiveVoice(samples);
    }

    [ClientRpc]
    void RpcReceiveVoice(float[] samples)
    {
        if (netIdentity.isLocalPlayer) return;
        if (samples == null || samples.Length == 0) return;

        audioSource.Stop();

        AudioClip clip = AudioClip.Create("voice", samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        audioSource.clip = clip;
        audioSource.Play();
    }

    float GetVolume(float[] samples)
    {
        float sum = 0f;
        foreach (float s in samples) sum += Mathf.Abs(s);
        return sum / samples.Length;
    }

    void OnDestroy()
    {
        if (isLocalPlayer && micDevice != null)
            Microphone.End(micDevice);
    }
}