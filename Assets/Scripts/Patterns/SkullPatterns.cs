using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // ================= СКЕЛЕТ (враг «Череп») =================

    // Стена костей: вертикальный ряд костей выезжает слева или справа и встаёт на полпути. Все кости синие,
    // кроме одной белой: её нужно разрубить, не задев соседние. После короткой паузы вся стена разом летит
    // в сердце. Со сложностью растёт только скорость стены; бросок костей всегда одинаково быстрый.
    public class BoneWallPattern : AttackPattern
    {
        public const string ID = "bonewall";
        public override string Id => ID;
        const string Bone = "Bone";
        const float LaunchSpeed = 7f, StandTime = 0.8f;

        public override IEnumerator Run(AttackContext ctx)
        {
            int walls = ctx.Difficulty >= 0.8f ? 2 : 1;
            bool fromLeft = Random.value < 0.5f;
            for (int w = 0; w < walls; w++, fromLeft = !fromLeft)
            {
                Rect r = ctx.Box.Current;
                float wallSpeed = ctx.Ramp(2f, 7f, 0f, 2f);
                int count = Mathf.Max(5, Mathf.FloorToInt((r.height - 0.6f) / 0.85f));
                int white = Random.Range(0, count);
                float x0 = fromLeft ? r.xMin - 0.5f : r.xMax + 0.5f;
                Vector2 slideDir = fromLeft ? Vector2.right : Vector2.left;
                float slideDistance = Mathf.Abs(ctx.Target.x - x0) * 0.5f;
                float launchAt = slideDistance / wallSpeed + StandTime;

                var bones = new List<Projectile>();
                for (int i = 0; i < count; i++)
                {
                    float y = Mathf.Lerp(r.yMin + 0.5f, r.yMax - 0.5f, (float)i / (count - 1));
                    var type = i == white ? ProjectileType.White : ProjectileType.Blue;
                    var motion = new BoneWallMotion(new Vector2(x0, y), slideDir, slideDistance, wallSpeed, launchAt, ctx.Target, LaunchSpeed);
                    bones.Add(ctx.SpawnFormation(type, motion, Bone));
                }

                // следующая стена — с другой стороны и только когда эта закончилась
                float timeout = launchAt + 6f;
                while (AttackContext.AliveCount(bones) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                if (w < walls - 1) yield return new WaitForSeconds(0.3f);
            }
        }
    }

    // Клетка из костей: квадрат со стороной 6 костей катится слева и справа к сердцу, вращаясь как колесо.
    // Синих в клетке от 1 до 4, фиолетовых всего до 3. С середины сложности, когда от прошлых клеток
    // клетки могут прийти сверху и снизу вместо боков. Скорость растёт и после максимума.
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
            float speed = 1.5f + 0.35f * ctx.Difficulty;   // клетка не ползёт: фаза защиты не затягивается
            float k = Mathf.Clamp01(ctx.Difficulty);
            int bluePerCage = 1 + Mathf.RoundToInt(3 * k);
            int purpleTotal = Mathf.RoundToInt(3 * k);

            // Клеток всегда две. С середины сложности они могут прийти не с боков, а сверху и снизу —
            // вместо боковых, а не в добавок к ним: длина атаки со сложностью не растёт.
            float half = (PerSide - 1) * Spacing / 2f;
            bool vertical = ctx.Difficulty >= 0.5f && Random.value < 0.5f;
            Vector2 axis = vertical ? Vector2.up : Vector2.right;
            float reach = (vertical ? r.height : r.width) / 2f + half + 1f;
            int purpleLeft = purpleTotal;
            Cage(ctx, ctx.Target - axis * reach, speed, bluePerCage, ref purpleLeft, 2);
            // вторая клетка отстаёт: клетки не сходятся у сердца одновременно и синие не тонут среди чужих костей
            float lag = speed * 1.6f;
            Cage(ctx, ctx.Target + axis * (reach + lag), speed, bluePerCage, ref purpleLeft, 1);
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
            var skip = AttackContext.BlueGaps(types, true);

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
                    int idx = n++;
                    if (skip[idx]) continue;   // просвет рядом с синей костью
                    var motion = new CageMotion(center, ctx.Target, speed, local, boneAngle, angle0, spin);
                    list.Add(ctx.SpawnFormation(types[idx], motion, Bone));
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
            const float duration = 4f;   // длина атаки не растёт со сложностью — растёт только плотность
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

        // Кучка: 3–5 белых/фиолетовых с одного направления (±20°), а по её краям (≈20° в сторону) 1–2 синих:
        // кучку нужно разрубить, не задев их
        static IEnumerator Cluster(AttackContext ctx, float purpleShare)
        {
            int n = ctx.RampInt(3, 5, 0f, 1.4f);
            int blue = Random.value < 0.3f ? 1 : 2 + (Random.value < ctx.Ramp(0f, 0.4f, 0.6f, 1.5f) ? 1 : 0);
            var types = ctx.Types(n, 0, purpleShare);
            for (int i = 0; i < blue; i++) types.Add(ProjectileType.Blue);
            float baseAngle = Random.Range(0f, Mathf.PI * 2f);
            int blueIndex = 0;
            foreach (var type in types)
            {
                float angle = baseAngle + Random.Range(-0.35f, 0.35f);
                if (type == ProjectileType.Blue)
                {
                    float side = (blueIndex++ % 2 == 0) ? 1f : -1f;
                    angle = baseAngle + side * Random.Range(0.3f, 0.45f);
                }
                Vector2 dir = ctx.Dir(angle);
                Vector2 start = ctx.PointOffScreen(dir);
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
