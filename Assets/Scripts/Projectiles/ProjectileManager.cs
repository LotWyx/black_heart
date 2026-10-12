using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Реестр активных пуль: создание, разрезание, очистка.
    public class ProjectileManager : MonoBehaviour
    {
        [Tooltip("Насколько шире пули засчитывается разрез (юниты; 1/16 = 1 пиксель)")]
        public float sliceTolerance = 0.33f;
        [System.NonSerialized] public float blueRushDelay = 1f;   // сколько секунд одни синие ждут перед рывком в сердце

        readonly List<Projectile> active = new List<Projectile>();
        float blueOnlyTime;

        // «Остановка времени» для пуль (обучение): пули замирают, но их можно резать
        public bool Frozen { get; set; }

        // В фазе защиты: если на поле остались одни синие пули, игра ждала бы только их.
        // Через blueRushDelay секунд они стремительно влетают в сердце (урона синие не наносят).
        public bool BlueRush { get; set; }

        public int Count => active.Count;
        public IReadOnlyList<Projectile> Active => active;

        // variant — особый вид пули у паттерна (Bone, Spirit…); null — обычный вид врага
        // formation — пуля группы: видна и разрезается только внутри рамки
        public Projectile Spawn(ProjectileType type, IProjectileMotion motion, float startTime = 0f, bool fragment = false,
            string variant = null, bool formation = false)
        {
            var p = Projectile.Create(type, motion, startTime, fragment, variant, formation);
            active.Add(p);
            return p;
        }

        public void Unregister(Projectile p) => active.Remove(p);

        public void Slice(Vector2 a, Vector2 b, Vector2 dir)
        {
            // Копия списка: при разрезании пули добавляются и удаляются
            foreach (var p in active.ToArray())
            {
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
            foreach (var p in active.ToArray())
                if (p != null) p.Explode();
            active.Clear();
        }
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
