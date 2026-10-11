using UnityEngine;

namespace BlackHeart
{
    // Звуки, подключённые к игре только через GameEvents.
    public class SfxPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 1f;

        AudioSource source;

        void Awake()
        {
            SfxSynth.Init();
            source = gameObject.AddComponent<AudioSource>();
        }

        void OnEnable()
        {
            GameEvents.OnGameStarted += OnGameStarted;
            GameEvents.OnProjectileSliced += OnProjectileSliced;
            GameEvents.OnProjectileSplit += OnProjectileSplit;
            GameEvents.OnPlayerTakeDamage += OnPlayerTakeDamage;
            GameEvents.OnPlayerHealed += OnPlayerHealed;
            GameEvents.OnEnemyAttack += OnEnemyAttack;
            GameEvents.OnEnemyTakeDamage += OnEnemyTakeDamage;
            GameEvents.OnEnemyLaughed += OnEnemyLaughed;
            GameEvents.OnEnemyDefeated += OnEnemyDefeated;
            GameEvents.OnSoulCracked += OnSoulCracked;
            GameEvents.OnSoulShattered += OnSoulShattered;
        }

        void OnDisable()
        {
            GameEvents.OnGameStarted -= OnGameStarted;
            GameEvents.OnProjectileSliced -= OnProjectileSliced;
            GameEvents.OnProjectileSplit -= OnProjectileSplit;
            GameEvents.OnPlayerTakeDamage -= OnPlayerTakeDamage;
            GameEvents.OnPlayerHealed -= OnPlayerHealed;
            GameEvents.OnEnemyAttack -= OnEnemyAttack;
            GameEvents.OnEnemyTakeDamage -= OnEnemyTakeDamage;
            GameEvents.OnEnemyLaughed -= OnEnemyLaughed;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnSoulCracked -= OnSoulCracked;
            GameEvents.OnSoulShattered -= OnSoulShattered;
        }

        void Play(AudioClip clip, float pitch = 1f)
        {
            if (clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        void OnGameStarted() => Play(SfxSynth.Start);
        void OnProjectileSliced(Projectile p) => Play(SfxSynth.Slice, Random.Range(0.9f, 1.15f));
        void OnProjectileSplit(Projectile p) => Play(SfxSynth.Split);
        void OnPlayerTakeDamage(int amount, int hp) => Play(SfxSynth.Hurt);
        void OnPlayerHealed(int amount, int hp) => Play(SfxSynth.Heal);
        void OnEnemyAttack(Enemy e) => Play(SfxSynth.Whoosh);
        void OnEnemyTakeDamage(Enemy e, int amount) => Play(SfxSynth.EnemyHit);
        void OnEnemyLaughed(Enemy e) => Play(SfxSynth.Laugh);
        void OnEnemyDefeated(Enemy e) => Play(SfxSynth.Defeat);
        void OnSoulCracked() => Play(SfxSynth.Crack);
        void OnSoulShattered() => Play(SfxSynth.Shatter);
    }
}
