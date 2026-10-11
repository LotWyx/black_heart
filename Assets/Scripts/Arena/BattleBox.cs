using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Рамка боя в стиле Rhythm Doctor: белая граница 3 px, внутренняя линия акцентного цвета,
    // чёрная заливка (фон виден только снаружи). Меняет размер ступенями между обычным видом
    // (фаза атаки, внизу экрана) и расширенным (фаза защиты). Сердце всегда в центре рамки.
    public class BattleBox : MonoBehaviour
    {
        [Header("Фаза атаки")]
        public Vector2 normalSize = new Vector2(6f, 3.6f);
        public float normalBottomSpace = 1.9f;   // место под рамкой для HP и подсказок

        [Header("Фаза защиты: отступы от краёв экрана")]
        public float expandedSideMargin = 1.5f;
        public float expandedTopMargin = 1.4f;
        public float expandedBottomMargin = 1.6f;

        [Header("Вид")]
        public float thicknessPx = 3f;
        public float resizeDuration = 0.3f;
        public int resizeSteps = 4;              // изменение размера — ступенями, не плавно
        public float introStepTime = 0.05f;

        static readonly Color Accent = new Color(0.86f, 0.25f, 0.42f);     // #DB406B
        static readonly Color Telegraph = new Color(1f, 0.54f, 0.12f);    // #FF8A1E
        static readonly Color TimeStop = new Color(0.91f, 0.91f, 0.94f);  // #E8E8F0

        public Rect Current { get; private set; }
        public Rect Target { get; private set; }
        public bool IsAnimating => resizeT < 1f || intro;
        public Rect HudRect => intro ? Target : Current;   // HUD не прыгает во время появления рамки
        public bool TimeStopped { get; set; }               // время остановлено — внутренняя линия светлеет

        readonly SpriteRenderer[] sides = new SpriteRenderer[4];
        readonly SpriteRenderer[] inner = new SpriteRenderer[4];
        SpriteRenderer fill;
        Rect from;
        float resizeT = 1f, telegraphLeft;
        int flashFrames;
        bool intro;

        void Awake()
        {
            fill = CreatePart("Fill", Color.black, -10);
            for (int i = 0; i < 4; i++) sides[i] = CreatePart("Side", Color.white, 0);
            for (int i = 0; i < 4; i++) inner[i] = CreatePart("Inner", Accent, 0);
            Current = Target = from = NormalRect();
            UpdateParts();
        }

        void OnEnable()
        {
            GameEvents.OnPlayerTakeDamage += OnPlayerDamaged;
            GameEvents.OnEnemyAttack += OnEnemyAttack;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerTakeDamage -= OnPlayerDamaged;
            GameEvents.OnEnemyAttack -= OnEnemyAttack;
        }

        void OnPlayerDamaged(int amount, int hp) => flashFrames = 1;          // вся рамка белая на 1 кадр
        void OnEnemyAttack(Enemy e) => telegraphLeft = 0.3f;                    // внутренняя линия оранжевая на время замаха

        SpriteRenderer CreatePart(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.Pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public Rect NormalRect()
        {
            Rect v = GameManager.I.Pixel.ViewRect;
            float centerY = v.yMin + normalBottomSpace + normalSize.y / 2f;
            return new Rect(v.center.x - normalSize.x / 2f, centerY - normalSize.y / 2f, normalSize.x, normalSize.y);
        }

        public Rect ExpandedRect()
        {
            Rect v = GameManager.I.Pixel.ViewRect;
            return Rect.MinMaxRect(v.xMin + expandedSideMargin, v.yMin + expandedBottomMargin,
                                   v.xMax - expandedSideMargin, v.yMax - expandedTopMargin);
        }

        public void Expand() => ResizeTo(ExpandedRect());
        public void Shrink() => ResizeTo(NormalRect());
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SnapToNormal()
        {
            intro = false;
            StopAllCoroutines();
            Current = Target = from = NormalRect();
            resizeT = 1f;
            UpdateParts();
        }

        void ResizeTo(Rect target)
        {
            from = Current;
            Target = target;
            resizeT = 0f;
        }

        // Появление в начале боя: точка в центре → полоса на всю ширину → полная высота
        public void PlayIntro()
        {
            SnapToNormal();
            if (gameObject.activeInHierarchy) StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            intro = true;
            Rect full = NormalRect();
            float dot = thicknessPx / ArtLibrary.PPU;
            Vector2 c = full.center;
            for (int i = 0; i <= 3; i++)
            {
                float w = Mathf.Lerp(dot, full.width, i / 3f);
                Current = new Rect(c.x - w / 2, c.y - dot / 2, w, dot);
                UpdateParts();
                yield return new WaitForSeconds(introStepTime);
            }
            for (int i = 1; i <= 3; i++)
            {
                float h = Mathf.Lerp(dot, full.height, i / 3f);
                Current = new Rect(full.xMin, c.y - h / 2, full.width, h);
                UpdateParts();
                yield return new WaitForSeconds(introStepTime);
            }
            Current = Target = from = full;
            intro = false;
            UpdateParts();
        }

        void Update()
        {
            telegraphLeft = Mathf.Max(0, telegraphLeft - Time.deltaTime);
            if (!intro && resizeT < 1f)
            {
                resizeT = Mathf.Min(1f, resizeT + Time.deltaTime / Mathf.Max(0.01f, resizeDuration));
                float k = Mathf.Ceil(resizeT * resizeSteps) / resizeSteps;
                Current = Rect.MinMaxRect(
                    Mathf.Lerp(from.xMin, Target.xMin, k), Mathf.Lerp(from.yMin, Target.yMin, k),
                    Mathf.Lerp(from.xMax, Target.xMax, k), Mathf.Lerp(from.yMax, Target.yMax, k));
            }
            UpdateParts();
        }

        void UpdateParts()
        {
            Rect r = Current;
            float th = thicknessPx / ArtLibrary.PPU, px = 1f / ArtLibrary.PPU;
            Place(fill.transform, r.center, r.size);
            Place(sides[0].transform, new Vector2(r.center.x, r.yMax + th / 2), new Vector2(r.width + 2 * th, th));
            Place(sides[1].transform, new Vector2(r.center.x, r.yMin - th / 2), new Vector2(r.width + 2 * th, th));
            Place(sides[2].transform, new Vector2(r.xMin - th / 2, r.center.y), new Vector2(th, r.height));
            Place(sides[3].transform, new Vector2(r.xMax + th / 2, r.center.y), new Vector2(th, r.height));

            // Внутренняя линия 1 px с отступом 1 px от белой границы
            Rect i = Rect.MinMaxRect(r.xMin + px, r.yMin + px, r.xMax - px, r.yMax - px);
            bool showInner = i.width > 2 * px && i.height > 2 * px;
            Place(inner[0].transform, new Vector2(i.center.x, i.yMax - px / 2), new Vector2(i.width, px));
            Place(inner[1].transform, new Vector2(i.center.x, i.yMin + px / 2), new Vector2(i.width, px));
            Place(inner[2].transform, new Vector2(i.xMin + px / 2, i.center.y), new Vector2(px, i.height));
            Place(inner[3].transform, new Vector2(i.xMax - px / 2, i.center.y), new Vector2(px, i.height));

            Color innerColor = flashFrames > 0 ? Color.white
                             : telegraphLeft > 0 ? Telegraph
                             : TimeStopped ? TimeStop : Accent;
            foreach (var s in inner)
            {
                s.enabled = showInner;
                s.color = innerColor;
            }
        }

        void LateUpdate()
        {
            if (flashFrames > 0) flashFrames--;
        }

        static void Place(Transform t, Vector2 pos, Vector2 size)
        {
            t.position = pos;
            t.localScale = new Vector3(size.x, size.y, 1);
        }

        // Расстояние от точки внутри рамки до её края по направлению
        public float DistanceToEdge(Vector2 origin, Vector2 dir) => TimeToExit(origin, dir.normalized);

        // Через сколько секунд точка, движущаяся со скоростью velocity, выйдет за рамку
        public float TimeToExit(Vector2 origin, Vector2 velocity)
        {
            Rect r = Current;
            float tx = velocity.x > 1e-5f ? (r.xMax - origin.x) / velocity.x
                     : velocity.x < -1e-5f ? (r.xMin - origin.x) / velocity.x : float.PositiveInfinity;
            float ty = velocity.y > 1e-5f ? (r.yMax - origin.y) / velocity.y
                     : velocity.y < -1e-5f ? (r.yMin - origin.y) / velocity.y : float.PositiveInfinity;
            return Mathf.Max(0, Mathf.Min(tx, ty));
        }
    }
}
