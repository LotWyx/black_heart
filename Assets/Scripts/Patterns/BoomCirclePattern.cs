using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // В случайном месте (не близко к сердцу) появляется полупрозрачный круг, сжимается и
    // выпускает кольцо пуль. Пули летят от центра, а через 2 секунды или при ударе о рамку
    // сворачивают к сердцу. Дистанцию здесь не увеличить, поэтому скорость не растёт со сложностью.
    public class BoomCirclePattern : AttackPattern
    {
        public const string ID = "boom";
        public override string Id => ID;

        const float HoldTime = 1f, ShrinkTime = 0.25f, MaxStraightTime = 2f;
        const float CircleDiameter = 4f, MinDistanceToHeart = 4.5f;
        const float Speed = 2.2f, HomingSpeed = 2.8f;

        public override IEnumerator Run(AttackContext ctx)
        {
            int circles = ctx.Count(1, 2);
            for (int c = 0; c < circles; c++)
            {
                // Круг целиком помещается в рамку
                Vector2 center = ctx.RandomPointInBox(MinDistanceToHeart, CircleDiameter / 2f + 0.2f);
                BoomCircleFx.Spawn(center, CircleDiameter, 0.5f, HoldTime, ShrinkTime);
                yield return new WaitForSeconds(HoldTime + ShrinkTime);

                int count = ctx.Count(4, 8);
                bool blue = ctx.RollBlueRing();
                if (!blue) ctx.PlanTypes(count, allowBlue: false);

                float offset = Random.Range(0f, Mathf.PI * 2f);
                for (int i = 0; i < count; i++)
                {
                    float a = offset + Mathf.PI * 2f * i / count;
                    Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    float turn = Mathf.Min(MaxStraightTime, ctx.Box.TimeToExit(center, dir * Speed));
                    var type = blue ? ProjectileType.Blue : ctx.NextType();
                    ctx.Spawn(type, new BoomMotion(center, dir, Speed, turn, ctx.Target, HomingSpeed));
                }
                yield return new WaitForSeconds(ctx.Lerp(2f, 1.4f));
            }
        }
    }
}
