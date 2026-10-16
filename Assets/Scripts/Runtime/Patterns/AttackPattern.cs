using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Базовый паттерн атаки: определяет, откуда и как летят пули.
    // Типы пуль паттерн получает от контекста (ctx.PlanTypes / ctx.NextType),
    // поэтому любой тип может лететь по любому паттерну.
    // Новый паттерн: унаследуйтесь, реализуйте Run и зарегистрируйте в PatternLibrary.
    public abstract class AttackPattern
    {
        public abstract string Id { get; }
        public abstract IEnumerator Run(AttackContext ctx);
    }

    // Темп атаки на высокой сложности: либо пуль много, но они медленные (и много фиолетовых),
    // либо они быстрые, но их меньше и синих почти нет. Normal — обычный темп.
    public enum AttackTempo { Normal, Dense, Fast }

    // Всё, что нужно паттерну: сложность, рамка, цель, правила баланса и создание пуль
    public class AttackContext
    {
        public float Difficulty;               // 0 — начало игры, 1 — «середина», дальше сложнее
        public BattleBox Box;
        public Vector2 Target;                 // позиция сердца
        public ProjectileManager Projectiles;
        public EnemyDefinition Enemy;

        public float SpecialShare = 0.25f;     // доля особых пуль — строго от общего числа
        public float MinReactionTime = 1.5f;   // минимум секунд от края рамки до сердца

        // Скорость, с которой путь distance займёт не меньше MinReactionTime
        public float CapSpeed(float speed, float distance) => Mathf.Min(speed, distance / MinReactionTime);

        readonly Queue<ProjectileType> deck = new Queue<ProjectileType>();

        // Значение от «начала игры» к «середине» по текущей сложности
        public float Lerp(float easy, float mid) => Mathf.LerpUnclamped(easy, mid, Difficulty);
        public int Count(int easy, int mid) => Mathf.Max(1, Mathf.RoundToInt(Lerp(easy, mid)));

        // ---------- типы пуль ----------

        // Готовит типы для count пуль: особых ровно round(count * SpecialShare).
        // На низкой сложности особые пули в одной атаке — только одного вида.
        public void PlanTypes(int count, bool allowBlue = true)
        {
            deck.Clear();
            var list = new List<ProjectileType>(count);
            int specials = Mathf.RoundToInt(count * SpecialShare);
            bool mixed = Difficulty >= 0.6f;
            ProjectileType single = Enemy.PickSpecialType(allowBlue);
            for (int i = 0; i < specials; i++)
                list.Add(mixed ? Enemy.PickSpecialType(allowBlue) : single);
            while (list.Count < count) list.Add(ProjectileType.White);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = GameRandom.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            foreach (var t in list) deck.Enqueue(t);
        }

        public ProjectileType NextType() => deck.Count > 0 ? deck.Dequeue() : ProjectileType.White;

        // Для колец: синее кольцо целиком синее и не смешивается с остальными
        public bool RollBlueRing() => GameRandom.value < SpecialShare * Enemy.BlueRatio;

        // ---------- темп ----------

        public AttackTempo Tempo = AttackTempo.Normal;
        public float Pace = 1f;          // темп траекторий всех пуль атаки
        public int FieldCap = 999;       // больше пуль на поле одиночные не появляются
        // Значения темпов — в GameBalance.asset (раздел «Высокая сложность: темп атак»)

        // Задать темп атаки (BattleController — перед каждой атакой серии)
        public void SetTempo(AttackTempo tempo)
        {
            var b = GameBalance.Current;
            Tempo = tempo;
            Pace = tempo == AttackTempo.Dense ? b.densePace : tempo == AttackTempo.Fast ? b.fastPace : 1f;
            FieldCap = tempo == AttackTempo.Dense ? b.denseFieldCap : tempo == AttackTempo.Fast ? b.fastFieldCap : b.normalFieldCap;
        }

        // Одиночная пуля в текущем темпе: в плотной атаке белые часто становятся фиолетовыми, в быструю
        // синие почти не попадают, а фиолетовые чаще становятся белыми. Перебор пуль на поле — пропуск.
        bool Admit(ref ProjectileType type)
        {
            if (Projectiles.Count >= FieldCap) return false;
            var b = GameBalance.Current;
            if (Tempo == AttackTempo.Dense && type == ProjectileType.White && GameRandom.value < b.denseWhiteToPurple) type = ProjectileType.Purple;
            if (Tempo == AttackTempo.Fast)
            {
                if (type == ProjectileType.Blue && GameRandom.value < b.fastBlueDrop) return false;
                if (type == ProjectileType.Purple && GameRandom.value < b.fastPurpleToWhite) type = ProjectileType.White;
            }
            return true;
        }

        Projectile Paced(Projectile p)
        {
            if (p != null) p.Pace = Pace;
            return p;
        }

        // Одиночная пуля (может не появиться: перебор на поле или быстрая атака без синих) — результат может быть null
        public Projectile Spawn(ProjectileType type, IProjectileMotion motion) => Spawn(type, motion, null);
        // variant — особый вид пули (Bone — кость Скелета, Spirit — призрак маятника)
        public Projectile Spawn(ProjectileType type, IProjectileMotion motion, string variant) =>
            Admit(ref type) ? Paced(Projectiles.Spawn(type, motion, 0f, false, variant)) : null;
        // Пуля группы (стена, клетка, волна, круг): за рамкой её не видно и не разрезать.
        // Строй не прореживается (иначе ломается рисунок), меняется только темп.
        public Projectile SpawnFormation(ProjectileType type, IProjectileMotion motion, string variant = null) =>
            Paced(Projectiles.Spawn(type, motion, 0f, false, variant, true));

        // ---------- для паттернов врагов ----------

        public MonoBehaviour Host;             // на нём паттерн может запускать параллельные корутины

        // Значение растёт от from (при сложности d0) до to (при d1) и дальше не меняется
        public float Ramp(float from, float to, float d0 = 0f, float d1 = 1f) =>
            Mathf.Lerp(from, to, Mathf.InverseLerp(d0, d1, Difficulty));
        public int RampInt(float from, float to, float d0 = 0f, float d1 = 1f) => Mathf.RoundToInt(Ramp(from, to, d0, d1));

        // Перемешанный набор типов: ровно blue синих, из остальных доля purpleShare — фиолетовые
        public List<ProjectileType> Types(int count, int blue, float purpleShare)
        {
            var list = new List<ProjectileType>(count);
            blue = Mathf.Clamp(blue, 0, count);
            int purple = Mathf.RoundToInt((count - blue) * Mathf.Clamp01(purpleShare));
            for (int i = 0; i < blue; i++) list.Add(ProjectileType.Blue);
            for (int i = 0; i < purple; i++) list.Add(ProjectileType.Purple);
            while (list.Count < count) list.Add(ProjectileType.White);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = GameRandom.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        // В строю (кольцо или линия, по порядку) рядом с синей не ставим других пуль: соседи синих пропускаются.
        // Иначе синюю невозможно не задеть, разрезая соседнюю.
        static readonly int[] Neighbours = { -1, 1 };

        public static bool[] BlueGaps(IList<ProjectileType> types, bool cyclic)
        {
            int n = types.Count;
            var skip = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (types[i] != ProjectileType.Blue) continue;
                foreach (int d in Neighbours)
                {
                    int j = i + d;
                    if (cyclic) j = (j + n) % n;
                    if (j >= 0 && j < n && types[j] != ProjectileType.Blue) skip[j] = true;
                }
            }
            return skip;
        }

        // Особый тип по соотношению врага (фиолетовый/синий) с заданной долей особых
        public ProjectileType RollType(float specialShare) =>
            GameRandom.value < specialShare ? Enemy.PickSpecialType() : ProjectileType.White;

        public static int AliveCount(List<ProjectileHandle> list)
        {
            int n = 0;
            foreach (var p in list) if (p.Alive) n++;
            return n;
        }

        // Ждать, пока на поле останется не больше count пуль (или истечёт timeout)
        public IEnumerator WaitFieldBelow(int count, float timeout)
        {
            while (Projectiles.Count > count && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public Vector2 Dir(float angleRad) => new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // ---------- скорость и направление ----------

        // Медленные пули остаются всегда, но с ростом сложности встречаются реже.
        // Скорость ограничена дистанцией: пуля не может долететь быстрее MinReactionTime.
        public float PickSpeed(float slow, float fast, float distance)
        {
            float bias = Mathf.Lerp(2.5f, 0.6f, Mathf.Clamp01(Difficulty / 1.5f));   // >1 — чаще медленные
            float speed = Mathf.Lerp(slow, fast, Mathf.Pow(GameRandom.value, bias));
            return Mathf.Min(speed, distance / MinReactionTime);
        }

        public Vector2 RandomDirection()
        {
            float a = GameRandom.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // Направление прилёта. Синяя летит рядом с какой-нибудь не синей пулей (в BlueNear градусах от неё):
        // её нужно обойти разрезом, а не просто не трогать где-то в стороне. Остальные — со случайной стороны.
        const float BlueNearMin = 14f, BlueNearMax = 26f;
        static readonly List<Vector2> directionBuffer = new List<Vector2>();
        public Vector2 DirectionFor(ProjectileType type)
        {
            if (type != ProjectileType.Blue) return RandomDirection();
            var others = directionBuffer;   // общий буфер: синие пули появляются часто, не мусорим списками
            others.Clear();
            foreach (var p in Projectiles.Active)
            {
                if (p == null || !p.Alive || p.Type == ProjectileType.Blue) continue;
                Vector2 rel = p.Position - Target;
                if (rel.sqrMagnitude > 1f) others.Add(rel);
            }
            if (others.Count == 0) return RandomDirection();
            Vector2 near = others[GameRandom.Range(0, others.Count)];
            float side = GameRandom.value < 0.5f ? 1f : -1f;
            return Dir(Mathf.Atan2(near.y, near.x) + side * GameRandom.Range(BlueNearMin, BlueNearMax) * Mathf.Deg2Rad);
        }

        // Скорость сближения спирали: пуля стартует за экраном, но путь от края рамки до сердца занимает duration
        public float SpiralSpeed(Vector2 dir, float duration) => (Box.DistanceToEdge(Target, dir) + 0.6f) / duration;

        // Одиночные пули прилетают из-за края экрана. Расстояние от сердца до точки за экраном по направлению dir
        public float OffScreenDistance(Vector2 dir, float margin = 1f)
        {
            Rect v = GameManager.I.Pixel.ViewRect;
            dir = dir.normalized;
            float tx = dir.x > 1e-5f ? (v.xMax - Target.x) / dir.x : dir.x < -1e-5f ? (v.xMin - Target.x) / dir.x : float.PositiveInfinity;
            float ty = dir.y > 1e-5f ? (v.yMax - Target.y) / dir.y : dir.y < -1e-5f ? (v.yMin - Target.y) / dir.y : float.PositiveInfinity;
            return Mathf.Min(tx, ty) + margin;
        }

        public Vector2 PointOffScreen(Vector2 dir, float margin = 1f) => Target + dir.normalized * OffScreenDistance(dir, margin);

        // Случайная точка в рамке не ближе minDistance к сердцу (или самая дальняя из попыток)
        public Vector2 RandomPointInBox(float minDistance, float edgeMargin)
        {
            Rect r = Box.Current;
            Vector2 best = Target;
            float bestDist = -1f;
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector2(GameRandom.Range(r.xMin + edgeMargin, r.xMax - edgeMargin),
                                    GameRandom.Range(r.yMin + edgeMargin, r.yMax - edgeMargin));
                float d = Vector2.Distance(p, Target);
                if (d >= minDistance) return p;
                if (d > bestDist) { bestDist = d; best = p; }
            }
            return best;
        }
    }
}
