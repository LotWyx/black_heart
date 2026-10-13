using UnityEngine;

namespace BlackHeart
{
    // Траектория, которая ещё и поворачивает пулю (кости в стене и клетке)
    public interface IOrientedMotion : IProjectileMotion
    {
        float AngleAt(float t);   // градусы; 0 — спрайт стоит вертикально
    }

    static class MotionMath
    {
        public static Vector2 Rotate(Vector2 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static float DirAngle(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
    }

    // Обёртка: пуля (кость) смотрит по направлению полёта любой траектории
    public sealed class FacingMotion : IOrientedMotion
    {
        readonly IProjectileMotion inner;
        public FacingMotion(IProjectileMotion inner) => this.inner = inner;
        public Vector2 Evaluate(float t) => inner.Evaluate(t);
        public float AngleAt(float t)
        {
            Vector2 v = inner.Evaluate(t + 0.03f) - inner.Evaluate(t);
            return v.sqrMagnitude > 1e-6f ? MotionMath.DirAngle(v) : 0f;
        }
    }

    // Стена костей: выезжает, стоит до launchAt, затем кость летит в цель.
    // Пока стена едет, кость стоит поперёк движения; в полёте смотрит на цель.
    public sealed class BoneWallMotion : IOrientedMotion
    {
        const float Tremble = 0.5f, Pixel = 1f / 16f;
        readonly Vector2 start, slideDir, stop, flyDir;
        readonly float slideSpeed, slideTime, launchAt, launchSpeed;

        public BoneWallMotion(Vector2 start, Vector2 slideDir, float slideDistance, float slideSpeed,
                              float launchAt, Vector2 target, float launchSpeed)
        {
            this.start = start;
            this.slideDir = slideDir.normalized;
            this.slideSpeed = slideSpeed;
            slideTime = slideDistance / slideSpeed;
            stop = start + this.slideDir * slideDistance;
            this.launchAt = Mathf.Max(launchAt, slideTime);
            this.launchSpeed = launchSpeed;
            flyDir = (target - stop).normalized;
        }

        public Vector2 Evaluate(float t)
        {
            if (t < slideTime) return start + slideDir * slideSpeed * t;
            if (t < launchAt)
            {
                // за полсекунды до броска кость дрожит на пиксель — живая подсказка «сейчас полетит»
                if (launchAt - t > Tremble) return stop;
                return stop + new Vector2(-slideDir.y, slideDir.x) * (Mathf.Sin(t * 70f) > 0f ? Pixel : -Pixel);
            }
            return stop + flyDir * launchSpeed * (t - launchAt);
        }

        public float AngleAt(float t)
        {
            if (t < launchAt) return MotionMath.DirAngle(slideDir) + 90f;   // кость поперёк хода стены
            return MotionMath.DirAngle(flyDir);
        }
    }

    // Клетка из костей: квадрат катится к цели и вращается как колесо, проходя через неё
    public sealed class CageMotion : IOrientedMotion
    {
        readonly Vector2 center0, dir, local;
        readonly float speed, angle0, angularSpeed, boneAngle;

        public CageMotion(Vector2 center0, Vector2 target, float speed, Vector2 local, float boneAngle, float angle0, float angularSpeed)
        {
            this.center0 = center0;
            dir = (target - center0).normalized;
            this.speed = speed;
            this.local = local;
            this.boneAngle = boneAngle;
            this.angle0 = angle0;
            this.angularSpeed = angularSpeed;
        }

        float Spin(float t) => angle0 + angularSpeed * t;
        public Vector2 Evaluate(float t) => center0 + dir * speed * t + MotionMath.Rotate(local, Spin(t));
        public float AngleAt(float t) => boneAngle + Spin(t) * Mathf.Rad2Deg;
    }

    // Спутник: крутится вокруг другой траектории; радиус сжимается, когда центр подлетает к цели
    public sealed class OrbitMotion : IProjectileMotion
    {
        readonly IProjectileMotion parent;
        readonly Vector2 target;
        readonly float radius, angle0, angularSpeed;

        public OrbitMotion(IProjectileMotion parent, Vector2 target, float radius, float angle0, float angularSpeed)
        {
            this.parent = parent;
            this.target = target;
            this.radius = radius;
            this.angle0 = angle0;
            this.angularSpeed = angularSpeed;
        }

        public Vector2 Evaluate(float t)
        {
            Vector2 c = parent.Evaluate(t);
            // спутники держатся на расстоянии от центра и смыкаются только у самой цели
            float r = radius * Mathf.Clamp01(Vector2.Distance(c, target) / 0.6f);
            float a = angle0 + angularSpeed * t;
            return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
    }

    // Точка контура прямоугольника вокруг цели: контур вращается и медленно сжимается в цель
    public sealed class RectRingMotion : IProjectileMotion
    {
        readonly Vector2 target, local;
        readonly float angle0, angularSpeed, shrinkTime;

        public RectRingMotion(Vector2 target, Vector2 local, float angle0, float angularSpeed, float shrinkTime)
        {
            this.target = target;
            this.local = local;
            this.angle0 = angle0;
            this.angularSpeed = angularSpeed;
            this.shrinkTime = shrinkTime;
        }

        public Vector2 Evaluate(float t)
        {
            float scale = 1f - t / shrinkTime;   // после нуля проходит насквозь — пуля всё равно уже попала
            return target + MotionMath.Rotate(local * scale, angle0 + angularSpeed * t);
        }
    }

    // Волна: пули вылетают из угла друг за другом и идут вдоль рамки синусоидой.
    // Когда голова волны доходит до стенки, вся волна замирает, а после паузы пули по очереди бросаются в цель.
    public sealed class WaveMotion : IProjectileMotion
    {
        readonly Vector2 start, dir, perp, target;
        readonly float speed, amplitude, frequency, stopAt, releaseAt, dashSpeed;
        readonly Vector2 stopPoint, releasePoint, dashDir;

        public WaveMotion(Vector2 start, Vector2 dir, float speed, float amplitude, float frequency,
                          float stopAt, float releaseAt, Vector2 target, float dashSpeed)
        {
            this.start = start;
            this.dir = dir.normalized;
            perp = new Vector2(-this.dir.y, this.dir.x);
            this.speed = speed;
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.stopAt = Mathf.Max(0f, stopAt);
            this.releaseAt = Mathf.Max(this.stopAt, releaseAt);
            this.target = target;
            this.dashSpeed = dashSpeed;
            stopPoint = Path(this.stopAt);
            releasePoint = Hover(this.releaseAt);
            dashDir = (target - releasePoint).normalized;
        }

        // Пока волна стоит, пули покачиваются на месте (не замирают — иначе кажется, что игра зависла)
        Vector2 Hover(float t) => stopPoint + perp * 0.18f * Mathf.Sin((t - stopAt) * 7f);

        Vector2 Path(float t) => start + dir * speed * t + perp * amplitude * Mathf.Sin(frequency * t);

        public Vector2 Evaluate(float t)
        {
            if (t < stopAt) return Path(t);
            if (t < releaseAt) return Hover(t);
            return releasePoint + dashDir * dashSpeed * (t - releaseAt);
        }
    }

    // Маятник: равномерно спускается к цели, раскачиваясь поперёк; размах гаснет у цели
    public sealed class PendulumMotion : IProjectileMotion
    {
        readonly Vector2 start, dir, perp;
        readonly float speed, length, amplitude, frequency, phase;

        public PendulumMotion(Vector2 start, Vector2 target, float speed, float amplitude, float frequency, float phase)
        {
            this.start = start;
            Vector2 d = target - start;
            length = d.magnitude;
            dir = d / Mathf.Max(length, 0.01f);
            perp = new Vector2(-dir.y, dir.x);
            this.speed = speed;
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.phase = phase;
        }

        public Vector2 Evaluate(float t)
        {
            float along = speed * t;
            float swing = amplitude * Mathf.Sin(frequency * t + phase) * Mathf.Clamp01(1f - along / length);
            return start + dir * along + perp * swing;
        }
    }

    // Муха: короткие резкие броски в случайные стороны с зависаниями, постепенно приближаясь к цели
    public sealed class FlyMotion : IProjectileMotion
    {
        readonly Vector2[] points;
        readonly float hop;          // длительность одного броска с зависанием
        readonly Vector2 exitDir;
        readonly float exitSpeed;
        const float Move = 0.6f;     // доля такта на бросок (остальное — зависание)

        public FlyMotion(Vector2 start, Vector2 target, int hops, float hopTime, float jitter)
        {
            hop = hopTime;
            points = new Vector2[hops + 1];
            points[0] = start;
            Vector2 d = target - start;
            Vector2 perp = new Vector2(-d.y, d.x).normalized;
            for (int i = 1; i < hops; i++)
            {
                float k = (float)i / hops;
                float j = jitter * (1f - k);
                points[i] = start + d * k + perp * Random.Range(-j, j) + (Vector2)Random.insideUnitCircle * j * 0.4f;
            }
            points[hops] = target;
            exitDir = (target - points[hops - 1]).normalized;
            exitSpeed = Vector2.Distance(points[hops - 1], target) / (hopTime * Move);
        }

        public Vector2 Evaluate(float t)
        {
            int last = points.Length - 1;
            if (t >= last * hop) return points[last] + exitDir * exitSpeed * (t - last * hop);
            int i = Mathf.Max(0, Mathf.FloorToInt(t / hop));
            float u = Mathf.Clamp01((t - i * hop) / (hop * Move));    // бросок занимает часть такта, дальше зависает
            u = 1f - (1f - u) * (1f - u);                             // резкий старт, торможение
            // в зависании муха жужжит на месте, а не стоит колом; на границах такта дрожь гаснет
            float phase = (t - i * hop) / hop;
            Vector2 buzz = new Vector2(Mathf.Sin(t * 47f), Mathf.Cos(t * 39f)) * 0.07f * Mathf.Sin(Mathf.PI * phase);
            return Vector2.Lerp(points[i], points[i + 1], u) + buzz;
        }
    }
}

namespace BlackHeart
{
    // Траектория, которая какое-то время не летит к сердцу, а носится вокруг: пока Roaming — без замедления у сердца
    public interface IRoamingMotion : IProjectileMotion
    {
        bool Roaming(float t);
    }

    // Зигзаг-пуля: стремительно влетает в рамку, мечется зигзагом вдоль её краёв (по точкам на вписанном
    // эллипсе — хорды не проходят рядом с сердцем) и только через «фору» бросается в сердце.
    // Фора отсчитывается с момента входа в рамку: пока пуля за рамкой, время на реакцию не тратится.
    public sealed class ZigzagDartMotion : IRoamingMotion
    {
        readonly Vector2[] points;     // старт за экраном, точки зигзага в рамке, цель
        readonly float[] times;        // момент прихода в каждую точку
        readonly float roamUntil;      // когда кончается зигзаг и начинается бросок

        public ZigzagDartMotion(Vector2 start, Rect box, Vector2 target, float headStart, float entrySpeed,
                                float zigzagSpeed, float dashSpeed)
        {
            float rx = box.width / 2f - 0.7f, ry = box.height / 2f - 0.6f;
            Vector2 c = box.center;
            var pts = new System.Collections.Generic.List<Vector2> { start };
            var ts = new System.Collections.Generic.List<float> { 0f };

            // первая точка — на эллипсе со стороны старта
            float a = Mathf.Atan2((start - c).y / ry, (start - c).x / rx);
            Vector2 OnEllipse(float ang) => c + new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry);
            Vector2 p = OnEllipse(a);
            float t = Vector2.Distance(start, p) / entrySpeed;
            pts.Add(p); ts.Add(t);
            float enteredAt = t;

            // зигзаг: каждый бросок — на 70–115° по эллипсу, направление скачет туда-обратно с перевесом вперёд
            float dir = Random.value < 0.5f ? 1f : -1f;
            while (t - enteredAt < headStart)
            {
                if (Random.value < 0.35f) dir = -dir;
                a += dir * Random.Range(70f, 115f) * Mathf.Deg2Rad;
                Vector2 next = OnEllipse(a);
                t += Vector2.Distance(p, next) / zigzagSpeed;
                p = next;
                pts.Add(p); ts.Add(t);
            }
            roamUntil = t;
            pts.Add(target); ts.Add(t + Vector2.Distance(p, target) / dashSpeed);
            // за сердцем — по прямой дальше (если промахнулась и её не разрезали, она уже попала)
            points = pts.ToArray();
            times = ts.ToArray();
        }

        public bool Roaming(float t) => t < roamUntil;

        public Vector2 Evaluate(float t)
        {
            if (t <= 0f) return points[0];
            for (int i = 1; i < times.Length; i++)
                if (t <= times[i]) return Vector2.Lerp(points[i - 1], points[i], (t - times[i - 1]) / (times[i] - times[i - 1]));
            int n = points.Length;
            Vector2 d = (points[n - 1] - points[n - 2]).normalized;
            float v = Vector2.Distance(points[n - 2], points[n - 1]) / (times[n - 1] - times[n - 2]);
            return points[n - 1] + d * v * (t - times[n - 1]);
        }
    }
}
