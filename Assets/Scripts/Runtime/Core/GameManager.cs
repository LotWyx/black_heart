using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Точка сборки: создаёт подсистемы, связывает ввод с логикой и управляет жизнью/смертью игрока.
    // Подсистемы можно заранее повесить на этот же объект, чтобы настраивать их в инспекторе.
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public enum State { Menu, Playing, Dying, GameOver }

        // Параметры игрока — в ассете GameBalance
        public int playerMaxHp => GameBalance.Current.playerMaxHp;
        public float playerHitRadius => GameBalance.Current.playerHitRadius;
        public float playerInvulnTime => GameBalance.Current.playerInvulnTime;

        public State CurrentState { get; private set; }
        public bool IsPlaying => CurrentState == State.Playing && !Paused;
        public bool Paused { get; private set; }

        public PixelCamera Pixel { get; private set; }
        public ScoreKeeper Score { get; private set; }
        public BattleBox Box { get; private set; }
        public PlayerHeart Player { get; private set; }
        public SliceInput Slicer { get; private set; }
        public ProjectileManager Projectiles { get; private set; }
        public BattleController Battle { get; private set; }
        public BattleBackground Background { get; private set; }
        public MainMenu Menu { get; private set; }
        public TutorialController Tutorial { get; private set; }
        public IntroController Intro { get; private set; }
        public ScreenFader Fader { get; private set; }

        float stateTime;

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            Pixel = Ensure<PixelCamera>();
            Pixel.Init();
            Score = Ensure<ScoreKeeper>();
            Background = new GameObject("Background").AddComponent<BattleBackground>();
            Box = new GameObject("BattleBox").AddComponent<BattleBox>();
            Player = new GameObject("PlayerHeart").AddComponent<PlayerHeart>();
            Player.HitRadius = playerHitRadius;
            Player.InvulnTime = playerInvulnTime;
            Slicer = Ensure<SliceInput>();
            Projectiles = Ensure<ProjectileManager>();
            Battle = Ensure<BattleController>();
            Ensure<HUD>();
            Ensure<SfxPlayer>();
            Ensure<MusicPlayer>();
            Menu = Ensure<MainMenu>();
            Tutorial = Ensure<TutorialController>();
            Ensure<PauseMenu>();
            Intro = Ensure<IntroController>();
            Fader = Ensure<ScreenFader>();
            Ensure<GameCursor>();

            Slicer.OnSlice += HandleSlice;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        T Ensure<T>() where T : Component
        {
            return TryGetComponent<T>(out var c) ? c : gameObject.AddComponent<T>();
        }

        void OnDestroy()
        {
            if (I != this) return;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            I = null;
        }

        void Start()
        {
            GameSettings.Apply();
            ShowMenu();
            // вступление при каждом запуске; когда оно кончится или его пропустят, экран гаснет и проявляется уже меню
            Menu.Hide();
            if (!Intro.Play(() => Menu.Show())) Menu.Show();
        }

        // Главное меню: бой остановлен, поле и душа скрыты
        public void ShowMenu()
        {
            SetPausedInternal(false);
            StopAllCoroutines();
            Tutorial.Abort();
            Battle.Stop();
            Projectiles.ClearAll();
            Battle.DespawnEnemy();
            Box.SetVisible(false);
            Background.SetVisible(false);
            Player.SetHidden(true);
            CurrentState = State.Menu;
            stateTime = 0;
            Menu.Show();
        }

        // При самом первом запуске перед боем идёт обучение
        public void StartGame() => StartGame(!TutorialController.Completed);

        public void StartGame(bool withTutorial)
        {
            SetPausedInternal(false);
            Menu.Hide();
            StopAllCoroutines();
            Tutorial.Abort();
            Battle.Stop();
            Projectiles.ClearAll();
            Projectiles.Frozen = false;
            Box.TimeStopped = false;
            Battle.DespawnEnemy();
            Score.ResetScore();
            Player.ResetState(playerMaxHp);
            Background.SetVisible(true);
            Box.SetVisible(true);
            Box.PlayIntro();
            CurrentState = State.Playing;
            stateTime = 0;
            GameEvents.RaiseGameStarted();
            if (withTutorial) Tutorial.Begin();
            else Battle.Begin();
        }

        // После обучения: полное HP, счёт с нуля, первый настоящий враг
        public void BeginMainBattle()
        {
            Player.ResetState(playerMaxHp);
            Score.ResetScore();
            Battle.Begin();
        }

        void HandleSlice(Vector2 a, Vector2 b, Vector2 dir, int strokeId)
        {
            if (!IsPlaying) return;
            if (Tutorial.IsRunning)
            {
                Tutorial.HandleSlice(a, b, dir);
                return;
            }
            Projectiles.Slice(a, b, dir);
            Battle.HandleSlice(a, b);
        }

        void Update()
        {
            stateTime += Time.deltaTime;
            // с экрана проигрыша в меню — через затемнение (сам экран проигрыша появляется резко)
            if (CurrentState == State.GameOver && stateTime > 3f && Slicer.PressedThisFrame)
                Fader.Transition(ShowMenu);
        }

        // Пауза: время стоит, бой и обучение не реагируют на ввод
        public void SetPaused(bool paused)
        {
            if (CurrentState != State.Playing) return;
            SetPausedInternal(paused);
        }

        void SetPausedInternal(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        // Выход из боя в главное меню (рекорд засчитывается)
        public void QuitToMenu()
        {
            Score.CommitBest();
            Fader.Transition(ShowMenu);
        }

        void HandlePlayerDied() => StartCoroutine(GameOverSequence());

        // Как в Undertale: всё замирает, душа трескается, разлетается, затем GAME OVER
        IEnumerator GameOverSequence()
        {
            CurrentState = State.Dying;
            Battle.Stop();
            Projectiles.ClearAll();
            Battle.DespawnEnemy();
            Box.SetVisible(false);
            Background.SetVisible(false);
            Score.CommitBest();

            yield return new WaitForSeconds(0.6f);
            Player.Crack();
            yield return new WaitForSeconds(1f);
            Player.Shatter();
            yield return new WaitForSeconds(1.4f);

            CurrentState = State.GameOver;
            stateTime = 0;
            GameEvents.RaiseGameOver(Score.Score);
        }
    }
}
