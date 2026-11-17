using UnityEngine;

namespace Yeeun.GasMask
{
    /// <summary>
    /// Breathing through the mask and the heartbeat, both driven by <see cref="PlayerHealth"/>.
    /// Hurt = faster, rougher, louder breathing and an audible, faster heartbeat.
    /// The sounds are synthesised at start (placeholders until real recordings exist);
    /// assign clips to replace them.
    /// </summary>
    public class GasMaskAudio : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] AudioClip breathClip;
        [SerializeField] AudioClip heartbeatClip;

        [Header("Breathing")]
        [SerializeField, Range(0f, 1f)] float breathVolumeCalm = 0.12f;
        [SerializeField, Range(0f, 1f)] float breathVolumeHurt = 0.45f;
        [SerializeField] float breathPitchHurt = 1.6f;

        [Header("Heartbeat")]
        [SerializeField, Range(0f, 1f)] float heartVolumeHurt = 0.8f;
        [SerializeField] float heartBpmCalm = 70f;
        [SerializeField] float heartBpmHurt = 140f;
        [Tooltip("Heartbeat is silent above this much health (0..1 of max).")]
        [SerializeField, Range(0f, 1f)] float heartAudibleBelow = 0.95f;

        public float BreathPitch { get; private set; } = 1f;
        public float BreathVolume { get; private set; }
        public float HeartVolume { get; private set; }
        public float HeartBpm { get; private set; }
        public int HeartbeatsPlayed { get; private set; }

        AudioSource breath, heart;
        float nextBeat;

        void Start()
        {
            if (breathClip == null) breathClip = MakeBreathClip();
            if (heartbeatClip == null) heartbeatClip = MakeHeartbeatClip();
            breath = AddSource(breathClip, true);
            heart = AddSource(heartbeatClip, false);
            breath.Play();
        }

        AudioSource AddSource(AudioClip clip, bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        void Update()
        {
            float hurt = health != null ? health.Damage01 : 0f;
            BreathPitch = Mathf.Lerp(1f, breathPitchHurt, hurt);
            BreathVolume = Mathf.Lerp(breathVolumeCalm, breathVolumeHurt, hurt);
            breath.pitch = BreathPitch;
            breath.volume = BreathVolume;

            float healthLeft = 1f - hurt;
            bool audible = health != null && !health.IsDead && healthLeft < heartAudibleBelow;
            HeartBpm = Mathf.Lerp(heartBpmCalm, heartBpmHurt, hurt);
            HeartVolume = audible ? Mathf.Lerp(0.25f, heartVolumeHurt, hurt) : 0f;
            if (HeartVolume > 0f && Time.time >= nextBeat)
            {
                heart.volume = HeartVolume;
                heart.Play();
                HeartbeatsPlayed++;
                nextBeat = Time.time + 60f / HeartBpm;
            }
        }

        // ------------------------------------------------------------------ placeholder sounds

        static AudioClip MakeHeartbeatClip()
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            int n = (int)(rate * 0.55f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float lub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 22f);
                float t2 = t - 0.2f;
                float dub = t2 > 0f ? Mathf.Sin(2f * Mathf.PI * 46f * t2) * Mathf.Exp(-t2 * 26f) * 0.7f : 0f;
                data[i] = (lub + dub) * 0.9f;
            }
            var clip = AudioClip.Create("Heartbeat_Placeholder", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeBreathClip()
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            float length = 3.2f;
            int n = (int)(rate * length);
            var data = new float[n];
            var rng = new System.Random(7);
            float low = 0f, peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += 0.07f * (white - low);                 // muffled, like breathing through a filter
                float env = 0f;
                if (t > 0.2f && t < 1.3f) env = Mathf.Sin(Mathf.PI * (t - 0.2f) / 1.1f) * 0.7f;  // inhale
                if (t > 1.6f && t < 2.9f) env = Mathf.Sin(Mathf.PI * (t - 1.6f) / 1.3f);         // exhale
                data[i] = low * env;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0f) for (int i = 0; i < n; i++) data[i] *= 0.8f / peak;
            var clip = AudioClip.Create("Breath_Placeholder", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
