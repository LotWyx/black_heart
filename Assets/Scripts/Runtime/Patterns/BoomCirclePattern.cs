using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // В случайном месте (не близко к сердцу) появляется метка, сжимается и выпускает кольцо пуль.
    // Пули летят от центра, а через 2 секунды или при ударе о рамку сворачивают к сердцу.
    // Сам взрыв (Burst) используют и паттерны врагов: Скелет — черепами внутри стандартной атаки,
    // Глаз — серией кругов, в каждом ровно один синий.
    public class BoomCirclePattern : AttackPattern
    {
        public const string ID = "boom";
        public override string Id => ID;

        public const float HoldTime = 1f, ShrinkTime = 0.25f, MaxStraightTime = 2f;
        public const float CircleDiameter = 4f, MinDistanceToHeart = 4.5f, StartRadius = 0.7f;
        const float Speed = 2.2f, HomingSpeed = 2.8f;

        public override IEnumerator Run(AttackContext ctx)
        {
            int circles = ctx.Count(1, 2);
            for (int c = 0; c < circles; c++)
            {
                int count = ctx.Count(4, 8);
                List<ProjectileType> types;
                if (ctx.RollBlueRing())
                {
                    types = new List<ProjectileType>();
                    for (int i = 0; i < count; i++) types.Add(ProjectileType.Blue);
                }
                else
                {
                    ctx.PlanTypes(count, allowBlue: false);
                    types = new List<ProjectileType>();
                    for (int i = 0; i < count; i++) types.Add(ctx.NextType());
                }
                yield return Burst(ctx, RandomCenter(ctx), types, Speed, HomingSpeed);
                yield return new WaitForSeconds(ctx.Lerp(2f, 1.4f));
            }
        }

        // Точка для круга: целиком в рамке и не ближе MinDistanceToHeart к сердцу
        public static Vector2 RandomCenter(AttackContext ctx) =>
            ctx.RandomPointInBox(MinDistanceToHeart, CircleDiameter / 2f + 0.2f);

        // Метка → пауза → кольцо пуль заданных типов. Корутина заканчивается в момент выстрела.
        public static IEnumerator Burst(AttackContext ctx, Vector2 center, IList<ProjectileType> types, float speed, float homingSpeed, string variant = null)
        {
            BoomCircleFx.Spawn(center, CircleDiameter, 0.5f, HoldTime, ShrinkTime);
            yield return new WaitForSeconds(HoldTime + ShrinkTime);

            int count = types.Count;
            float offset = GameRandom.Range(0f, Mathf.PI * 2f);
            var skip = AttackContext.BlueGaps(types, true);   // по бокам от синей в кольце — пусто
            for (int i = 0; i < count; i++)
            {
                if (skip[i]) continue;
                float a = offset + Mathf.PI * 2f * i / count;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 from = center + dir * StartRadius;   // кольцо рождается уже раскрытым, а не кучей в одной точке
                float turn = Mathf.Min(MaxStraightTime, ctx.Box.TimeToExit(from, dir * speed));
                // синяя сворачивает к сердцу медленнее — приходит позже белых, а не в одной куче с ними
                // кольцо долетает к сердцу по очереди, а не всё в один миг
                float homing = types[i] == ProjectileType.Blue ? homingSpeed * 0.6f : homingSpeed * (1f - 0.3f * i / count);
                ctx.SpawnFormation(types[i], new BoomMotion(from, dir, speed, turn, ctx.Target, homing), variant);
            }
        }
    }
}
