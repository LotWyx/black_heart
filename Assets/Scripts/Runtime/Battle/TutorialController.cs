using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Обучение при первом запуске. Луноликий по очереди показывает механики:
    // резать белые шары, дважды резать фиолетовые, не трогать синие, бить врага.
    // «Остановка времени» замораживает только пули — анимации и подсказки продолжают идти.
    public partial class TutorialController : MonoBehaviour
    {
        public static bool Completed
        {
            get => SaveSystem.Data.tutorialDone;
            set { SaveSystem.Data.tutorialDone = value; SaveSystem.Save(); }
        }

        EnemyDefinition boss => GameBalance.Current.tutorialBoss;
        [Tooltip("Скорость печати реплик, символов в секунду")]
        public float charsPerSecond = 30f;

        public bool IsRunning { get; private set; }

        static readonly Color Ink = new Color(0.07f, 0.02f, 0.12f);      // контуры и тени текста
        static readonly Color Panel = new Color(0.06f, 0.02f, 0.04f);    // заливка панели реплик
        static readonly Color Accent = PixelUI.Accent;   // красный, как рамка боя

        enum Hint { None, Bullet, Fragment, Blue, Enemy }

        GameManager gm => GameManager.I;
        Enemy moonface;
        AudioSource voice;

        // что сейчас разрешено резать и что уже произошло
        bool allowBullets, allowEnemy;
        bool bulletSliced, fragmentSliced, purpleSplit, enemyHit;

        // реплика Луноликого
        string speech;
        float speechTime;

        // подсказка
        Func<string> instruction;
        string instructionSub;
        Hint hint;
        float hintTime;
        SpriteRenderer hintIcon, hintCross;
        SliceVisual hintTrail;   // разрез-подсказка выглядит как настоящий (создаётся при первом показе)

        static bool TouchUI => Application.isMobilePlatform || Pointer.current is Touchscreen;
        static string CutText => TouchUI ? "ПРОВЕДИТЕ ПАЛЬЦЕМ, ЧТОБЫ ПОРЕЗАТЬ" : "ЗАЖМИТЕ ЛКМ И ПРОВЕДИТЕ, ЧТОБЫ ПОРЕЗАТЬ";

        Vector2 Heart => gm.Box.Current.center;

        // ---------- жизненный цикл ----------

        void Awake()
        {
            voice = GameAudio.AddSource(gameObject, false);
            hintIcon = new GameObject("TutorialHint").AddComponent<SpriteRenderer>();
            hintIcon.sortingOrder = 30;
            hintCross = new GameObject("TutorialCross").AddComponent<SpriteRenderer>();
            hintCross.sprite = ArtLibrary.Get(ArtIds.HintCross);
            hintCross.sortingOrder = 31;
            ShowHintObjects(false);
        }

        void OnEnable()
        {
            GameEvents.OnProjectileSliced += OnSliced;
            GameEvents.OnProjectileSplit += OnSplit;
        }

        void OnDisable()
        {
            GameEvents.OnProjectileSliced -= OnSliced;
            GameEvents.OnProjectileSplit -= OnSplit;
        }

        void OnSliced(Projectile p)
        {
            if (!IsRunning) return;
            bulletSliced = true;
            if (p.IsFragment) fragmentSliced = true;
        }

        void OnSplit(Projectile p)
        {
            if (IsRunning) purpleSplit = true;
        }

        public void Begin()
        {
            StopAllCoroutines();
            IsRunning = true;
            StartCoroutine(Run());
        }

        public void Abort()
        {
            if (!IsRunning) return;
            StopAllCoroutines();
            Cleanup();
        }

        void Cleanup()
        {
            IsRunning = false;
            allowBullets = allowEnemy = false;
            speech = null;
            ClearInstruction();
            gm.Projectiles.Frozen = false;
            gm.Box.TimeStopped = false;
            gm.Player.CannotDie = false;
        }

        // Разрезы во время обучения проходят только туда, куда сейчас можно
        public void HandleSlice(Vector2 a, Vector2 b, Vector2 dir)
        {
            if (allowBullets) gm.Projectiles.Slice(a, b, dir);
            if (allowEnemy && !enemyHit && moonface != null &&
                Geometry.DistancePointSegment(moonface.Position, a, b) <= moonface.HitRadius)
            {
                enemyHit = true;
                moonface.TakeDamage(1);
            }
        }

        // ---------- сценарий ----------

        IEnumerator Run()
        {
            gm.Player.CannotDie = true;
            allowBullets = allowEnemy = false;
            while (gm.Box.IsAnimating) yield return null;
            yield return new WaitForSeconds(0.3f);

            // 1. Окно боя, атаковать нельзя. Появляется Луноликий и смеётся
            moonface = gm.Battle.PlaceEnemy(boss);
            yield return moonface.Appear();
            StartCoroutine(moonface.Laugh());
            yield return new WaitForSeconds(0.4f);
            yield return Say("НЕУЖЕЛИ ТЫ РЕШИЛ БРОСИТЬ МНЕ, ЛУНОЛИКОМУ, ВЫЗОВ?! УМРИ!");

            // 2. Защита: белые шары. Время стоит, пока игрок не порежет первый шар
            yield return ToDefense();
            SpawnVolley(ProjectileType.White, new[] { 200f, -20f, 250f }, 5f);   // сбоку и снизу: верх занят подсказкой
            yield return new WaitForSeconds(1.2f);
            allowBullets = true;
            bulletSliced = false;
            Freeze(true);
            Instruct(() => CutText);
            SetHint(Hint.Bullet);
            while (!bulletSliced) yield return null;
            ClearInstruction();
            Freeze(false);
            yield return WaitForClearField();

            // 3. Фиолетовый: после первого удара время снова стоит — нужно добить осколок
            yield return new WaitForSeconds(0.4f);
            purpleSplit = false;
            var purple = new ProjectileHandle(SpawnVolley(ProjectileType.Purple, new[] { 15f }, 4.5f)[0]);
            while (!purpleSplit)
            {
                // игрок медлит — останавливаем время у сердца и напоминаем, как резать
                if (!gm.Projectiles.Frozen && purple.Alive && Vector2.Distance(purple.Position, Heart) < 2.6f)
                {
                    Freeze(true);
                    Instruct(() => CutText);
                    SetHint(Hint.Bullet);
                }
                yield return null;
            }
            Freeze(true);
            fragmentSliced = false;
            Instruct(() => "ФИОЛЕТОВЫЕ АТАКИ НУЖНО УНИЧТОЖАТЬ ДВАЖДЫ");
            SetHint(Hint.Fragment);
            while (!fragmentSliced) yield return null;
            ClearInstruction();
            Freeze(false);
            yield return WaitForClearField();
            allowBullets = false;

            // 4. Режим атаки без атаки: Луноликий злится
            yield return ToAttack();
            StartCoroutine(moonface.Angry(1.4f));
            yield return Say("А КАК ТЕБЕ ТАКОЕ?!");

            // 5. Синие круги со всех сторон: их резать нельзя
            yield return ToDefense();
            var angles = new float[8];
            for (int i = 0; i < angles.Length; i++) angles[i] = 22.5f + i * 45f;
            SpawnVolley(ProjectileType.Blue, angles, 4.5f);
            yield return new WaitForSeconds(1.1f);
            Freeze(true);
            Instruct(() => "СИНИЕ АТАКИ НЕЛЬЗЯ РЕЗАТЬ.", "НАЖМИТЕ, ЧТОБЫ ПРОДОЛЖИТЬ");
            SetHint(Hint.Blue);
            yield return WaitForTap();
            ClearInstruction();
            Freeze(false);
            allowBullets = true;   // если всё же порежет синюю — получит урон и штраф, это тоже урок
            yield return WaitForClearField();
            allowBullets = false;

            // 6. Атака: время стоит, таймера нет — нужно провести по врагу
            yield return ToAttack();
            Freeze(true);
            enemyHit = false;
            allowEnemy = true;
            Instruct(() => "ПРОВЕДИТЕ ПО ВРАГУ, ЧТОБЫ АТАКОВАТЬ");
            SetHint(Hint.Enemy);
            while (!enemyHit) yield return null;
            allowEnemy = false;
            ClearInstruction();
            Freeze(false);
            yield return new WaitForSeconds(0.8f);

            // 7. Луноликий снова улыбается и уходит — начинается основная игра
            StartCoroutine(moonface.Laugh());
            yield return Say("ТАК СЛАБО! ТЫ НЕ ДОСТОИН МОЕГО ВНИМАНИЯ. УДАЧИ ПРОТИВ МОИХ СЛУГ!");
            yield return moonface.Vanish();
            gm.Battle.DespawnEnemy();
            moonface = null;
            yield return new WaitForSeconds(0.6f);

            Completed = true;
            Cleanup();
            gm.BeginMainBattle();
        }

        IEnumerator ToDefense()
        {
            gm.Battle.ForcePhase(BattlePhase.ToDefense);
            yield return moonface.AttackWindup();
            gm.Box.Expand();
            yield return moonface.FlyUp(gm.Pixel.ViewRect.yMax + 3f);
            while (gm.Box.IsAnimating) yield return null;
            gm.Battle.ForcePhase(BattlePhase.Defense);
        }

        IEnumerator ToAttack()
        {
            gm.Battle.ForcePhase(BattlePhase.ToAttack);
            gm.Box.Shrink();
            yield return moonface.Return();
            while (gm.Box.IsAnimating) yield return null;
            gm.Battle.ForcePhase(BattlePhase.Attack);
        }

        // Пули из-за рамки по заданным углам; все долетают до сердца за arriveTime секунд
        List<Projectile> SpawnVolley(ProjectileType type, float[] anglesDeg, float arriveTime)
        {
            var list = new List<Projectile>();
            Vector2 heart = Heart;
            foreach (float angle in anglesDeg)
            {
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 start = heart + dir * (gm.Box.DistanceToEdge(heart, dir) + 0.6f);
                float speed = Vector2.Distance(start, heart) / arriveTime;
                list.Add(gm.Projectiles.Spawn(type, new StraightMotion(start, heart, speed)));
            }
            return list;
        }

        IEnumerator WaitForClearField()
        {
            float timeout = 20f;
            while (gm.Projectiles.Count > 0 && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            gm.Projectiles.ClearAll();
        }

        IEnumerator WaitForTap()
        {
            yield return null;
            while (!Tapped()) yield return null;
        }

        bool Tapped()
        {
            if (gm.Paused) return false;
            var kb = Keyboard.current;
            return gm.Slicer.PressedThisFrame ||
                   (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
        }

        void Freeze(bool on)
        {
            if (gm.Projectiles.Frozen == on) return;
            gm.Projectiles.Frozen = on;
            gm.Box.TimeStopped = on;
            voice.pitch = 1f;
            voice.PlayOneShot(on ? SfxSynth.TimeStop : SfxSynth.TimeResume);
        }

        // Реплика: текст печатается с «голосом»; тап дописывает текст сразу, второй тап — дальше
        IEnumerator Say(string text)
        {
            speech = text;
            speechTime = 0f;
            int shown = 0;
            float typeTime = text.Length / charsPerSecond;
            yield return null;
            while (speechTime < typeTime)
            {
                if (Tapped()) speechTime = typeTime;
                else speechTime += Time.deltaTime;

                int visible = Mathf.Min(text.Length, Mathf.FloorToInt(speechTime * charsPerSecond));
                for (int i = shown; i < visible; i++)
                {
                    if (text[i] == ' ' || i % 2 != 0) continue;
                    voice.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                    voice.PlayOneShot(SfxSynth.Voice);
                    break;
                }
                shown = visible;
                yield return null;
            }
            speechTime = typeTime;
            float wait = Mathf.Max(1.6f, text.Length * 0.045f);
            yield return null;
            for (float t = 0; t < wait && !Tapped(); t += Time.deltaTime) yield return null;
            speech = null;
        }

        void Instruct(Func<string> text, string sub = null)
        {
            instruction = text;
            instructionSub = sub;
        }

        void ClearInstruction()
        {
            instruction = null;
            instructionSub = null;
            SetHint(Hint.None);
        }

        // ---------- анимация подсказки ----------

        void SetHint(Hint value)
        {
            hint = value;
            hintTime = 0f;
            if (value == Hint.None) ShowHintObjects(false);
        }

        void ShowHintObjects(bool visible)
        {
            hintIcon.enabled = visible;
            hintCross.enabled = visible;
            if (!visible && hintTrail != null) hintTrail.Hide();
        }

        // Рука (или мышь) проводит по цели: появляется → ведёт разрез → отпускает → пауза
        void Update()
        {
            if (!IsRunning || hint == Hint.None) return;
            if (!TryGetHintPath(out Vector2 target, out Vector2 a, out Vector2 b))
            {
                ShowHintObjects(false);
                return;
            }

            hintTime += Time.deltaTime;
            const float cycle = 1.6f;
            float u = (hintTime % cycle) / cycle;
            Vector2 pos = a, trailEnd = a;
            bool pressed = false, iconVisible = true, trail = true;
            if (u < 0.15f) trail = false;
            else if (u < 0.6f) { pos = trailEnd = Vector2.Lerp(a, b, (u - 0.15f) / 0.45f); pressed = true; }
            else if (u < 0.85f) pos = trailEnd = b;
            else { iconVisible = false; trail = false; }

            // Цель ниже сердца — иконка встаёт над линией разреза (палец смотрит вниз), чтобы не залезать на HUD
            bool touch = TouchUI;
            bool below = hint != Hint.Enemy && target.y < Heart.y - 0.5f;
            var frames = ArtLibrary.Frames(touch ? ArtIds.HintFinger : ArtIds.HintMouse);
            Vector2 offset = (touch ? new Vector2(1.5f, below ? 6.5f : -6.5f) : new Vector2(9f, below ? 9f : -9f)) / ArtLibrary.PPU;
            hintIcon.flipY = touch && below;
            hintIcon.enabled = iconVisible;
            hintIcon.sprite = frames[pressed && frames.Length > 1 ? 1 : 0];
            hintIcon.transform.position = pos + offset;

            // Разрез как у игрока: короткий светящийся хвост за кончиком; после «отпускания» хвост
            // за долю секунды втягивается в точку, как настоящий след
            if (hintTrail == null) hintTrail = new SliceVisual("TutorialSlice", 3f, 8f, 28);
            float len = 0f;
            if (trail)
            {
                const float tail = 1.8f, vanish = 0.12f;
                float since = (u - 0.6f) * cycle;   // сколько прошло после «отпускания»
                len = pressed ? tail : tail * Mathf.Clamp01(1f - since / vanish);
                len = Mathf.Min(len, Vector2.Distance(a, trailEnd));
            }
            if (len <= 0.01f) hintTrail.Hide();
            else
            {
                Vector2 start = trailEnd - (b - a).normalized * len;
                var pts = new Vector3[6];
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 p = Vector2.Lerp(start, trailEnd, i / (pts.Length - 1f));
                    pts[i] = new Vector3(p.x, p.y, -1f);
                }
                hintTrail.Show(pts, pressed);
            }

            hintCross.enabled = hint == Hint.Blue && u > 0.35f;
            hintCross.transform.position = target;
        }

        bool TryGetHintPath(out Vector2 target, out Vector2 a, out Vector2 b)
        {
            target = a = b = Vector2.zero;
            if (hint == Hint.Enemy)
            {
                if (moonface == null) return false;
                target = moonface.Position;
                a = target + new Vector2(-3.3f, 0.8f);
                b = target + new Vector2(3.3f, -0.6f);
                return true;
            }

            // Цель — ближайшая к сердцу подходящая пуля; для синих — самая боковая
            // (сверху подсказка, снизу HUD)
            Projectile best = null;
            float bestScore = float.MaxValue;
            Vector2 heart = Heart;
            foreach (var p in gm.Projectiles.Active)
            {
                if (p == null) continue;
                bool match = hint == Hint.Fragment ? p.IsFragment
                           : hint == Hint.Blue ? p.Type == ProjectileType.Blue
                           : !p.IsFragment && p.Type != ProjectileType.Blue;
                if (!match) continue;
                float score = hint == Hint.Blue
                    ? -Mathf.Abs(p.Position.x - heart.x) + 0.01f * p.Position.y
                    : Vector2.Distance(p.Position, heart);
                if (score < bestScore) { bestScore = score; best = p; }
            }
            if (best == null) return false;

            // Разрез поперёк пути пули
            target = best.Position;
            Vector2 dir = (heart - target).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            a = target - perp * 1.4f;
            b = target + perp * 1.4f;
            return true;
        }
    }
}
