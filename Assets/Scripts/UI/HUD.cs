using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Интерфейс боя на IMGUI с пиксельным шрифтом. Получает данные через GameEvents.
    public partial class HUD : MonoBehaviour
    {
        // Основной акцент — красный, как внутренняя линия рамки боя
        static readonly Color Accent = PixelUI.Accent;
        static readonly Color Red = new Color(1f, 0.15f, 0.15f);
        static readonly Color Green = new Color(0.2f, 1f, 0.3f);

        // Цвета полос здоровья: основа, блик сверху, тень снизу, пустая часть, «след» урона
        struct BarStyle { public Color fill, light, shade, empty, trail; }
        static readonly BarStyle RedBar = new BarStyle
        {
            fill = new Color(0.9f, 0.1f, 0.14f), light = new Color(1f, 0.45f, 0.4f), shade = new Color(0.55f, 0.03f, 0.08f),
            empty = new Color(0.16f, 0.02f, 0.05f), trail = new Color(1f, 0.85f, 0.75f)
        };
        static readonly BarStyle GreenBar = new BarStyle
        {
            fill = new Color(0.2f, 0.85f, 0.3f), light = new Color(0.6f, 1f, 0.55f), shade = new Color(0.08f, 0.45f, 0.18f),
            empty = new Color(0.05f, 0.14f, 0.08f), trail = new Color(1f, 0.95f, 0.7f)
        };
        const float TrailDelay = 0.35f, TrailSpeed = 0.9f;   // «след» стоит на месте, затем стекает (доля полосы в секунду)

        class Popup
        {
            public Vector2 world;
            public string text;
            public Color color;
            public float t, life, scale = 1f;
            public bool rise = true;
            public float idleSince;   // когда надпись погасла и ушла в запас
        }

        // Надписи показываются строго по событию. Погасшая надпись не выбрасывается, а PopupKeepTime секунд ждёт
        // в запасе: следующее такое же «+10» берёт готовый объект. Новый создаётся, только если одинаковых
        // надписей одновременно нужно больше, чем есть в запасе, или запасные уже устарели.
        const float PopupKeepTime = 6f;
        readonly List<Popup> popups = new List<Popup>();
        readonly List<Popup> idlePopups = new List<Popup>();

        // Готовые строки для чисел: «+10» и «-25» не собираются заново при каждом событии
        static readonly Dictionary<int, string> signedTexts = new Dictionary<int, string>();
        static readonly Dictionary<int, string> healTexts = new Dictionary<int, string>();
        string scoreLabel, hpLabel, typedSource, typedText = "";
        int scoreLabelValue = int.MinValue, hpLabelHp = int.MinValue, hpLabelMax = int.MinValue, typedLength = -1;
        string message;
        float messageTime;
        float gameOverTime;
        bool newRecord;
        float playerTrail = 1f, enemyTrail = 1f, playerTrailWait, enemyTrailWait;
        Enemy trailEnemy;

        GameManager gm => GameManager.I;



        void OnEnable()
        {
            GameEvents.OnGameStarted += OnGameStarted;
            GameEvents.OnGameOver += OnGameOver;
            GameEvents.OnScoreChanged += OnScoreChanged;
            GameEvents.OnCleanDefense += OnCleanDefense;
            GameEvents.OnPlayerHealed += OnPlayerHealed;
            GameEvents.OnEnemySpawned += OnEnemySpawned;
            GameEvents.OnEnemyTakeDamage += OnEnemyTakeDamage;
            GameEvents.OnEnemyLaughed += OnEnemyLaughed;
            GameEvents.OnEnemyDefeated += OnEnemyDefeated;
            GameEvents.OnPhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            GameEvents.OnGameStarted -= OnGameStarted;
            GameEvents.OnGameOver -= OnGameOver;
            GameEvents.OnScoreChanged -= OnScoreChanged;
            GameEvents.OnCleanDefense -= OnCleanDefense;
            GameEvents.OnPlayerHealed -= OnPlayerHealed;
            GameEvents.OnEnemySpawned -= OnEnemySpawned;
            GameEvents.OnEnemyTakeDamage -= OnEnemyTakeDamage;
            GameEvents.OnEnemyLaughed -= OnEnemyLaughed;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnPhaseChanged -= OnPhaseChanged;
        }

        // ---------- события ----------

        void OnGameStarted()
        {
            idlePopups.AddRange(popups);
            popups.Clear();
            message = null;
        }

        void OnGameOver(int score)
        {
            gameOverTime = 0;
            newRecord = score > 0 && score >= gm.Score.Best;
        }

        void OnScoreChanged(int delta, int total, Vector2 pos)
        {
            if (delta == 0) return;
            Color c = delta < 0 ? Red : delta >= 50 ? Accent : Color.white;
            AddPopup(pos, Signed(delta), c, 0.7f, delta >= 50 ? 2f : 1f);
        }

        // «ЧИСТО!» строкой выше очков за защиту без урона (рядом с сердцем); вид тот же, что у «+200» за победу
        void OnCleanDefense(Vector2 pos) =>
            AddPopup(pos + Vector2.up * (10f / ArtLibrary.PPU), "ЧИСТО!", Accent, 0.7f, 2f);

        void OnPlayerHealed(int amount, int hp) =>
            AddPopup(gm.Player.Position + Vector2.up * 0.9f, HealText(amount), Green, 1f);

        void OnEnemySpawned(Enemy e) => ShowMessage($"* {e.Definition.displayName} НАПАДАЕТ!");

        void OnEnemyTakeDamage(Enemy e, int amount) =>
            AddPopup(e.Position + new Vector2(e.HitRadius * 0.7f, e.HitRadius * 0.4f), Signed(-amount), Red, 0.9f, 2f);

        void OnEnemyLaughed(Enemy e)
        {
            var p = AddPopup(e.Position + new Vector2(e.HitRadius + 1.8f, e.HitRadius * 0.4f), e.Definition.laughText, Color.white, 1.2f);
            p.rise = false;
        }

        void OnEnemyDefeated(Enemy e) => ShowMessage($"* {e.Definition.displayName} ПОВЕРЖЕН!");

        void OnPhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.ToDefense) message = null;
        }

        Popup AddPopup(Vector2 world, string text, Color color, float life, float scale = 1f)
        {
            Popup p = null;
            for (int i = idlePopups.Count - 1; i >= 0; i--)
            {
                if (idlePopups[i].text != text) continue;
                p = idlePopups[i];
                idlePopups.RemoveAt(i);
                break;
            }
            if (p == null) p = new Popup { text = text };
            p.world = world;
            p.color = color;
            p.life = life;
            p.scale = scale;
            p.t = 0f;
            p.rise = true;
            popups.Add(p);
            return p;
        }

        static string Signed(int value)
        {
            if (!signedTexts.TryGetValue(value, out var text))
                signedTexts[value] = text = value > 0 ? "+" + value : value.ToString();
            return text;
        }

        static string HealText(int amount)
        {
            if (!healTexts.TryGetValue(amount, out var text)) healTexts[amount] = text = "+" + amount + " HP";
            return text;
        }

        // Строки HUD пересобираются только когда меняется число, а не каждый кадр
        string ScoreLabel(int score)
        {
            if (score != scoreLabelValue) { scoreLabelValue = score; scoreLabel = "ОЧКИ " + score; }
            return scoreLabel;
        }

        string HpLabel(int hp, int max)
        {
            if (hp != hpLabelHp || max != hpLabelMax) { hpLabelHp = hp; hpLabelMax = max; hpLabel = hp + "/" + max; }
            return hpLabel;
        }

        // Печатающееся сообщение: новая подстрока нужна только когда появилась следующая буква
        string Typed(string text, float time)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(time * 30f), 0, text.Length);
            if (!ReferenceEquals(text, typedSource) || n != typedLength)
            {
                typedSource = text;
                typedLength = n;
                typedText = n == text.Length ? text : text.Substring(0, n);
            }
            return typedText;
        }

        void ShowMessage(string text)
        {
            message = text;
            messageTime = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            messageTime += dt;
            gameOverTime += dt;
            UpdateTrails(dt);
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.t += dt;
                if (p.rise) p.world += Vector2.up * dt * 0.8f;
                if (p.t <= p.life) continue;
                popups.RemoveAt(i);
                p.idleSince = Time.unscaledTime;
                idlePopups.Add(p);
            }
            for (int i = idlePopups.Count - 1; i >= 0; i--)
                if (Time.unscaledTime - idlePopups[i].idleSince > PopupKeepTime) idlePopups.RemoveAt(i);
        }

        // «След» урона: после удара показывает потерянную часть полосы и с задержкой стекает к текущему значению
        void UpdateTrails(float dt)
        {
            if (gm == null || gm.Player == null) return;
            Step(ref playerTrail, ref playerTrailWait, (float)gm.Player.Hp / gm.Player.MaxHp, dt);
            var e = gm.Battle != null ? gm.Battle.CurrentEnemy : null;
            if (e == null) return;
            if (e != trailEnemy) { trailEnemy = e; enemyTrail = 1f; enemyTrailWait = 0; }
            Step(ref enemyTrail, ref enemyTrailWait, (float)e.Hp / e.MaxHp, dt);
        }

        static void Step(ref float trail, ref float wait, float value, float dt)
        {
            if (trail <= value) { trail = value; wait = TrailDelay; return; }   // лечение — сразу
            if (wait > 0) { wait -= dt; return; }
            trail = Mathf.Max(value, trail - TrailSpeed * dt);
        }
    }
}
