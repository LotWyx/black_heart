using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // ================= СКЕЛЕТ (враг «Череп») =================

    // Стена костей: вертикальный ряд костей выезжает слева или справа, встаёт на полпути,
    // и кости по очереди летят в сердце. Сложность сначала разгоняет стену, а когда она
    // достигла предела — ускоряет полёт костей.
    public class BoneWallPattern : AttackPattern
    {
        public const string ID = "bonewall";
        public override string Id => ID;
        const string Bone = "Bone";

        public override IEnumerator Run(AttackContext ctx)
        {
            int walls = ctx.Difficulty >= 0.8f ? 2 : 1;
            bool fromLeft = Random.value < 0.5f;
            for (int w = 0; w < walls; w++, fromLeft = !fromLeft)
            {
                Rect r = ctx.Box.Current;
                float wallSpeed = ctx.Ramp(1.5f, 4.5f);                                  // растёт до сложности 1
                float launchSpeed = Mathf.Min(7f, 3f + Mathf.Max(0f, ctx.Difficulty - 1f) * 4f);   // потом — полёт костей
                float stagger = ctx.Ramp(0.24f, 0.12f, 0f, 1.6f);

                int count = Mathf.Max(5, Mathf.FloorToInt((r.height - 0.6f) / 0.85f));
                var types = ctx.Types(count, Random.Range(1, 3), ctx.Ramp(0f, 0.3f, 0.2f, 1.4f));
                float x0 = fromLeft ? r.xMin - 0.5f : r.xMax + 0.5f;
                Vector2 slideDir = fromLeft ? Vector2.right : Vector2.left;
                float slideDistance = Mathf.Abs(ctx.Target.x - x0) * 0.5f;
                float slideTime = slideDistance / wallSpeed;

                // Порядок вылета: от краёв стены к середине, со случайной перестановкой соседей
                var order = new List<int>();
                for (int i = 0; i < count; i++) order.Add(i);
                order.Sort((a, b) => Mathf.Abs(b - (count - 1) / 2f).CompareTo(Mathf.Abs(a - (count - 1) / 2f)));
                for (int i = 0; i + 1 < count; i += 2) if (Random.value < 0.5f) (order[i], order[i + 1]) = (order[i + 1], order[i]);

                for (int k = 0; k < count; k++)
                {
                    int i = order[k];
                    float y = Mathf.Lerp(r.yMin + 0.5f, r.yMax - 0.5f, count == 1 ? 0.5f : (float)i / (count - 1));
                    var start = new Vector2(x0, y);
                    float launchAt = slideTime + 0.35f + k * stagger;
                    // кость стоит в стене на виду, но бросок всё равно не быстрее времени реакции
                    float speed = ctx.CapSpeed(launchSpeed, Vector2.Distance(start + slideDir * slideDistance, ctx.Target));
                    ctx.Spawn(types[i], new BoneWallMotion(start, slideDir, slideDistance, wallSpeed, launchAt, ctx.Target, speed), Bone);
                }
                yield return new WaitForSeconds(slideTime + 0.35f + count * stagger * 0.7f);
            }
        }
    }

    // Клетка из костей: квадрат со стороной 6 костей катится слева и справа к сердцу, вращаясь как колесо.
    // Синих в клетке от 1 до 4, фиолетовых всего до 3. С середины сложности, когда от прошлых клеток
    // осталось меньше половины костей, приходят клетки сверху и снизу. Скорость растёт и после максимума.
    public class BoneCagePattern : AttackPattern
    {
        public const string ID = "bonecage";
        public override string Id => ID;
        const string Bone = "Bone";
        const float Spacing = 0.62f;     // шаг костей по стороне
        const int PerSide = 6;           // костей на стороне, считая углы

        public override IEnumerator Run(AttackContext ctx)
        {
            Rect r = ctx.Box.Current;
            float speed = 1.0f + 0.45f * ctx.Difficulty;
            float k = Mathf.Clamp01(ctx.Difficulty);
            int bluePerCage = 1 + Mathf.RoundToInt(3 * k);
            int purpleTotal = Mathf.RoundToInt(3 * k);

            var bones = new List<Projectile>();
            float half = (PerSide - 1) * Spacing / 2f;
            int purpleLeft = purpleTotal;
            bones.AddRange(Cage(ctx, new Vector2(r.xMin - half - 1f, ctx.Target.y), speed, bluePerCage, ref purpleLeft, 2));
            bones.AddRange(Cage(ctx, new Vector2(r.xMax + half + 1f, ctx.Target.y), speed, bluePerCage, ref purpleLeft, 1));

            if (ctx.Difficulty >= 0.5f)
            {
                int spawned = bones.Count;
                float timeout = 12f;
                while (AttackContext.AliveCount(bones) > spawned / 2 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                int purpleMore = purpleTotal;
                Cage(ctx, new Vector2(ctx.Target.x, r.yMax + half + 1f), speed, bluePerCage, ref purpleMore, 2);
                Cage(ctx, new Vector2(ctx.Target.x, r.yMin - half - 1f), speed, bluePerCage, ref purpleMore, 1);
            }
            yield return null;
        }

        // Одна клетка: 20 костей по контуру квадрата. purpleCages — на сколько клеток ещё делятся фиолетовые
        static List<Projectile> Cage(AttackContext ctx, Vector2 center, float speed, int blue, ref int purpleLeft, int purpleCages)
        {
            int perimeter = (PerSide - 1) * 4;
            int purple = purpleCages <= 1 ? purpleLeft : Random.Range(0, purpleLeft + 1);
            purpleLeft -= purple;
            var types = new List<ProjectileType>();
            for (int i = 0; i < blue; i++) types.Add(ProjectileType.Blue);
            for (int i = 0; i < purple; i++) types.Add(ProjectileType.Purple);
            while (types.Count < perimeter) types.Add(ProjectileType.White);
            for (int i = types.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (types[i], types[j]) = (types[j], types[i]); }

            // Колесо катится: угловая скорость = скорость / радиус, направление — по ходу движения
            Vector2 dir = (ctx.Target - center).normalized;
            float radius = (PerSide - 1) * Spacing / 2f;
            float spin = -Mathf.Sign(dir.x != 0 ? dir.x : -dir.y) * speed / (radius * 1.2f);
            float angle0 = Random.Range(0f, Mathf.PI / 2f);

            var list = new List<Projectile>();
            float half = radius;
            var corners = new[] { new Vector2(-half, -half), new Vector2(half, -half), new Vector2(half, half), new Vector2(-half, half) };
            int n = 0;
            for (int side = 0; side < 4; side++)
            {
                Vector2 a = corners[side], b = corners[(side + 1) % 4];
                float boneAngle = (side % 2 == 0) ? 90f : 0f;   // кость лежит вдоль стороны
                for (int j = 0; j < PerSide - 1; j++)
                {
                    Vector2 local = Vector2.Lerp(a, b, (float)j / (PerSide - 1));
                    var motion = new CageMotion(center, ctx.Target, speed, local, boneAngle, angle0, spin);
                    list.Add(ctx.Spawn(types[n++], motion, Bone));
                }
            }
            return list;
        }
    }

    // Стандартная атака Скелета: кучки черепов вперемешку с редкими костями, прямо и «пьяно».
    // Рядом с каждой кучкой — 1–2 синих. Растут частота кучек, длительность и доля фиолетовых (до 50%).
    // Иногда (1–2 раза за атаку) посреди всего взрывается бум-круг из черепов.
    public class SkullStandardPattern : AttackPattern
    {
        public const string ID = "skull_std";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            float duration = ctx.Ramp(7f, 12f, 0f, 1.5f);
            float interval = ctx.Ramp(1.7f, 0.8f, 0f, 1.5f);
            float purpleShare = ctx.Ramp(0.1f, 0.5f, 0f, 1.2f);

            // бум-круги в случайные моменты атаки
            if (Random.value < ctx.Ramp(0.35f, 0.8f))
            {
                int booms = Random.value < ctx.Ramp(0.2f, 0.6f) ? 2 : 1;
                for (int b = 0; b < booms; b++) ctx.Host.StartCoroutine(DelayedBoom(ctx, Random.Range(1f, duration - 1.5f), purpleShare));
            }

            float t = 0f;
            while (t < duration)
            {
                yield return Cluster(ctx, purpleShare);
                yield return new WaitForSeconds(interval);
                t += interval + 0.4f;
            }
        }

        static IEnumerator DelayedBoom(AttackContext ctx, float delay, float purpleShare)
        {
            yield return new WaitForSeconds(delay);
            var types = ctx.Types(ctx.RampInt(5, 8), 1, purpleShare);
            yield return BoomCirclePattern.Burst(ctx, BoomCirclePattern.RandomCenter(ctx), types, 2f, 2.6f);
        }

        // Кучка: 3–5 белых/фиолетовых и 1–2 синих с одного направления (±20°)
        static IEnumerator Cluster(AttackContext ctx, float purpleShare)
        {
            int n = ctx.RampInt(3, 5, 0f, 1.4f);
            int blue = Random.value < 0.5f ? 1 : 2;
            var types = ctx.Types(n + blue, blue, purpleShare);
            float baseAngle = Random.Range(0f, Mathf.PI * 2f);
            foreach (var type in types)
            {
                Vector2 dir = ctx.Dir(baseAngle + Random.Range(-0.35f, 0.35f));
                Vector2 start = ctx.PointOutsideBox(dir);
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir);
                bool drunk = Random.value < 0.5f;
                float speed = ctx.PickSpeed(drunk ? 1.6f : 2.2f, drunk ? 3f : 4.2f, distance * (drunk ? 1.15f : 1f));
                IProjectileMotion motion = drunk
                    ? new DrunkMotion(start, ctx.Target, speed, Random.Range(1.5f, 3f) * (Random.value < 0.5f ? 1 : -1), ctx.Ramp(0.5f, 0.8f))
                    : new StraightMotion(start, ctx.Target, speed);
                bool bone = type != ProjectileType.Blue && Random.value < 0.2f;   // редкие кости среди черепов
                if (bone) ctx.Spawn(type, new FacingMotion(motion), "Bone");
                else ctx.Spawn(type, motion);
                yield return new WaitForSeconds(0.12f);
            }
        }
    }
}
