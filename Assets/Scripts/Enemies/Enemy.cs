using System;
using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Враг: здоровье, покадровые анимации и события. Атаки выбирает BattleController.
    // Анимации берутся из полосок <spriteId>_idle/_windup/_hurt/_laugh/_defeat.
    public class Enemy : MonoBehaviour
    {
        public const float SpriteScale = 1f;   // спрайты 80x80 рисуются 1:1

        public EnemyDefinition Definition { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => Hp <= 0;
        public float HitRadius { get; private set; }
        public Vector2 Position => transform.position;

        public event Action OnAttack;
        public event Action<int> OnTakeDamage;
        public event Action OnLaugh;
        public event Action OnDefeated;

        const float IdleFrameTime = 0.4f;        // покой из 2 кадров (база + сдвиг)
        const float IdleFrameTimeLong = 0.3f;    // покой из 3+ поз — кадры чаще, движение плавнее
        const float WindupFrameTime = 0.1f;
        const float FlashFrameTime = 0.05f;
        const float LaughFrameTime = 0.1f;
        static readonly int[] HurtSequence = { 0, 1, 0, 1, 0, 1 };
        static readonly int[] DefeatSequence = { 0, 0, 0, 0, 0, 0, 1, 2, 3 };

        SpriteRenderer sr;
        SpriteAnimator anim;
        Vector2 home, animOffset;
        float shake;
        bool busy;   // проигрывается особая анимация — idle не перебиваем

        Sprite[] idle, windup, hurt, laugh, defeat, angry, vanish;

        public static Enemy Create(EnemyDefinition def, Vector2 home)
        {
            var go = new GameObject("Enemy_" + def.displayName);
            var e = go.AddComponent<Enemy>();
            e.Definition = def;
            e.home = home;
            e.MaxHp = e.Hp = UnityEngine.Random.Range(Mathf.Max(1, def.minHp), Mathf.Max(def.minHp, def.maxHp) + 1);

            e.sr = go.AddComponent<SpriteRenderer>();
            e.sr.sprite = ArtLibrary.Get(def.spriteId);
            e.sr.sortingOrder = 3;
            e.anim = go.AddComponent<SpriteAnimator>();
            go.transform.localScale = Vector3.one * SpriteScale;
            go.transform.position = home;

            string id = def.spriteId;
            e.idle = ArtLibrary.Frames(ArtIds.EnemyAnim(id, "idle"), id);
            e.windup = ArtLibrary.Frames(ArtIds.EnemyAnim(id, "windup"), id);
            e.hurt = ArtLibrary.Frames(ArtIds.EnemyAnim(id, "hurt"), id);
            e.laugh = ArtLibrary.Frames(ArtIds.EnemyAnim(id, "laugh"), id);
            e.defeat = ArtLibrary.Frames(ArtIds.EnemyAnim(id, "defeat"), id);
            // Необязательные анимации: если их нет — берём похожие
            string angryId = ArtIds.EnemyAnim(id, "angry"), vanishId = ArtIds.EnemyAnim(id, "vanish");
            e.angry = ArtLibrary.Has(angryId) ? ArtLibrary.Frames(angryId) : e.windup;
            e.vanish = ArtLibrary.Has(vanishId) ? ArtLibrary.Frames(vanishId) : e.defeat;

            Vector3 size = e.sr.sprite.bounds.size * SpriteScale;
            e.HitRadius = Mathf.Max(size.x, size.y) * 0.5f;
            return e;
        }

        void Start()
        {
            if (!busy) PlayIdle();
        }

        void Update()
        {
            shake = Mathf.Max(0, shake - Time.deltaTime);
            Vector2 jitter = shake > 0 ? UnityEngine.Random.insideUnitCircle * 0.15f : Vector2.zero;
            transform.position = home + animOffset + jitter;
        }

        void PlayIdle()
        {
            busy = false;
            anim.Play(idle, idle.Length > 2 ? IdleFrameTimeLong : IdleFrameTime);
        }

        static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

        // Появление: кадр белой вспышки, затем враг опускается на место
        public IEnumerator Appear()
        {
            busy = true;
            anim.Stop();
            sr.sprite = hurt[0];
            animOffset = new Vector2(0, 1.5f);
            yield return null;
            PlayIdle();
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                animOffset = Vector2.Lerp(new Vector2(0, 1.5f), Vector2.zero, EaseOut(t / 0.5f));
                yield return null;
            }
            animOffset = Vector2.zero;
        }

        // Замах: контур оранжевый → силуэт раздувается → глаза вспыхивают
        public IEnumerator AttackWindup()
        {
            OnAttack?.Invoke();
            GameEvents.RaiseEnemyAttack(this);
            busy = true;
            bool done = false;
            anim.Play(windup, WindupFrameTime, false, () => done = true);
            while (!done) yield return null;
            yield return new WaitForSeconds(WindupFrameTime);
        }

        // Вылет вверх за экран
        public IEnumerator FlyUp(float worldY)
        {
            anim.Play(idle, idle.Length > 2 ? IdleFrameTimeLong : IdleFrameTime);
            float startY = animOffset.y, endY = worldY - home.y;
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float k = t / 0.5f;
                animOffset = new Vector2(0, Mathf.Lerp(startY, endY, k * k));
                yield return null;
            }
            animOffset = new Vector2(0, endY);
        }

        // Возвращение на место
        public IEnumerator Return()
        {
            PlayIdle();
            Vector2 from = animOffset;
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                animOffset = Vector2.Lerp(from, Vector2.zero, EaseOut(t / 0.5f));
                yield return null;
            }
            animOffset = Vector2.zero;
        }

        // Урон: белая вспышка и красные глаза попеременно, затем снова idle
        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            Hp = Mathf.Max(0, Hp - amount);
            shake = 0.3f;
            busy = true;
            anim.Play(hurt, FlashFrameTime, false, PlayIdle, HurtSequence);
            Debris.Burst(Position, Color.white, 12, 5f);
            OnTakeDamage?.Invoke(amount);
            GameEvents.RaiseEnemyTakeDamage(this, amount);
        }

        public IEnumerator Laugh()
        {
            OnLaugh?.Invoke();
            GameEvents.RaiseEnemyLaughed(this);
            busy = true;
            anim.Play(laugh, LaughFrameTime);
            yield return new WaitForSeconds(1.2f);
            PlayIdle();
        }

        // Злость: красный контур и тряска
        public IEnumerator Angry(float duration)
        {
            busy = true;
            shake = 0.3f;
            anim.Play(angry, 0.08f);
            yield return new WaitForSeconds(duration);
            PlayIdle();
        }

        // Уход со сцены (без события поражения): силуэт → полоса → точка
        public IEnumerator Vanish()
        {
            busy = true;
            bool done = false;
            anim.Play(vanish, 0.07f, false, () => done = true);
            while (!done) yield return null;
            Debris.Burst(Position, Color.white, 20, 6f);
            yield return new WaitForSeconds(0.07f);
            sr.enabled = false;
        }

        // Поражение: дрожь → трещины → белый силуэт → полоса → точка → исчезает
        public IEnumerator Defeat()
        {
            OnDefeated?.Invoke();
            GameEvents.RaiseEnemyDefeated(this);
            busy = true;
            anim.Stop();
            shake = 0.4f;
            yield return new WaitForSeconds(0.4f);

            bool done = false;
            anim.Play(defeat, FlashFrameTime, false, () => done = true, DefeatSequence);
            while (!done) yield return null;
            Debris.Burst(Position, Color.white, 30, 7f);
            yield return new WaitForSeconds(FlashFrameTime);
            sr.enabled = false;
        }
    }
}
