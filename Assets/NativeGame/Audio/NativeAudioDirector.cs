using UnityEngine;

namespace Echo.NativeGame
{
    // Scene sound only: observes existing gameplay state and never changes its owners.
    public sealed class NativeAudioDirector : MonoBehaviour
    {
        const string MusicVolumeKey = "NativeDemo.Audio.MusicVolume";
        const string EffectsVolumeKey = "NativeDemo.Audio.EffectsVolume";

        [Header("Scene owner (auto-found if left empty)")]
        public NativeRunController run;
        [Header("Original procedural placeholder clips")]
        public AudioClip musicLoop;
        public AudioClip interactionCue;
        public AudioClip hurtCue;
        public AudioClip defeatCue;
        [Range(0f, 1f)] public float defaultMusicVolume = 0.45f;
        [Range(0f, 1f)] public float defaultEffectsVolume = 0.7f;

        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }

        AudioSource musicSource;
        AudioSource effectsSource;
        NativeInteraction[] interactions;
        float observedHealth;
        int observedKills;
        bool observed;

        void Awake()
        {
            if (!run) run = FindObjectOfType<NativeRunController>();
            if (!run || !run.player || !run.combat || !run.hud || !musicLoop || !interactionCue || !hurtCue || !defeatCue)
            {
                Debug.LogError("Native audio: assign a complete run and all four clips.", this);
                enabled = false;
                return;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = musicLoop;

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;

            SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
            SetEffectsVolume(PlayerPrefs.GetFloat(EffectsVolumeKey, defaultEffectsVolume));
        }

        void OnEnable()
        {
            if (!run) return;
            run.Started += OnRunStarted;
            interactions = run.hud.interactables;
            if (interactions == null) return;
            foreach (var interaction in interactions)
                if (interaction) interaction.Confirmed += OnInteractionConfirmed;
        }

        void Start()
        {
            if (!run) return;
            observedHealth = run.player.Health;
            observedKills = run.combat.Kills;
            observed = true;
            if (run.Running) OnRunStarted();
        }

        void OnDisable()
        {
            if (run) run.Started -= OnRunStarted;
            if (interactions != null)
                foreach (var interaction in interactions)
                    if (interaction) interaction.Confirmed -= OnInteractionConfirmed;
            interactions = null;
            if (musicSource) musicSource.Stop();
        }

        void Update()
        {
            if (!run || !observed) return;

            // These controls work in Play Mode and Windows builds, including while the phone is open.
            if (Input.GetKeyDown(KeyCode.F5)) SetMusicVolume(MusicVolume - 0.1f);
            if (Input.GetKeyDown(KeyCode.F6)) SetMusicVolume(MusicVolume + 0.1f);
            if (Input.GetKeyDown(KeyCode.F7)) SetEffectsVolume(EffectsVolume - 0.1f);
            if (Input.GetKeyDown(KeyCode.F8)) SetEffectsVolume(EffectsVolume + 0.1f);

            float health = run.player.Health;
            if (health < observedHealth) Play(hurtCue);
            observedHealth = health;

            int kills = run.combat.Kills;
            if (kills > observedKills) Play(defeatCue);
            observedKills = kills;

            if (run.Phase == NativeRunController.RunPhase.Dead || run.Phase == NativeRunController.RunPhase.Completed)
            {
                if (musicSource.isPlaying) musicSource.Stop();
            }
        }

        void OnRunStarted()
        {
            if (musicSource && !musicSource.isPlaying) musicSource.Play();
        }

        void OnInteractionConfirmed(NativeInteraction interaction)
        {
            if (interaction && run.Running) Play(interactionCue);
        }

        void Play(AudioClip clip)
        {
            if (effectsSource && clip && EffectsVolume > 0f) effectsSource.PlayOneShot(clip);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            if (musicSource) musicSource.volume = MusicVolume;
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        }

        public void SetEffectsVolume(float value)
        {
            EffectsVolume = Mathf.Clamp01(value);
            if (effectsSource) effectsSource.volume = EffectsVolume;
            PlayerPrefs.SetFloat(EffectsVolumeKey, EffectsVolume);
        }

        void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        void OnApplicationQuit() { PlayerPrefs.Save(); }
    }
}
