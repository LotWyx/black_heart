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
            bool fromLeft = GameRandom.value < 0.5f;
            for (int w = 0; w < walls; w++, fromLeft = !fromLeft)
            {
                Rect r = ctx.Box.Current;
                float wallSpeed = ctx.Ramp(2f, 7f, 0f, 2f);
                int count = Mathf.Max(5, Mathf.FloorToInt((r.height - 0.6f) / 0.85f));
                int white = GameRandom.Range(0, count);
                float x0 = fromLeft ? r.xMin - 0.5f : r.xMax + 0.5f;
                Vector2 slideDir = fromLeft ? Vector2.right : Vector2.left;
                float slideDistance = Mathf.Abs(ctx.Target.x - x0) * 0.5f;
                float launchAt = slideDistance / wallSpeed + StandTime;

                var bones = new List<ProjectileHandle>();
                for (int i = 0; i < count; i++)
                {
                    float y = Mathf.Lerp(r.yMin + 0.5f, r.yMax - 0.5f, (float)i / (count - 1));
                    var type = i == white ? ProjectileType.White : ProjectileType.Blue;
                    var motion = new BoneWallMotion(new Vector2(x0, y), slideDir, slideDistance, wallSpeed, launchAt, ctx.Target, LaunchSpeed);
                    bones.Add(new ProjectileHandle(ctx.SpawnFormation(type, motion, Bone)));
                }

                // следующая стена — с другой стороны и только когда эта закончилась
                float timeout = launchAt + 6f;
                while (AttackContext.AliveCount(bones) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                if (w < walls - 1) yield return new WaitForSeconds(0.3f);
            }
        }
    }

    // Клетка из костей: квадрат со стороной 6 костей катится слева и справа к сердцу, вращаясь как колесо.
    // Синих в клетке от 1 до 3, фиолетовых всего до 3 (и те и другие — с ростом сложности). С середины
    // сложности клетки могут прийти сверху и снизу вместо боков. Скорость растёт и после максимума.
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
            int bluePerCage = 1 + Mathf.RoundToInt(3 * Mathf.Clamp01(ctx.Difficulty - 0.3f));   // до 0.3 — одна синяя
            int purpleTotal = Mathf.RoundToInt(3 * Mathf.Clamp01((ctx.Difficulty - 0.3f) / 0.7f));  // фиолетовые с 0.3

            // Клеток всегда две. С середины сложности они могут прийти не с боков, а сверху и снизу —
            // вместо боковых, а не в добавок к ним: длина атаки со сложностью не растёт.
            float half = (PerSide - 1) * Spacing / 2f;
            bool vertical = ctx.Difficulty >= 0.5f && GameRandom.value < 0.5f;
            Vector2 axis = vertical ? Vector2.up : Vector2.right;
            float reach = (vertical ? r.height : r.width) / 2f + half + 1f;
            int purpleLeft = purpleTotal;
            Cage(ctx, ctx.Target - axis * reach, speed, bluePerCage, ref purpleLeft, 2);
            // вторая клетка отстаёт: клетки не сходятся у сердца одновременно и синие не тонут среди чужих костей
            float lag = speed * ctx.Ramp(3f, 1.6f);   // на низкой сложности клетки приходят по очереди, а не почти разом
            Cage(ctx, ctx.Target + axis * (reach + lag), speed, bluePerCage, ref purpleLeft, 1);
            yield return null;
        }

        // Одна клетка: 20 костей по контуру квадрата. purpleCages — на сколько клеток ещё делятся фиолетовые
        static List<Projectile> Cage(AttackContext ctx, Vector2 center, float speed, int blue, ref int purpleLeft, int purpleCages)
        {
            bool sparse = ctx.Difficulty < 0.5f;
            int perimeter = (PerSide - 1) * 4;
            int purple = purpleCages <= 1 ? purpleLeft : GameRandom.Range(0, purpleLeft + 1);
            purpleLeft -= purple;
            var types = new List<ProjectileType>();
            for (int i = 0; i < blue; i++) types.Add(ProjectileType.Blue);
            for (int i = 0; i < purple; i++) types.Add(ProjectileType.Purple);
            while (types.Count < perimeter) types.Add(ProjectileType.White);
            for (int i = types.Count - 1; i > 0; i--) { int j = GameRandom.Range(0, i + 1); (types[i], types[j]) = (types[j], types[i]); }
            var skip = AttackContext.BlueGaps(types, true);

            // Колесо катится: угловая скорость = скорость / радиус, направление — по ходу движения
            Vector2 dir = (ctx.Target - center).normalized;
            float radius = (PerSide - 1) * Spacing / 2f;
            float spin = -Mathf.Sign(dir.x != 0 ? dir.x : -dir.y) * speed / (radius * 1.2f);
            float angle0 = GameRandom.Range(0f, Mathf.PI / 2f);

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
                    // на низкой сложности каркас редкий: белые кости через одну (углы остаются — клетку видно)
                    if (sparse && j % 2 == 1 && types[idx] == ProjectileType.White) continue;
                    var motion = new CageMotion(center, ctx.Target, speed, local, boneAngle, angle0, spin);
                    list.Add(ctx.SpawnFormation(types[idx], motion, Bone));
                }
            }
            return list;
        }
    }

    // Стандартная атака Скелета: кучки черепов вперемешку с редкими костями, прямо и «пьяно».
    // Рядом с каждой кучкой синие (на низкой сложности одна и в стороне). Растут частота кучек и доля
    // фиолетовых (с нуля до 50%). С середины сложности посреди всего взрывается бум-круг из черепов.
    public class SkullStandardPattern : AttackPattern
    {
        public const string ID = "skull_std";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            const float duration = 4f;   // длина атаки не растёт со сложностью — растёт только плотность
            float interval = ctx.Ramp(2.4f, 0.8f, 0f, 1.5f);           // на низкой сложности кучки редкие
            float purpleShare = ctx.Ramp(0f, 0.5f, 0.2f, 1.2f);        // фиолетовые — только с ростом сложности

            // бум-круги в случайные моменты атаки: на низкой сложности их нет, второй — только на высокой
            int booms = 0;
            if (GameRandom.value < ctx.Ramp(0f, 0.8f, 0.3f, 1.2f))
                booms = GameRandom.value < ctx.Ramp(0f, 0.6f, 0.9f, 1.6f) ? 2 : 1;
            int running = booms;
            for (int b = 0; b < booms; b++)
                ctx.Host.StartCoroutine(Done(DelayedBoom(ctx, GameRandom.Range(1f, duration - 1.5f), purpleShare), () => running--));

            float t = 0f;
            while (t < duration)
            {
                yield return Cluster(ctx, purpleShare);
                yield return new WaitForSeconds(interval);
                t += interval + 0.4f;
            }
            // атака заканчивается вместе с бум-кругами — иначе они взрываются уже посреди следующей атаки
            while (running > 0) yield return null;
        }

        static IEnumerator Done(IEnumerator routine, System.Action onDone)
        {
            yield return routine;
            onDone();
        }

        static IEnumerator DelayedBoom(AttackContext ctx, float delay, float purpleShare)
        {
            yield return new WaitForSeconds(delay);
            var types = ctx.Types(ctx.RampInt(5, 8), 1, purpleShare);
            yield return BoomCirclePattern.Burst(ctx, BoomCirclePattern.RandomCenter(ctx), types, 2f, 2.6f);
        }

        // Кучка: 3–5 белых/фиолетовых с одного направления, а по её краям синие: кучку нужно разрубить,
        // не задев их. На низкой сложности кучка узкая, синяя одна и летит заметно в стороне;
        // со сложностью синих больше и они прижимаются к кучке.
        static IEnumerator Cluster(AttackContext ctx, float purpleShare)
        {
            int n = ctx.RampInt(3, 5, 0f, 1.4f);
            int blue = 1 + (GameRandom.value < ctx.Ramp(0f, 0.7f, 0.4f, 1.2f) ? 1 : 0) + (GameRandom.value < ctx.Ramp(0f, 0.4f, 0.9f, 1.6f) ? 1 : 0);
            float spread = ctx.Ramp(0.18f, 0.35f, 0f, 1.2f);                                   // разброс кучки, рад
            float blueMin = ctx.Ramp(0.75f, 0.42f, 0f, 1.4f), blueMax = blueMin + 0.15f;      // отступ синих от центра кучки
            var types = ctx.Types(n, 0, purpleShare);
            for (int i = 0; i < blue; i++) types.Add(ProjectileType.Blue);
            float baseAngle = GameRandom.Range(0f, Mathf.PI * 2f);
            int blueIndex = 0;
            foreach (var type in types)
            {
                float angle = baseAngle + GameRandom.Range(-spread, spread);
                if (type == ProjectileType.Blue)
                {
                    float side = (blueIndex++ % 2 == 0) ? 1f : -1f;
                    angle = baseAngle + side * GameRandom.Range(blueMin, blueMax);
                }
                Vector2 dir = ctx.Dir(angle);
                Vector2 start = ctx.PointOffScreen(dir);
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir);
                bool drunk = GameRandom.value < 0.5f;
                float speed = ctx.PickSpeed(drunk ? 1.6f : 2.2f, drunk ? 3f : 4.2f, distance * (drunk ? 1.15f : 1f));
                IProjectileMotion motion = drunk
                    ? new DrunkMotion(start, ctx.Target, speed, GameRandom.Range(1.5f, 3f) * (GameRandom.value < 0.5f ? 1 : -1), ctx.Ramp(0.5f, 0.8f))
                    : new StraightMotion(start, ctx.Target, speed);
                bool bone = type != ProjectileType.Blue && GameRandom.value < 0.2f;   // редкие кости среди черепов
                if (bone) ctx.Spawn(type, new FacingMotion(motion), "Bone");
                else ctx.Spawn(type, motion);
                yield return new WaitForSeconds(0.12f);
            }
        }
    }
}
