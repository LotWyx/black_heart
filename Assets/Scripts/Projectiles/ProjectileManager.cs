using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Реестр активных пуль: создание, разрезание, очистка.
    // Пули берутся из пула: объект создаётся один раз и дальше только переинициализируется (Projectile.Init),
    // а «убитая» пуля выключается и в конце кадра возвращается в пул. Так в бою нет ни создания,
    // ни уничтожения объектов, а значит и рывков от сборки мусора.
    public class ProjectileManager : MonoBehaviour
    {
        static GameBalance B => GameBalance.Current;

        public float sliceTolerance => B.sliceTolerance;
        public float blueRushDelay => B.blueRushDelay;

        readonly List<Projectile> active = new List<Projectile>();
        readonly Stack<Projectile> pool = new Stack<Projectile>();
        Transform root;
        float blueOnlyTime;

        // «Остановка времени» для пуль (обучение): пули замирают, но их можно резать
        public bool Frozen { get; set; }

        // В фазе защиты: если на поле остались одни синие пули, игра ждала бы только их.
        // Через blueRushDelay секунд они стремительно влетают в сердце (урона синие не наносят).
        public bool BlueRush { get; set; }

        public int Count => active.Count;
        public IReadOnlyList<Projectile> Active => active;

        // Статистика пула — чтобы подобрать GameBalance.projectilePoolSize
        public int Created { get; private set; }      // сколько объектов создано всего (заранее и по нехватке)
        public int PeakActive { get; private set; }   // наибольшее число пуль на поле одновременно

        void Awake()
        {
            root = new GameObject("Projectiles").transform;
            for (int i = 0; i < B.projectilePoolSize; i++) pool.Push(CreatePooled());
        }

        Projectile CreatePooled()
        {
            Created++;
            return Projectile.CreatePooled(root);
        }

        // variant — особый вид пули у паттерна (Bone, Spirit…); null — обычный вид врага
        // formation — пуля группы: видна только внутри рамки
        public Projectile Spawn(ProjectileType type, IProjectileMotion motion, float startTime = 0f, bool fragment = false,
            string variant = null, bool formation = false)
        {
            var p = pool.Count > 0 ? pool.Pop() : CreatePooled();
            p.Init(type, motion, startTime, fragment, variant, formation);
            active.Add(p);
            if (active.Count > PeakActive) PeakActive = active.Count;
            return p;
        }

        public void Slice(Vector2 a, Vector2 b, Vector2 dir)
        {
            // Осколки фиолетовой добавляются в конец списка, а убитые пули уходят из него только в LateUpdate,
            // поэтому достаточно пройти по пулям, которые были на момент разреза
            for (int i = 0, n = active.Count; i < n; i++)
            {
                var p = active[i];
                if (p == null || !p.CanBeSliced) continue;
                // синюю задевает только сам разрез (без запаса) — чтобы не зацепить её случайно у соседней пули
                float reach = p.Type == ProjectileType.Blue ? p.Radius * 0.8f : p.Radius + sliceTolerance;
                if (Geometry.DistancePointSegment(p.Position, a, b) <= reach)
                    p.Slice(dir);
            }
        }

        void Update()
        {
            if (!BlueRush || Frozen || !OnlyBlueLeft())
            {
                blueOnlyTime = 0f;
                return;
            }
            blueOnlyTime += Time.deltaTime;
            if (blueOnlyTime < blueRushDelay) return;
            foreach (var p in active)
                if (p != null) p.Rush();
        }

        // Очередь к сердцу: сколько одиночных (не синих, не из строя) пуль сейчас в зоне у сердца.
        // Если их уже NearLimit, следующие притормаживают на границе зоны и ждут — так к сердцу не подходит
        // больше пуль разом, чем можно успеть разрезать. Строй (стены, клетки, кольца) не трогается.
        // Размер зоны и лимит — в GameBalance.asset (раздел «Очередь к сердцу»)
        public static float NearZone => B.heartQueueZone;
        public static int NearLimit => B.heartQueueLimit;
        public int NearCount { get; private set; }

        // Убитые за кадр пули возвращаются в пул после того, как все отработали свой Update
        void LateUpdate()
        {
            int near = 0;
            Vector2 heart = GameManager.I != null ? GameManager.I.Player.Position : Vector2.zero;
            foreach (var p in active)
                if (p != null && p.Alive && p.InHeartQueue && Vector2.Distance(p.Position, heart) < NearZone) near++;
            NearCount = near;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p != null && p.Alive) continue;
                active.RemoveAt(i);
                if (p != null) pool.Push(p);
            }
        }

        bool OnlyBlueLeft()
        {
            int alive = 0;
            foreach (var p in active)
            {
                if (p == null || !p.Alive) continue;
                if (p.Type != ProjectileType.Blue) return false;
                alive++;
            }
            return alive > 0;
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null) continue;
                p.Explode();
                pool.Push(p);
            }
            active.Clear();
        }
    }

    // Ссылка на конкретное появление пули. Объект пули после гибели переиспользуется, поэтому хранить
    // сам Projectile между кадрами нельзя: через секунду это может быть уже другая пуля.
    public readonly struct ProjectileHandle
    {
        readonly Projectile projectile;
        readonly int spawnId;

        public ProjectileHandle(Projectile projectile)
        {
            this.projectile = projectile;
            spawnId = projectile != null ? projectile.SpawnId : 0;
        }

        public bool Alive => projectile != null && projectile.Alive && projectile.SpawnId == spawnId;
        public Vector2 Position => projectile.Position;
    }

    public static class Geometry
    {
        public static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
