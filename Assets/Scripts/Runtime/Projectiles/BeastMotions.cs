using UnityEngine;

namespace BlackHeart
{
    // Прыжок хищника: подкрасться к точке засады, замереть, присесть (дрожь на пиксель — подсказка «сейчас прыгнет»)
    // и броситься в цель. Если start == stand и approachTime == 0, пуля сразу стоит в засаде (зубы улыбки, клыки пасти).
    // Кошачьи головы не поворачиваются; клыки — через FacingPounce.
    public sealed class PounceMotion : IProjectileMotion
    {
        const float Crouch = 0.45f, Pixel = 1f / 16f;
        readonly Vector2 start, stand, dashDir;
        readonly float approachTime, launchAt, dashSpeed;

        public PounceMotion(Vector2 start, Vector2 stand, float approachTime, float launchAt, Vector2 target, float dashSpeed)
        {
            this.start = start;
            this.stand = stand;
            this.approachTime = Mathf.Max(0f, approachTime);
            this.launchAt = Mathf.Max(launchAt, this.approachTime);
            this.dashSpeed = dashSpeed;
            Vector2 d = target - stand;
            dashDir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.down;
        }

        public float LaunchAt => launchAt;

        public Vector2 Evaluate(float t)
        {
            if (t < approachTime)
            {
                float k = t / approachTime;
                return Vector2.Lerp(start, stand, 1f - (1f - k) * (1f - k));   // подкрадывается и тормозит у засады
            }
            if (t < launchAt)
            {
                if (launchAt - t > Crouch) return stand;
                return stand + new Vector2(-dashDir.y, dashDir.x) * (Mathf.Sin(t * 70f) > 0f ? Pixel : -Pixel);
            }
            return stand + dashDir * dashSpeed * (t - launchAt);
        }

        // Куда смотрит остриё: пока подкрадывается — по ходу, в засаде и в прыжке — на цель
        public float FaceAngle(float t)
        {
            if (t < approachTime && (stand - start).sqrMagnitude > 1e-6f) return MotionMath.DirAngle(stand - start);
            return MotionMath.DirAngle(dashDir);
        }
    }

    // Клык в засаде: та же траектория, остриё смотрит на цель даже пока стоит (FacingMotion берёт направление
    // из скорости, а у стоящей пули её нет)
    public sealed class FacingPounce : IOrientedMotion
    {
        readonly PounceMotion inner;
        public FacingPounce(PounceMotion inner) => this.inner = inner;
        public Vector2 Evaluate(float t) => inner.Evaluate(t);
        public float AngleAt(float t) => inner.FaceAngle(t);
    }

    // «Мышь» в кошках-мышках: бегает по краю рамки (на отступе inset), разворачиваясь в моменты reversals,
    // а в roamUntil бросается в цель. Пока бегает, к сердцу не ближе, чем край рамки минус inset.
    public sealed class PerimeterRunMotion : IRoamingMotion
    {
        readonly Rect lane;            // прямоугольник, по краю которого бежит мышь
        readonly float perimeter, s0, speed, roamUntil, dashSpeed;
        readonly float dir0;           // +1 — против часовой стрелки
        readonly float[] reversals;    // моменты разворота, по возрастанию
        readonly Vector2 dashFrom, dashDir;

        public PerimeterRunMotion(Rect box, float inset, float startFraction, float direction, float speed,
                                  float[] reversals, float roamUntil, Vector2 target, float dashSpeed)
        {
            lane = new Rect(box.xMin + inset, box.yMin + inset, box.width - 2f * inset, box.height - 2f * inset);
            perimeter = 2f * (lane.width + lane.height);
            s0 = Mathf.Repeat(startFraction, 1f) * perimeter;
            dir0 = direction >= 0f ? 1f : -1f;
            this.speed = speed;
            this.reversals = reversals ?? new float[0];
            this.roamUntil = roamUntil;
            this.dashSpeed = dashSpeed;
            dashFrom = OnLane(Distance(roamUntil));
            Vector2 d = target - dashFrom;
            dashDir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.zero;
        }

        public bool Roaming(float t) => t < roamUntil;

        public Vector2 Evaluate(float t)
        {
            if (t < roamUntil) return OnLane(Distance(t));
            return dashFrom + dashDir * dashSpeed * (t - roamUntil);
        }

        // Путь по краю к моменту t с учётом разворотов
        float Distance(float t)
        {
            float s = s0, sign = dir0, prev = 0f;
            foreach (float r in reversals)
            {
                if (r >= t) break;
                s += sign * speed * (r - prev);
                sign = -sign;
                prev = r;
            }
            return s + sign * speed * (t - prev);
        }

        // Точка на краю: низ слева направо, правый край снизу вверх, верх справа налево, левый сверху вниз
        Vector2 OnLane(float s)
        {
            s = Mathf.Repeat(s, perimeter);
            float w = lane.width, h = lane.height;
            if (s < w) return new Vector2(lane.xMin + s, lane.yMin);
            s -= w;
            if (s < h) return new Vector2(lane.xMax, lane.yMin + s);
            s -= h;
            if (s < w) return new Vector2(lane.xMax - s, lane.yMax);
            s -= w;
            return new Vector2(lane.xMin, lane.yMax - s);
        }
    }
}
