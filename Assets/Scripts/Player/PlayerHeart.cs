using System;
using UnityEngine;

namespace BlackHeart
{
    // Неподвижное сердце в центре рамки — хитбокс игрока.
    public class PlayerHeart : MonoBehaviour
    {
        public int MaxHp { get; private set; } = 10;
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;
        public float HitRadius { get; set; } = 0.35f;
        public float InvulnTime { get; set; } = 0.4f;
        public bool CannotDie { get; set; }   // в обучении HP не опускается ниже 1
        public Vector2 Position => transform.position;
        public Material SpriteMaterial => sr.sharedMaterial;

        public event Action<int> OnTakeDamage;
        public event Action<int> OnHeal;
        public event Action OnDeath;


        SpriteRenderer sr;
        SpriteAnimator anim;
        float invuln;
        bool hidden, broken;

        // Сердцебиение «тук-тук»: удар → затухание, ещё удар → затухание, затем пауза (≈ 1 удар в секунду)
        static readonly int[] PulseSequence = { 1, 2, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0 };
        const float PulseFrameTime = 0.08f, HitStepTime = 0.1f;

        void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.Get(ArtIds.Soul);
            sr.color = Color.white;   // цвет души — в самом спрайте
            sr.sortingOrder = 5;
            anim = gameObject.AddComponent<SpriteAnimator>();
            PlayPulse();
        }

        void PlayPulse() =>
            anim.Play(ArtLibrary.Frames(ArtIds.SoulPulse, ArtIds.Soul), PulseFrameTime, true, null, PulseSequence);

        public void ResetState(int maxHp)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            invuln = 0;
            hidden = false;
            broken = false;
            PlayPulse();
        }

        public void SetHidden(bool value) => hidden = value;

        public bool TakeDamage(int amount)
        {
            if (IsDead || invuln > 0 || amount <= 0) return false;
            Hp = Mathf.Max(CannotDie ? 1 : 0, Hp - amount);
            invuln = InvulnTime;
            GameManager.I.Pixel.Shake(0.25f);
            OnTakeDamage?.Invoke(amount);
            GameEvents.RaisePlayerTakeDamage(amount, Hp);
            if (Hp == 0)
            {
                OnDeath?.Invoke();
                GameEvents.RaisePlayerDied();
            }
            return true;
        }

        public void Heal(int amount)
        {
            int before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (Hp == before) return;
            OnHeal?.Invoke(Hp - before);
            GameEvents.RaisePlayerHealed(Hp - before, Hp);
        }

        // Анимация смерти в стиле Undertale
        public void Crack()
        {
            broken = true;
            anim.Stop();
            sr.sprite = ArtLibrary.Get(ArtIds.SoulBroken);
            GameEvents.RaiseSoulCracked();
        }

        public void Shatter()
        {
            hidden = true;
            var shards = ArtLibrary.Frames(ArtIds.SoulShardSet, ArtIds.SoulShard);
            for (int i = 0; i < 6; i++)
            {
                Vector2 v = new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(1.5f, 5f));
                Vector2 p = Position + UnityEngine.Random.insideUnitCircle * 0.25f;
                Debris.Spawn(shards[i % shards.Length], p, 0, 1f, Color.white, v, 0, 2f, 9f, false, 6);
            }
            GameEvents.RaiseSoulShattered();
        }

        // Сердце всегда в центре рамки (рамка двигается между фазами, пуль в это время нет)
        void LateUpdate()
        {
            var box = GameManager.I.Box;
            if (box != null) transform.position = box.Current.center;
        }

        // Во время неуязвимости: белая вспышка → база → серый → база (по 0.1 с), затем снова пульс
        void Update()
        {
            sr.enabled = !hidden;
            if (invuln <= 0 || broken) return;

            invuln = Mathf.Max(0, invuln - Time.deltaTime);
            if (invuln <= 0)
            {
                PlayPulse();
                return;
            }
            anim.Stop();
            var hit = ArtLibrary.Frames(ArtIds.SoulHit, ArtIds.Soul);
            int step = Mathf.FloorToInt((InvulnTime - invuln) / HitStepTime) % 4;
            sr.sprite = step == 0 ? hit[0]
                      : step == 2 ? hit[Mathf.Min(1, hit.Length - 1)]
                      : ArtLibrary.Get(ArtIds.Soul);
        }
    }
}
