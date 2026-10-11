using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Интерфейс боя на IMGUI с пиксельным шрифтом. Получает данные через GameEvents.
    public class HUD : MonoBehaviour
    {
        // Основной акцент — нежный пурпурно-красный, полосы урона — его потемневший вариант
        static readonly Color Accent = new Color(0.86f, 0.25f, 0.42f);
        static readonly Color AccentDark = new Color(0.36f, 0.07f, 0.17f);
        static readonly Color Red = new Color(1f, 0.15f, 0.15f);
        static readonly Color Green = new Color(0.2f, 1f, 0.3f);

        class Popup
        {
            public Vector2 world;
            public string text;
            public Color color;
            public float t, life, scale = 1f;
            public bool rise = true;
        }

        readonly List<Popup> popups = new List<Popup>();
        string message;
        float messageTime;
        float gameOverTime;
        bool newRecord;

        GameManager gm => GameManager.I;

        void OnEnable()
        {
            GameEvents.OnGameStarted += OnGameStarted;
            GameEvents.OnGameOver += OnGameOver;
            GameEvents.OnScoreChanged += OnScoreChanged;
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
            AddPopup(pos, delta > 0 ? "+" + delta : delta.ToString(), c, 0.7f, delta >= 50 ? 2f : 1f);
        }

        void OnPlayerHealed(int amount, int hp) =>
            AddPopup(gm.Player.Position + Vector2.up * 0.9f, $"+{amount} HP", Green, 1f);

        void OnEnemySpawned(Enemy e) => ShowMessage($"* {e.Definition.displayName} НАПАДАЕТ!");

        void OnEnemyTakeDamage(Enemy e, int amount) =>
            AddPopup(e.Position + new Vector2(e.HitRadius * 0.7f, e.HitRadius * 0.4f), "-" + amount, Red, 0.9f, 2f);

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
            var p = new Popup { world = world, text = text, color = color, life = life, scale = scale };
            popups.Add(p);
            return p;
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
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.t += dt;
                if (p.rise) p.world += Vector2.up * dt * 0.8f;
                if (p.t > p.life) popups.RemoveAt(i);
            }
        }

        // ---------- отрисовка ----------

        void OnGUI()
        {
            if (gm == null) return;
            GUI.depth = 0;
            float s = gm.Pixel.UiScale;

            if (gm.CurrentState == GameManager.State.GameOver)
            {
                DrawGameOver(s);
                return;
            }
            if (gm.CurrentState == GameManager.State.Dying || gm.CurrentState == GameManager.State.Menu) return;

            PixelFont.Draw($"ОЧКИ {gm.Score.Score}", 6 * s, 6 * s, s, Color.white);
            DrawEnemyInfo(s);

            Rect box = gm.Box.HudRect;
            Vector2 bottom = gm.Pixel.WorldToGui(new Vector2(box.center.x, box.yMin));
            float cx = bottom.x;
            float y = bottom.y + 6 * s;
            DrawPlayerHp(cx, y, s);

            float y2 = y + 12 * s;
            var phase = gm.Battle.Phase;
            bool tutorial = gm.Tutorial != null && gm.Tutorial.IsRunning;   // у обучения свои подсказки
            if (!tutorial && phase == BattlePhase.Attack)
            {
                if (Mathf.Repeat(Time.time, 0.5f) < 0.35f)
                    PixelFont.Draw("РУБИ ВРАГА!", cx, y2, s, Accent, 0);
                float w = 60 * s * Mathf.Clamp01(gm.Battle.AttackTimeLeft / gm.Battle.attackWindow);
                Bar(new Rect(cx - w / 2, y2 + 10 * s, w, 2 * s), Color.white);
            }
            else if (!tutorial && !string.IsNullOrEmpty(message))
            {
                PixelFont.Draw(Typed(message, messageTime), cx, y2, s, Color.white, 0);
            }

            foreach (var p in popups)
            {
                Vector2 g = gm.Pixel.WorldToGui(p.world);
                var c = p.color;   // без затухания: надпись просто исчезает в конце жизни
                PixelFont.Draw(p.text, g.x, g.y - 3.5f * s * p.scale, s * p.scale, c, 0);
            }
        }

        void DrawPlayerHp(float cx, float y, float s)
        {
            var player = gm.Player;
            string hp = $"{player.Hp}/{player.MaxHp}";
            float barW = player.MaxHp * 4 * s;
            float total = PixelFont.Width("HP", s) + 6 * s + barW + 6 * s + PixelFont.Width(hp, s);
            float x = cx - total / 2;

            PixelFont.Draw("HP", x, y, s, Color.white);
            x += PixelFont.Width("HP", s) + 6 * s;
            Bar(new Rect(x, y, barW, 7 * s), AccentDark);
            Bar(new Rect(x, y, barW * player.Hp / player.MaxHp, 7 * s), Accent);
            x += barW + 6 * s;
            PixelFont.Draw(hp, x, y, s, Color.white);
        }

        void DrawEnemyInfo(float s)
        {
            var e = gm.Battle.CurrentEnemy;
            if (e == null) return;
            float right = Screen.width - (8 + PauseMenu.IconSize + 6) * s;   // правый угол занят иконкой паузы
            float barW = Mathf.Min(e.MaxHp * 10, 60) * s;
            Bar(new Rect(right - barW, 6 * s, barW, 7 * s), AccentDark);
            Bar(new Rect(right - barW, 6 * s, barW * e.Hp / e.MaxHp, 7 * s), Green);
            PixelFont.Draw(e.Definition.displayName, right - barW - 6 * s, 6 * s, s, Color.white, 1);
        }

        void DrawGameOver(float s)
        {
            float cx = Screen.width / 2f;
            float top = Screen.height * 0.15f;
            var fadeIn = new Color(1, 1, 1, Mathf.Clamp01(gameOverTime / 1.2f));
            PixelFont.Draw("GAME", cx, top, 5 * s, fadeIn, 0);
            PixelFont.Draw("OVER", cx, top + 40 * s, 5 * s, fadeIn, 0);

            float y = top + 90 * s;
            PixelFont.Draw(Typed("* НЕ СДАВАЙСЯ...", gameOverTime - 1.2f), cx - 70 * s, y, s, Color.white);
            PixelFont.Draw(Typed("* СОХРАНЯЙ РЕШИМОСТЬ!", gameOverTime - 2f), cx - 70 * s, y + 12 * s, s, Color.white);
            if (gameOverTime > 3f)
            {
                string rec = newRecord ? "НОВЫЙ РЕКОРД!" : $"РЕКОРД: {gm.Score.Best}";
                PixelFont.Draw($"ОЧКИ: {gm.Score.Score}   {rec}", cx, y + 30 * s, s, Accent, 0);
                if (Mathf.Repeat(Time.time, 1f) < 0.6f)
                    PixelFont.Draw("НАЖМИ, ЧТОБЫ ПРОДОЛЖИТЬ", cx, y + 44 * s, s, Color.white, 0);
            }
        }

        static void Bar(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // Эффект печатной машинки
        static string Typed(string text, float time)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(time * 30f), 0, text.Length);
            return text.Substring(0, n);
        }
    }
}
