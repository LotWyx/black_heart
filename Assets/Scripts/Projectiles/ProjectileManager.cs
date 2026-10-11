using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Реестр активных пуль: создание, разрезание, очистка.
    public class ProjectileManager : MonoBehaviour
    {
        [Tooltip("Насколько шире пули засчитывается разрез (юниты; 1/16 = 1 пиксель)")]
        public float sliceTolerance = 0.33f;

        readonly List<Projectile> active = new List<Projectile>();

        // «Остановка времени» для пуль (обучение): пули замирают, но их можно резать
        public bool Frozen { get; set; }

        public int Count => active.Count;
        public IReadOnlyList<Projectile> Active => active;

        public Projectile Spawn(ProjectileType type, IProjectileMotion motion, float startTime = 0f, bool fragment = false)
        {
            var p = Projectile.Create(type, motion, startTime, fragment);
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
                if (Geometry.DistancePointSegment(p.Position, a, b) <= p.Radius + sliceTolerance)
                    p.Slice(dir);
            }
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
