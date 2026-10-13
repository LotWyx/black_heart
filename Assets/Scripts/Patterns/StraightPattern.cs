using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Простейшая атака: пули по одной летят из-за экрана прямо в сердце
    public class StraightPattern : AttackPattern
    {
        public override string Summary =>
            "Прямые пули: по одной вылетают из-за экрана и летят прямо в сердце. Доля особых пуль — общая, из баланса игры. " +
            "Синяя прилетает рядом с какой-нибудь не синей. Сейчас ни одному врагу не назначена.";

        [Tooltip("Сколько пуль")]
        public Ramp count = new Ramp(4f, 10f);
        [Tooltip("Секунд между пулями")]
        public Ramp interval = new Ramp(0.6f, 0.3f);
        [Tooltip("Самая медленная и самая быстрая пуля. Медленные с ростом сложности встречаются реже")]
        public float slowSpeed = 2.2f;
        public float fastSpeed = 4.5f;

        public override IEnumerator Run(AttackContext ctx)
        {
            int n = Mathf.Max(1, ctx.Int(count));
            float dt = ctx.Value(interval);
            ctx.PlanTypes(n);

            for (int i = 0; i < n; i++)
            {
                var type = ctx.NextType();
                Vector2 dir = ctx.DirectionFor(type);
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir);
                float speed = ctx.PickSpeed(slowSpeed, fastSpeed, distance);
                ctx.Spawn(type, new StraightMotion(ctx.PointOffScreen(dir), ctx.Target, speed));
                yield return new WaitForSeconds(dt);
            }
        }
    }
}
