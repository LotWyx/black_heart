using UnityEngine;

namespace BlackHeart
{
    // Траектория пули — чистая функция времени. Благодаря этому осколки фиолетовой
    // пули продолжают тот же путь, просто со сдвигом по времени (впереди/позади).
    public interface IProjectileMotion
    {
        Vector2 Evaluate(float t);
    }

    // Прямо на цель
    public sealed class StraightMotion : IProjectileMotion
    {
        readonly Vector2 start, dir;
        readonly float speed;

        public StraightMotion(Vector2 start, Vector2 target, float speed)
        {
            this.start = start;
            dir = (target - start).normalized;
            this.speed = speed;
        }

        public Vector2 Evaluate(float t) => start + dir * speed * t;
    }

    // Спираль, сходящаяся к центру
    public sealed class SpiralMotion : IProjectileMotion
    {
        readonly Vector2 center;
        readonly float startRadius, startAngle, radialSpeed, angularSpeed;

        public SpiralMotion(Vector2 center, float startRadius, float startAngle, float radialSpeed, float angularSpeed)
        {
            this.center = center;
            this.startRadius = startRadius;
            this.startAngle = startAngle;
            this.radialSpeed = radialSpeed;
            this.angularSpeed = angularSpeed;
        }

        public Vector2 Evaluate(float t)
        {
            float r = Mathf.Max(0, startRadius - radialSpeed * t);
            float a = startAngle + angularSpeed * t;
            return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
    }

    // «Пьяная»: летит по дуге и хаотично качается вверх-вниз
    public sealed class DrunkMotion : IProjectileMotion
    {
        readonly Vector2 start, control, end;
        readonly float duration, amplitude, f1, f2, p1, p2;

        public DrunkMotion(Vector2 start, Vector2 target, float speed, float arc, float amplitude)
        {
            this.start = start;
            end = target;
            Vector2 d = target - start;
            control = (start + target) / 2f + new Vector2(-d.y, d.x).normalized * arc;
            duration = d.magnitude * 1.15f / speed;
            this.amplitude = amplitude;
            f1 = GameRandom.Range(3f, 6f);
            f2 = GameRandom.Range(6f, 10f);
            p1 = GameRandom.Range(0f, 6.3f);
            p2 = GameRandom.Range(0f, 6.3f);
        }

        public Vector2 Evaluate(float t)
        {
            float u = t / duration;
            float iu = 1f - u;
            Vector2 arcPos = iu * iu * start + 2f * iu * u * control + u * u * end;
            float wobble = amplitude * (Mathf.Sin(f1 * t + p1) + 0.6f * Mathf.Sin(f2 * t + p2));
            return arcPos + Vector2.up * wobble * Mathf.Clamp01(iu * 3f);
        }
    }

    // «Бум-круг»: летит от центра наружу, а через turnTime (2 с или удар о рамку) резко сворачивает к цели
    public sealed class BoomMotion : IProjectileMotion
    {
        readonly Vector2 center, dir, turnPoint, homingDir;
        readonly float speed, homingSpeed, turnTime;

        public BoomMotion(Vector2 center, Vector2 dir, float speed, float turnTime, Vector2 target, float homingSpeed)
        {
            this.center = center;
            this.dir = dir.normalized;
            this.speed = speed;
            this.turnTime = turnTime;
            this.homingSpeed = homingSpeed;
            turnPoint = center + this.dir * speed * turnTime;
            homingDir = (target - turnPoint).normalized;
        }

        public Vector2 Evaluate(float t)
        {
            if (t <= turnTime) return center + dir * speed * t;
            return turnPoint + homingDir * homingSpeed * (t - turnTime);
        }
    }
}
