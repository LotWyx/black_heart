using UnityEngine;

namespace BlackHeart
{
    // Универсальная пуля. Поведение задаётся типом (цвет и правила) и траекторией (IProjectileMotion).
    public class Projectile : MonoBehaviour
    {
        public ProjectileType Type { get; private set; }
        public bool IsFragment { get; private set; }     // осколок фиолетовой пули
        public float Radius { get; private set; } = 0.45f;   // тело пули ~15 px (кадр 17x17 вместе с ореолом)
        public Color Color => ProjectileTypes.ColorOf(Type);
        public Vector2 Position => transform.position;
        // Пуля группы (стена, клетка, волна, круг) видна только внутри рамки; одиночная прилетает
        // из-за края экрана и видна везде. Разрезать любую пулю можно только внутри рамки.
        public bool Formation { get; private set; }

        IProjectileMotion motion;
        float time;
        bool dead, reverseSpin, spinning, rushing, enteredFrame;
        Vector2 offset;      // сдвиг осколка от траектории, чтобы пара не сливалась; у сердца сходит на нет
        string theme;   // враг, выпустивший пулю (Ghost, Eye, ...) — от него зависит форма пули
        string variant; // особый вид пули у паттерна (Bone, Spirit…), передаётся осколкам
        string halfId;  // осколок фиолетовой: спрайт половинки (одно привидение, один глаз, ...)
        bool flipHalf;  // второй осколок пары — зеркальная половинка
        bool lengthwise; // половинки делят спрайт поперёк длины (кость: верх и низ), зеркалить надо по вертикали
        static bool nextHalfFlipped;
        int spawnFrame;
        SpriteAnimator anim;
        float sliceableAt;    // до этого момента пулю нельзя разрубить (пара кадров у осколков)
        SpriteRenderer sr;

        const float FragmentInvulnerability = 0.08f;
        const float MinFragmentGap = 0.5f;   // осколки появляются не ближе этой доли длины спрайта друг к другу
        const float RushSpeed = 30f;         // рывок синей пули в сердце, юнитов в секунду
        const float SpinFrameTime = 0.14f;   // кадры пули — действия (зрачок, челюсть, пламя), не вращение
        static readonly int[] ReverseSpin = { 0, 3, 2, 1 };

        public bool Alive => !dead;

        public static Projectile Create(ProjectileType type, IProjectileMotion motion, float startTime, bool fragment,
            string variant = null, bool formation = false)
        {
            var go = new GameObject("Projectile_" + type);
            var p = go.AddComponent<Projectile>();
            p.Type = type;
            p.motion = motion;
            p.time = startTime;
            p.IsFragment = fragment;
            p.sliceableAt = fragment ? Time.time + FragmentInvulnerability : 0f;
            p.reverseSpin = Random.value < 0.5f;
            p.variant = variant;
            p.Formation = formation;
            p.lengthwise = variant == "Bone";
            var enemy = GameManager.I.Battle.CurrentEnemy;
            if (enemy != null)
            {
                string id = enemy.Definition.spriteId;
                p.theme = id.Substring(id.LastIndexOf('/') + 1);
            }

            // Первый кадр — белая вспышка появления, дальше покадровое вращение
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sprite = ArtLibrary.Get(ArtIds.BulletSpawn);
            p.sr.color = p.Color;
            p.sr.sortingOrder = 10;
            if (formation) p.sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;   // маска — рамка боя
            p.anim = go.AddComponent<SpriteAnimator>();
            p.spawnFrame = Time.frameCount;

            // Осколок фиолетовой распадается на половинки своего врага; если их нет — копия с трещиной
            if (fragment && type == ProjectileType.Purple)
            {
                string half = (p.theme != null ? ArtIds.BulletOf(type, p.theme, variant) : ArtIds.BulletOf(type)) + "Half";
                if (!ArtLibrary.Has(half + "_spin") && p.theme != null) half = ArtIds.BulletOf(type, p.theme) + "Half";
                if (!ArtLibrary.Has(half + "_spin")) half = ArtIds.BulletOf(type) + "Half";
                if (ArtLibrary.Has(half + "_spin"))
                {
                    p.halfId = half;
                    p.flipHalf = nextHalfFlipped;   // осколки рождаются парами: левый, затем правый
                    nextHalfFlipped = !nextHalfFlipped;
                }
            }

            if (fragment && p.halfId == null)
            {
                var crack = new GameObject("Crack").AddComponent<SpriteRenderer>();
                crack.transform.SetParent(go.transform, false);
                crack.sprite = ArtLibrary.Get(ArtIds.BulletCrack);
                crack.sortingOrder = 11;
                crack.maskInteraction = p.sr.maskInteraction;
            }

            go.transform.position = motion.Evaluate(startTime);
            p.Orient();
            return p;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (dead || !gm.IsPlaying) return;
            // Свой спрайт пуля получает со второго кадра, даже если время остановлено (обучение):
            // иначе осколки висели бы вспышкой появления — цветными квадратами
            if (!spinning && Time.frameCount > spawnFrame)
            {
                spinning = true;
                // Форма врага → цветная по типу → общая серая, перекрашенная в цвет типа
                string id = halfId ?? (theme != null ? ArtIds.BulletOf(Type, theme, variant) : null);
                if (id != null && halfId == null && variant != null && !ArtLibrary.Has(id + "_spin")) id = ArtIds.BulletOf(Type, theme);
                if (id == null || !ArtLibrary.Has(id + "_spin")) id = ArtIds.BulletOf(Type);
                sr.flipX = halfId != null && flipHalf && !lengthwise;
                sr.flipY = halfId != null && flipHalf && lengthwise;
                bool typed = ArtLibrary.Has(id + "_spin");
                sr.color = typed ? Color.white : Color;
                anim.Play(typed ? ArtLibrary.Frames(id + "_spin", id) : ArtLibrary.Frames(ArtIds.BulletSpin, ArtIds.Bullet),
                    SpinFrameTime, true, null, reverseSpin ? ReverseSpin : null);
            }

            bool frozen = gm.Projectiles.Frozen;
            anim.enabled = !frozen;
            if (frozen) return;

            var heart = gm.Player;
            if (rushing)
            {
                RushStep(heart);
                return;
            }

            time += NearHeartStep(gm, Time.deltaTime);
            transform.position = Displaced(motion.Evaluate(time));
            Orient();

            // Пуля группы, прошедшая рамку насквозь (кости клетки мимо сердца), больше не нужна: за рамкой её
            // не видно и не разрезать, а фаза защиты ждала бы, пока она уедет за экран
            if (Formation)
            {
                Rect r = gm.Box.Current;
                bool near = Position.x > r.xMin - Radius && Position.x < r.xMax + Radius &&
                            Position.y > r.yMin - Radius && Position.y < r.yMax + Radius;
                if (near && InFrame) enteredFrame = true;
                else if (!near && enteredFrame)
                {
                    Kill();
                    return;
                }
            }

            if (Vector2.Distance(Position, heart.Position) < heart.HitRadius + Radius * 0.6f)
                HitPlayer();
            else if (FarOffScreen(gm))
                Kill();
        }

        // Ограничение скорости у сердца: в последних юнитах пути пуля не быстрее MaxNearSpeed, чтобы её всегда
        // можно было успеть разрезать (рывки мухи, броски из волны и стены). Траектория та же — замедляется её «время».
        const float SlowRadius = 3f, MaxNearSpeed = 2.6f;
        float NearHeartStep(GameManager gm, float dt)
        {
            if (dt <= 0f) return dt;
            Vector2 next = motion.Evaluate(time + dt);
            if (Vector2.Distance(next, gm.Player.Position) > SlowRadius) return dt;
            float speed = Vector2.Distance(next, motion.Evaluate(time)) / dt;
            return speed > MaxNearSpeed ? dt * MaxNearSpeed / speed : dt;
        }

        // Позиция с учётом сдвига осколка: вдали от сердца сдвиг полный, у самого сердца исчезает,
        // так что осколок попадает туда же, куда попала бы целая пуля
        Vector2 Displaced(Vector2 raw)
        {
            if (offset == Vector2.zero) return raw;
            float k = Mathf.Clamp01((Vector2.Distance(raw, GameManager.I.Player.Position) - 0.4f) / 1.5f);
            return raw + offset * k;
        }

        // Рывок в сердце: так уходят синие пули, когда кроме них на поле ничего не осталось
        public void Rush()
        {
            if (!dead) rushing = true;
        }

        void RushStep(PlayerHeart heart)
        {
            Vector2 to = heart.Position - Position;
            float step = RushSpeed * Time.deltaTime, reach = heart.HitRadius + Radius * 0.6f;
            if (to.magnitude <= step + reach)
            {
                transform.position = heart.Position - to.normalized * Mathf.Min(reach * 0.5f, to.magnitude);
                HitPlayer();
            }
            else transform.position = Position + to.normalized * step;
        }

        public bool InFrame => GameManager.I.Box.Current.Contains(Position);

        // Видна ли пуля игроку: группа — только в рамке, одиночная — в пределах экрана
        bool Visible => Formation ? InFrame : GameManager.I.Pixel.ViewRect.Contains(Position);

        // Кости и прочие вытянутые пули поворачиваются вслед за траекторией
        void Orient()
        {
            if (motion is IOrientedMotion om) transform.rotation = Quaternion.Euler(0, 0, om.AngleAt(time));
        }

        // Улетела далеко за экран (прошла мимо сердца) — больше не нужна
        bool FarOffScreen(GameManager gm)
        {
            Rect v = gm.Pixel.ViewRect;
            const float margin = 4f;
            Vector2 p = Position;
            return p.x < v.xMin - margin || p.x > v.xMax + margin || p.y < v.yMin - margin || p.y > v.yMax + margin;
        }

        public bool CanBeSliced => !dead && Time.time >= sliceableAt && InFrame;

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
        // Сдвиг по времени ограничен расстоянием: у замершей или медленной пули (кость в стене, волна перед
        // броском) он иначе уносил осколок на секунды вперёд и назад по траектории — вплоть до точки за экраном.
        void Split()
        {
            const float probe = 0.05f;
            float speed = Vector2.Distance(motion.Evaluate(time + probe), motion.Evaluate(time)) / probe;
            float spriteLength = sr.sprite.bounds.size.x;
            float shift = spriteLength / Mathf.Max(speed, 0.1f);
            float ahead = SplitTime(shift, spriteLength), behind = SplitTime(-shift, spriteLength);

            // Если по времени развести не вышло, осколки расходятся в стороны: кость — вдоль своей длины
            Vector2 a = motion.Evaluate(ahead), b = motion.Evaluate(behind);
            Vector2 axis = motion is IOrientedMotion ? (Vector2)transform.up
                         : (a - b).sqrMagnitude > 1e-4f ? (a - b).normalized : Vector2.right;
            float gap = Mathf.Max(0f, spriteLength * MinFragmentGap - Vector2.Dot(a - b, axis)) / 2f;

            var manager = GameManager.I.Projectiles;
            manager.Spawn(ProjectileType.Purple, motion, ahead, true, variant, Formation).Shift(offset + axis * gap);
            manager.Spawn(ProjectileType.Purple, motion, behind, true, variant, Formation).Shift(offset - axis * gap);

            Debris.Burst(Position, Color, 6, 3f);
            GameEvents.RaiseProjectileSplit(this);
            Kill();
        }

        // Момент на траектории, сдвинутый на shift, но не дальше maxDistance от текущего места пули
        float SplitTime(float shift, float maxDistance)
        {
            Vector2 here = motion.Evaluate(time);
            for (int i = 0; i < 8; i++)
            {
                float t = Mathf.Max(0f, time + shift);
                if (Vector2.Distance(motion.Evaluate(t), here) <= maxDistance * 1.05f) return t;
                shift *= 0.5f;
            }
            return time;
        }

        void Shift(Vector2 by)
        {
            offset = by;
            transform.position = Displaced(motion.Evaluate(time));
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
            if (Visible)   // за рамкой (за экраном) пуля исчезает молча
            {
                Debris.Burst(Position, Color, 8, 3f);
                Collapse();
            }
            Kill();
        }

        void SpawnHalves(Vector2 cutDir)
        {
            ArtLibrary.Halves(sr.sprite, out var left, out var right);
            float rot = Mathf.Atan2(cutDir.y, cutDir.x) * Mathf.Rad2Deg - 90f;
            Vector2 side = Quaternion.Euler(0, 0, rot) * Vector2.right;
            Color tint = sr.color;   // цветные спрайты не перекрашиваем повторно
            Debris.Spawn(left, Position, rot, 1f, tint, -side * 3f, Random.Range(-360f, -120f), 0.6f, 6f, false, 9);
            Debris.Spawn(right, Position, rot, 1f, tint, side * 3f, Random.Range(120f, 360f), 0.6f, 6f, false, 9);
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
