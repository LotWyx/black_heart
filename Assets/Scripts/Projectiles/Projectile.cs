using UnityEngine;

namespace BlackHeart
{
    // Универсальная пуля. Поведение задаётся типом (цвет и правила) и траекторией (IProjectileMotion).
    public class Projectile : MonoBehaviour
    {
        public ProjectileType Type { get; private set; }
        public bool IsFragment { get; private set; }     // осколок фиолетовой пули
        public float Radius { get; private set; } = 0.3f;
        public Color Color => ProjectileTypes.ColorOf(Type);
        public Vector2 Position => transform.position;

        IProjectileMotion motion;
        float time;
        bool dead, reverseSpin, spinning;
        int spawnFrame;
        SpriteAnimator anim;
        float sliceableAt;    // до этого момента пулю нельзя разрубить (пара кадров у осколков)
        SpriteRenderer sr;

        const float FragmentInvulnerability = 0.08f;
        const float SpinFrameTime = 0.06f;
        static readonly int[] ReverseSpin = { 0, 3, 2, 1 };

        public static Projectile Create(ProjectileType type, IProjectileMotion motion, float startTime, bool fragment)
        {
            var go = new GameObject("Projectile_" + type);
            var p = go.AddComponent<Projectile>();
            p.Type = type;
            p.motion = motion;
            p.time = startTime;
            p.IsFragment = fragment;
            p.sliceableAt = fragment ? Time.time + FragmentInvulnerability : 0f;
            p.reverseSpin = Random.value < 0.5f;

            // Первый кадр — белая вспышка появления, дальше покадровое вращение
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sprite = ArtLibrary.Get(ArtIds.BulletSpawn);
            p.sr.color = p.Color;
            p.sr.sortingOrder = 10;
            p.anim = go.AddComponent<SpriteAnimator>();
            p.spawnFrame = Time.frameCount;

            if (fragment)
            {
                var crack = new GameObject("Crack").AddComponent<SpriteRenderer>();
                crack.transform.SetParent(go.transform, false);
                crack.sprite = ArtLibrary.Get(ArtIds.BulletCrack);
                crack.sortingOrder = 11;
            }

            go.transform.position = motion.Evaluate(startTime);
            return p;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (dead || !gm.IsPlaying) return;
            bool frozen = gm.Projectiles.Frozen;
            anim.enabled = !frozen;
            if (frozen) return;

            time += Time.deltaTime;
            transform.position = motion.Evaluate(time);

            if (!spinning && Time.frameCount > spawnFrame)
            {
                spinning = true;
                anim.Play(ArtLibrary.Frames(ArtIds.BulletSpin, ArtIds.Bullet), SpinFrameTime, true, null,
                    reverseSpin ? ReverseSpin : null);
            }

            var heart = gm.Player;
            if (Vector2.Distance(Position, heart.Position) < heart.HitRadius + Radius * 0.6f)
                HitPlayer();
            else if (Position.sqrMagnitude > 900f)
                Kill();
        }

        public bool CanBeSliced => !dead && Time.time >= sliceableAt;

        public void Slice(Vector2 cutDir)
        {
            if (dead) return;
            var gm = GameManager.I;

            if (Type == ProjectileType.Purple && !IsFragment)
            {
                Split();
                return;
            }

            if (Type == ProjectileType.Blue)
            {
                gm.Player.TakeDamage(1);
                gm.Score.Add(gm.Score.blueBulletPenalty, Position);
            }
            else
            {
                gm.Score.Add(IsFragment ? gm.Score.purpleFragment : gm.Score.whiteBullet, Position);
            }

            SpawnHalves(cutDir);
            GameEvents.RaiseProjectileSliced(this);
            Kill();
        }

        // Две новые пули продолжают траекторию: одна на спрайт впереди, другая на спрайт позади.
        // Пару кадров они неуязвимы, чтобы не разрубились тем же движением мгновенно.
        void Split()
        {
            const float probe = 0.05f;
            float speed = Vector2.Distance(motion.Evaluate(time + probe), motion.Evaluate(time)) / probe;
            float spriteLength = sr.sprite.bounds.size.x;
            float shift = spriteLength / Mathf.Max(speed, 0.1f);

            var manager = GameManager.I.Projectiles;
            manager.Spawn(ProjectileType.Purple, motion, time + shift, true);
            manager.Spawn(ProjectileType.Purple, motion, time - shift, true);

            Debris.Burst(Position, Color, 6, 3f);
            GameEvents.RaiseProjectileSplit(this);
            Kill();
        }

        void HitPlayer()
        {
            if (Type != ProjectileType.Blue)
                GameManager.I.Player.TakeDamage(1);
            Debris.Burst(Position, Color, 8, 3f);
            GameEvents.RaiseProjectileHitPlayer(this);
            Collapse();
            Kill();
        }

        public void Explode()
        {
            if (dead) return;
            Debris.Burst(Position, Color, 8, 3f);
            Collapse();
            Kill();
        }

        void SpawnHalves(Vector2 cutDir)
        {
            ArtLibrary.Halves(sr.sprite, out var left, out var right);
            float rot = Mathf.Atan2(cutDir.y, cutDir.x) * Mathf.Rad2Deg - 90f;
            Vector2 side = Quaternion.Euler(0, 0, rot) * Vector2.right;
            Debris.Spawn(left, Position, rot, 1f, Color, -side * 3f, Random.Range(-360f, -120f), 0.6f, 6f, false, 9);
            Debris.Spawn(right, Position, rot, 1f, Color, side * 3f, Random.Range(120f, 360f), 0.6f, 6f, false, 9);
            Debris.Burst(Position, Color, 5, 4f);
        }

        // Исчезновение: на один кадр пуля схлопывается в белую точку 2x2
        void Collapse()
        {
            Debris.Spawn(ArtLibrary.Get(ArtIds.BulletDespawn), Position, 0, 1f, Color.white,
                Vector2.zero, 0, Debris.OneFrame, 0f, false, 10);
        }

        void Kill()
        {
            dead = true;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (GameManager.I != null && GameManager.I.Projectiles != null)
                GameManager.I.Projectiles.Unregister(this);
        }
    }
}
