using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // ================= НЕНАСЫТНЫЙ (враг «Зверь») =================
    // Чёрный кот-демон, и все его атаки — охота: подкрадывается и прыгает, захлопывает пасть, полосует когтями,
    // играет в кошки-мышки и скалится чеширской улыбкой. Пули — кошачьи головы (синие — следы лап),
    // в «Пасти», «Когтях» и «Улыбке» — клыки (вид Fang): они поворачиваются остриём по ходу полёта.

    // Пасть (челюсти боссов Deltarune): вокруг сердца проступают две дуги клыков — верхняя и нижняя челюсть.
    // Пасть раскрывается (клыки отходят от сердца), дрожит и захлопывается: клыки разом летят в сердце,
    // от уголков к середине. Синие клыки — с просветами рядом. Укуса два (на высокой сложности — три),
    // каждый следующий повёрнут на 90°: челюсти то сверху и снизу, то слева и справа.
    public class BeastJawsPattern : AttackPattern
    {
        public const string ID = "beast_jaws";
        public override string Id => ID;
        const string Fang = "Fang";

        public override IEnumerator Run(AttackContext ctx)
        {
            int bites = ctx.Difficulty >= 1.2f ? 3 : 2;
            bool vertical = GameRandom.value < 0.5f;
            for (int b = 0; b < bites; b++, vertical = !vertical)
            {
                var fangs = Bite(ctx, vertical ? Vector2.up : Vector2.right, out float launchAt);
                float timeout = launchAt + 5f;
                while (AttackContext.AliveCount(fangs) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                if (b < bites - 1) yield return new WaitForSeconds(0.35f);
            }
        }

        static List<ProjectileHandle> Bite(AttackContext ctx, Vector2 axis, out float launchAt)
        {
            Rect r = ctx.Box.Current;
            float half = Mathf.Abs(axis.y) > 0.5f ? r.height / 2f : r.width / 2f;
            float radius = Mathf.Min(3f, half - 1.4f);           // где проступает челюсть
            const float Open = 0.7f, OpenTime = 0.45f;            // насколько и как быстро раскрывается пасть
            int perJaw = ctx.RampInt(7, 10, 0f, 1.4f);
            float spread = 58f * Mathf.Deg2Rad;
            launchAt = ctx.Ramp(1.35f, 0.85f, 0f, 1.5f);
            float speed = ctx.Ramp(4.5f, 6.5f, 0f, 1.5f);
            float purpleShare = ctx.Ramp(0.15f, 0.4f, 0f, 1.2f);
            int blue = ctx.RampInt(1, 2, 0f, 1.4f);   // мало: каждый синий забирает ещё и соседей-просветы

            var fangs = new List<ProjectileHandle>();
            foreach (float side in new[] { 1f, -1f })            // верхняя и нижняя челюсть
            {
                var types = ctx.Types(perJaw, blue, purpleShare);
                var skip = AttackContext.BlueGaps(types, false);
                for (int i = 0; i < perJaw; i++)
                {
                    if (skip[i]) continue;
                    float k = perJaw == 1 ? 0.5f : (float)i / (perJaw - 1);
                    float angle = Mathf.Lerp(-spread, spread, k);
                    Vector2 dir = MotionMath.Rotate(axis * side, angle);
                    Vector2 pos = ctx.Target + dir * radius;
                    // захлопывается волной от уголков к середине за 0,35 с: клыков много, но к сердцу они
                    // подходят по очереди и их можно резать один за другим
                    float order = Mathf.Abs(k - 0.5f) * 2f;
                    var motion = new FacingPounce(new PounceMotion(pos, pos + dir * Open, OpenTime, launchAt + (1f - order) * 0.35f, ctx.Target, speed));
                    fangs.Add(new ProjectileHandle(ctx.SpawnFormation(types[i], motion, Fang)));
                }
            }
            return fangs;
        }
    }

    // Когти (полосы костей Санса, копья Андайн): четыре (на высокой сложности пять) параллельные полосы клыков одна за другой проносятся
    // через рамку наискосок, будто зверь полоснул лапой. Каждая полоса проходит через сердце — разрубить
    // нужно клыки на его пути (полоса плотная — часто их два), остальные пролетят мимо. Иногда на пути
    // сердца синий: его пропусти. Лапа бьёт дважды (на высокой сложности — трижды), каждый раз под другим углом.
    public class BeastClawsPattern : AttackPattern
    {
        public const string ID = "beast_claws";
        public override string Id => ID;
        const string Fang = "Fang";
        const float Spacing = 0.58f, Gap = 1.2f;   // шаг меньше радиуса попадания: на пути сердца часто два клыка

        public override IEnumerator Run(AttackContext ctx)
        {
            int swipes = ctx.Difficulty >= 1.2f ? 3 : 2;
            float angle = (45f + 90f * GameRandom.Range(0, 4) + GameRandom.Range(-15f, 15f)) * Mathf.Deg2Rad;
            for (int s = 0; s < swipes; s++)
            {
                var fangs = Swipe(ctx, ctx.Dir(angle), out float duration);
                float timeout = duration + 2f;
                while (AttackContext.AliveCount(fangs) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                angle += (GameRandom.value < 0.5f ? 1f : -1f) * GameRandom.Range(70f, 110f) * Mathf.Deg2Rad;
            }
        }

        static List<ProjectileHandle> Swipe(AttackContext ctx, Vector2 dir, out float duration)
        {
            Rect r = ctx.Box.Current;
            Vector2 across = new Vector2(-dir.y, dir.x);
            float diag = new Vector2(r.width, r.height).magnitude;
            int half = Mathf.CeilToInt(diag / 2f / Spacing);
            float speed = ctx.Ramp(4f, 6f, 0f, 1.5f);
            float reach = diag / 2f + 1f;                         // полосы стартуют за рамкой — там их не видно
            float purpleShare = ctx.Ramp(0.1f, 0.35f, 0f, 1.2f);
            float blueOnPath = ctx.Ramp(0.15f, 0.35f, 0f, 1.4f);   // шанс, что на пути сердца синий

            var fangs = new List<ProjectileHandle>();
            int lines = ctx.Difficulty >= 1f ? 5 : 4;
            for (int line = 0; line < lines; line++)
            {
                // полоса сдвинута поперёк на долю шага: на пути сердца всегда ровно один клык, но не всегда центральный
                float shift = GameRandom.Range(-0.35f, 0.35f) * Spacing;
                int count = half * 2 + 1;
                var types = ctx.Types(count, ctx.RampInt(1, 3, 0f, 1.4f), purpleShare);
                int onPath = half;
                types[onPath] = GameRandom.value < blueOnPath ? ProjectileType.Blue
                              : GameRandom.value < purpleShare ? ProjectileType.Purple : ProjectileType.White;
                var skip = AttackContext.BlueGaps(types, false);
                Vector2 start = ctx.Target - dir * (reach + line * Gap);
                for (int i = 0; i < count; i++)
                {
                    if (skip[i] && i != onPath) continue;
                    Vector2 p = start + across * ((i - half) * Spacing + shift);
                    var motion = new FacingMotion(new StraightMotion(p, p + dir * 100f, speed));
                    fangs.Add(new ProjectileHandle(ctx.SpawnFormation(types[i], motion, Fang)));
                }
            }
            duration = (reach * 2f + (lines - 1) * Gap) / speed;
            return fangs;
        }
    }

    // Кошки-мышки (скачущие объекты Deltarune): «мыши» бегают по краю рамки, иногда разворачиваются,
    // а потом по одной бросаются в сердце — поймай каждую. Пока бегают, не опасны и в очередь к сердцу не встают.
    public class BeastMicePattern : AttackPattern
    {
        public const string ID = "beast_mice";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            Rect r = ctx.Box.Current;
            int count = ctx.RampInt(6, 10, 0f, 1.5f);
            float runSpeed = ctx.Ramp(4f, 6.5f, 0f, 1.5f);
            float firstDash = ctx.Ramp(1.2f, 0.8f, 0f, 1.5f);
            float step = ctx.Ramp(0.6f, 0.3f, 0f, 1.5f);          // между бросками
            float dashSpeed = ctx.Ramp(5f, 7f, 0f, 1.5f);
            ctx.PlanTypes(count);
            float offset = GameRandom.value;
            var mice = new List<ProjectileHandle>();
            // порядок бросков перемешан: какая мышь прыгнет следующей, видно только по дрожи
            var order = new List<int>();
            for (int i = 0; i < count; i++) order.Insert(GameRandom.Range(0, order.Count + 1), i);
            for (int i = 0; i < count; i++)
            {
                float roam = firstDash + order[i] * step;
                var reversals = new List<float>();
                float tr = GameRandom.Range(0.4f, 1.2f);
                while (tr < roam - 0.2f) { reversals.Add(tr); tr += GameRandom.Range(0.6f, 1.4f); }
                var motion = new PerimeterRunMotion(r, 0.7f, offset + (float)i / count, GameRandom.value < 0.5f ? 1f : -1f,
                                                    runSpeed, reversals.ToArray(), roam, ctx.Target, dashSpeed);
                mice.Add(new ProjectileHandle(ctx.Spawn(ctx.NextType(), motion)));
                yield return new WaitForSeconds(0.08f);
            }
            float timeout = firstDash + count * step + 4f;
            while (AttackContext.AliveCount(mice) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
        }
    }

    // Чеширская улыбка (лица Омеги Флауи и Азриэля): вокруг сердца по одному проступают глаза и ухмылка
    // во всю рамку. Потом улыбка «смеётся»: зубы от уголков к середине по очереди срываются в сердце,
    // последними — глаза (пары фиолетовых). Синие зубы — с просветами. На высокой сложности следом
    // проступает вторая улыбка — перевёрнутая.
    public class BeastGrinPattern : AttackPattern
    {
        public const string ID = "beast_grin";
        public override string Id => ID;
        const string Fang = "Fang";

        public override IEnumerator Run(AttackContext ctx)
        {
            yield return Grin(ctx, false);
            if (ctx.Difficulty >= 1f) yield return Grin(ctx, true);   // на высокой сложности — вторая, перевёрнутая
        }

        static IEnumerator Grin(AttackContext ctx, bool flipped)
        {
            Rect r = ctx.Box.Current;
            float rx = Mathf.Min(5f, r.width / 2f - 1f), ry = Mathf.Min(3.2f, r.height / 2f - 0.9f) * (flipped ? -1f : 1f);
            int teeth = ctx.RampInt(12, 17, 0f, 1.5f);
            float hold = ctx.Ramp(0.9f, 0.55f, 0f, 1.5f);         // сколько улыбка висит целиком
            float step = ctx.Ramp(0.14f, 0.07f, 0f, 1.5f);        // между бросками зубов
            float speed = ctx.Ramp(4f, 6f, 0f, 1.5f);
            const float Appear = 0.07f;

            var types = ctx.Types(teeth, ctx.RampInt(1, 3, 0f, 1.4f), ctx.Ramp(0.1f, 0.35f, 0f, 1.2f));
            var skip = AttackContext.BlueGaps(types, false);
            var points = new List<(Vector2 pos, ProjectileType type, float order, string variant)>();
            for (int i = 0; i < teeth; i++)
            {
                if (skip[i]) continue;
                float k = (float)i / (teeth - 1);
                float a = Mathf.Lerp(200f, 340f, k) * Mathf.Deg2Rad;   // нижняя дуга — ухмылка
                points.Add((ctx.Target + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), types[i], Mathf.Abs(k - 0.5f) * 2f, Fang));
            }
            // глаза — над сердцем, бросаются последними
            foreach (float sx in new[] { -1f, 1f })
                points.Add((ctx.Target + new Vector2(sx * rx * 0.45f, ry * 0.75f), ProjectileType.Purple, -1f, null));

            // бросок: сначала уголки (order 1), к середине (0), потом глаза (-1)
            var launchOrder = new List<int>();
            for (int i = 0; i < points.Count; i++) launchOrder.Add(i);
            launchOrder.Sort((x, y) => points[y].order.CompareTo(points[x].order));
            var rank = new int[points.Count];
            for (int i = 0; i < launchOrder.Count; i++) rank[launchOrder[i]] = i;

            float appearTotal = points.Count * Appear;
            var grin = new List<ProjectileHandle>();
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                float spawnedAt = i * Appear;
                float launchAt = appearTotal - spawnedAt + hold + rank[i] * step;   // своё время пули идёт с её появления
                var pounce = new PounceMotion(p.pos, p.pos, 0f, launchAt, ctx.Target, speed);
                IProjectileMotion motion = p.variant == Fang ? new FacingPounce(pounce) : (IProjectileMotion)pounce;   // глаза не поворачиваются
                grin.Add(new ProjectileHandle(ctx.SpawnFormation(p.type, motion, p.variant)));
                yield return new WaitForSeconds(Appear);
            }
            float timeout = hold + points.Count * step + 5f;
            while (AttackContext.AliveCount(grin) > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
        }
    }

    // Прыжки — обычная атака Ненасытного: кошачьи головы подкрадываются из-за края экрана, замирают
    // в засаде на расстоянии прыжка, приседают (дрожат) и прыгают в сердце. Синие следы лап — рядом
    // с другими пулями. Иногда двое прыгают разом с противоположных сторон. С ростом сложности засады
    // чаще, прыжок раньше, а сдвоенных прыжков больше.
    public class BeastStandardPattern : AttackPattern
    {
        public const string ID = "beast_std";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            const float duration = 4f;
            float interval = ctx.Ramp(0.6f, 0.3f, 0f, 1.5f);
            float crouch = ctx.Ramp(0.7f, 0.4f, 0f, 1.5f);
            float dashSpeed = ctx.Ramp(5f, 7f, 0f, 1.5f);
            float pairChance = ctx.Ramp(0.2f, 0.6f, 0f, 1.5f);   // двое прыгают разом с противоположных сторон
            int count = Mathf.Max(1, Mathf.FloorToInt(duration / interval));
            ctx.PlanTypes(count);
            for (int i = 0; i < count; i++)
            {
                var type = ctx.NextType();
                Vector2 dir = ctx.DirectionFor(type);
                float launch = Ambush(ctx, type, dir, crouch, dashSpeed, -1f);
                // сдвоенный прыжок: второй — с противоположной стороны и в тот же миг
                if (i + 1 < count && GameRandom.value < pairChance)
                {
                    i++;
                    Ambush(ctx, ctx.NextType(), MotionMath.Rotate(-dir, GameRandom.Range(-0.4f, 0.4f)), crouch, dashSpeed, launch);
                }
                yield return new WaitForSeconds(interval);
            }
        }

        // Засада: подкрасться, присесть, прыгнуть. launchAt < 0 — сразу после приседания; иначе — к этому моменту
        static float Ambush(AttackContext ctx, ProjectileType type, Vector2 dir, float crouch, float dashSpeed, float launchAt)
        {
            Vector2 start = ctx.PointOffScreen(dir);
            float perch = Mathf.Min(ctx.Ramp(3.4f, 2.8f), ctx.Box.DistanceToEdge(ctx.Target, dir) - 0.6f);
            Vector2 stand = ctx.Target + dir * perch;
            float distance = Vector2.Distance(start, stand);
            float approach = distance / ctx.PickSpeed(3.5f, 7f, distance);
            float launch = launchAt < 0f ? approach + crouch : Mathf.Max(launchAt, approach + 0.2f);
            ctx.Spawn(type, new PounceMotion(start, stand, approach, launch, ctx.Target, dashSpeed));
            return launch;
        }
    }
}
