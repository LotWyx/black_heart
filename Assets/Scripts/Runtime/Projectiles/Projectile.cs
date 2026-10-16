using UnityEngine;

namespace BlackHeart
{
    // Универсальная пуля. Поведение задаётся типом (цвет и правила) и траекторией (IProjectileMotion).
    // Объекты пуль живут в пуле ProjectileManager: CreatePooled создаёт объект один раз, Init готовит его
    // к новому появлению, Kill выключает и отдаёт обратно.
    public class Projectile : MonoBehaviour
    {
        public ProjectileType Type { get; private set; }
        public bool IsFragment { get; private set; }     // осколок фиолетовой пули
        public float Radius { get; private set; } = 0.45f;   // тело пули ~15 px (кадр 17x17 вместе с ореолом)
        public Color Color => ProjectileTypes.ColorOf(Type);
        string skin;
        public Vector2 Position => transform.position;
        // Пуля группы (стена, клетка, волна, круг) видна только внутри рамки; одиночная прилетает
        // из-за края экрана и видна везде. Разрезать любую пулю можно только внутри рамки.
        public bool Formation { get; private set; }
        // Номер появления: растёт при каждом Init. По нему ProjectileHandle отличает «ту самую» пулю
        // от другой, которой достался тот же объект из пула.
        public int SpawnId { get; private set; }
        static int nextSpawnId;

        IProjectileMotion motion;
        float time;
        bool dead, reverseSpin, spinning, rushing, enteredFrame;
        Vector2 offset;      // сдвиг осколка от траектории, чтобы пара не сливалась; у сердца сходит на нет
        string variant; // особый вид пули у паттерна (Bone, Spirit…), передаётся осколкам
        ProjectileLook look;   // кадры и признаки вида: зависят от типа, врага и variant, берутся из кэша
        bool flipHalf;  // второй осколок пары — зеркальная половинка
        bool lengthwise; // половинки делят спрайт поперёк длины (кость: верх и низ), зеркалить надо по вертикали
        static bool nextHalfFlipped;
        int spawnFrame;
        SpriteAnimator anim;
        float sliceableAt;    // до этого момента пулю нельзя разрубить (пара кадров у осколков)
        SpriteRenderer sr, crack;   // crack — трещина поверх осколка, у которого нет своего спрайта половинки

        const float FragmentInvulnerability = 0.08f;
        const float FragmentGap = 1f;      // на какую долю длины спрайта осколки расходятся в стороны
        static readonly float[] DirectionProbes = { 0.05f, 0.25f, 0.6f, 1.2f, 2.5f };
        const float RushSpeed = 30f;         // рывок синей пули в сердце, юнитов в секунду
        const float SpinFrameTime = 0.14f;   // кадры пули — действия (зрачок, челюсть, пламя), не вращение
        static readonly int[] ReverseSpin = { 0, 3, 2, 1 };

        public bool Alive => !dead;

        // Новый объект для пула: компоненты создаются один раз, дальше пуля только переинициализируется
        public static Projectile CreatePooled(Transform parent)
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<Projectile>();
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sortingOrder = 10;
            p.anim = go.AddComponent<SpriteAnimator>();
            p.crack = new GameObject("Crack").AddComponent<SpriteRenderer>();
            p.crack.transform.SetParent(go.transform, false);
            p.crack.sortingOrder = 11;
            p.dead = true;
            go.SetActive(false);
            return p;
        }

        // Новое появление пули: сбрасывает всё состояние прошлой жизни объекта
        // Темп пули: во сколько раз быстрее (больше 1) или медленнее (меньше 1) она проходит свою траекторию.
        // Задаётся атакой (плотные атаки высокой сложности медленные, быстрые — разреженные), осколки наследуют.
        public float Pace { get; set; } = 1f;

        // skin — спрайт врага, в стиле которого рисуется пуля (череп, глаз, призрак); null — обычная пуля
        public void Init(ProjectileType type, IProjectileMotion motion, float startTime, bool fragment,
            string variant = null, bool formation = false, string skin = null)
        {
            Pace = 1f;
            SpawnId = ++nextSpawnId;
            Type = type;
            this.motion = motion;
            time = startTime;
            IsFragment = fragment;
            sliceableAt = fragment ? Time.time + FragmentInvulnerability : 0f;
            reverseSpin = Random.value < 0.5f;
            this.variant = variant;
            Formation = formation;
            lengthwise = variant == "Bone";
            dead = spinning = rushing = enteredFrame = flipHalf = false;
            offset = Vector2.zero;
            this.skin = skin;
            look = ProjectileLook.For(type, skin, variant, fragment);

            // Первый кадр — белая вспышка появления, дальше покадровое вращение
            anim.Stop();
            anim.enabled = true;
            sr.sprite = ArtLibrary.Get(ArtIds.BulletSpawn);
            sr.color = Color;
            sr.flipX = sr.flipY = false;
            sr.maskInteraction = formation ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;   // маска — рамка боя
            spawnFrame = Time.frameCount;

            if (look.half)
            {
                flipHalf = nextHalfFlipped;   // осколки рождаются парами: левый, затем правый
                nextHalfFlipped = !nextHalfFlipped;
            }

            bool cracked = fragment && !look.half;
            crack.gameObject.SetActive(cracked);
            if (cracked)
            {
                crack.sprite = ArtLibrary.Get(ArtIds.BulletCrack);
                crack.maskInteraction = sr.maskInteraction;
            }

            transform.SetPositionAndRotation(motion.Evaluate(startTime), Quaternion.identity);
            gameObject.SetActive(true);
            Orient();
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
                sr.flipX = look.half && flipHalf && !lengthwise;
                sr.flipY = look.half && flipHalf && lengthwise;
                sr.color = look.typed ? Color.white : Color;
                anim.Play(look.frames, SpinFrameTime, true, null, reverseSpin ? ReverseSpin : null);
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

            Vector2 before = Position;
            time += NearHeartStep(gm, Time.deltaTime * Pace * QueueFactor(gm));
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

            // Попадание — по всему пути за кадр, а не по конечной точке: при просадке FPS быстрая пуля
            // не «перепрыгнет» сердце
            if (Geometry.DistancePointSegment(heart.Position, before, Position) < heart.HitRadius + Radius * 0.6f)
                HitPlayer();
            else if (FarOffScreen(gm))
                Kill();
        }

        // Участвует ли пуля в очереди к сердцу (ProjectileManager.NearLimit)
        public bool InHeartQueue => !Formation && Type != ProjectileType.Blue && !(motion is IRoamingMotion r && r.Roaming(time));

        // У границы зоны сердца при полной очереди пуля почти замирает (зависает «в ожидании»), пока кто-то не уйдёт
        float QueueFactor(GameManager gm)
        {
            if (!InHeartQueue || gm.Projectiles.NearCount < ProjectileManager.NearLimit) return 1f;
            float d = Vector2.Distance(Position, gm.Player.Position);
            if (d < ProjectileManager.NearZone || d > ProjectileManager.NearZone + 1.2f) return 1f;   // уже внутри или ещё далеко
            return 0.12f;
        }

        // Ограничение скорости у сердца: в последних юнитах пути пуля не быстрее MaxNearSpeed, чтобы её всегда
        // можно было успеть разрезать (рывки мухи, броски из волны и стены). Траектория та же — замедляется её «время».
        const float SlowRadius = 3f, MaxNearSpeed = 2.6f;
        float NearHeartStep(GameManager gm, float dt)
        {
            if (dt <= 0f) return dt;
            if (motion is IRoamingMotion roam && roam.Roaming(time)) return dt;   // носится по рамке, не к сердцу
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
                // сначала штраф, потом урон: последний удар запускает смерть, и итоговый счёт должен уже учитывать −25
                gm.Score.Add(gm.Score.blueBulletPenalty, Position);
                gm.Player.TakeDamage(1);
            }
            else
            {
                gm.Score.Add(IsFragment ? gm.Score.purpleFragment : gm.Score.whiteBullet, Position);
            }

            SpawnHalves(cutDir);
            GameEvents.RaiseProjectileSliced(this);
            Kill();
        }

        // Фиолетовая распадается поперёк траектории: два осколка расходятся в стороны и летят бок о бок
        // тем же путём и в тот же момент времени, что и целая пуля. У сердца сдвиг сходит на нет (Displaced).
        // Пару кадров осколки неуязвимы, чтобы не разрубились тем же движением мгновенно.
        void Split()
        {
            float spriteLength = sr.sprite.bounds.size.x;
            Vector2 dir = TravelDirection();
            Vector2 side = new Vector2(-dir.y, dir.x) * (spriteLength * FragmentGap / 2f);

            var manager = GameManager.I.Projectiles;
            for (int k = 0; k < 2; k++)
            {
                var half = manager.Spawn(ProjectileType.Purple, motion, time, true, variant, Formation, skin);
                half.Shift(offset + (k == 0 ? side : -side));
                half.Pace = Pace;
            }

            Debris.Burst(Position, Color, 6, 3f);
            GameEvents.RaiseProjectileSplit(this);
            Kill();
        }

        // Куда пуля летит. Если она сейчас стоит или дрожит на месте (кость в стене, волна перед броском) —
        // куда двинется дальше; если и это неизвестно — к сердцу.
        Vector2 TravelDirection()
        {
            Vector2 here = motion.Evaluate(time);
            foreach (float dt in DirectionProbes)
            {
                Vector2 v = motion.Evaluate(time + dt) - here;
                if (v.magnitude > 0.2f) return v.normalized;
            }
            Vector2 toHeart = GameManager.I.Player.Position - here;
            return toHeart.sqrMagnitude > 1e-4f ? toHeart.normalized : Vector2.up;
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

        // Пуля выключается, а в пул её вернёт ProjectileManager в конце кадра
        void Kill()
        {
            dead = true;
            anim.Stop();
            gameObject.SetActive(false);
        }
    }
}
