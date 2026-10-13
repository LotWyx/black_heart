using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Рамка боя в стиле Rhythm Doctor: белая граница 3 px с чёрным контуром снаружи, внутренняя линия
    // акцентного цвета, уголки-заклёпки и метки на серединах сторон, чёрная заливка (фон виден только снаружи). Меняет размер ступенями между обычным видом
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

        static readonly Color Accent = new Color(0.91f, 0.14f, 0.14f);     // #E82424
        static readonly Color Telegraph = new Color(1f, 0.54f, 0.12f);    // #FF8A1E
        static readonly Color TimeStop = new Color(0.91f, 0.91f, 0.94f);  // #E8E8F0

        public Rect Current { get; private set; }
        public Rect Target { get; private set; }
        public bool IsAnimating => resizeT < 1f || intro;
        public Rect HudRect => intro ? Target : Current;   // HUD не прыгает во время появления рамки
        public bool TimeStopped { get; set; }               // время остановлено — внутренняя линия светлеет

        readonly SpriteRenderer[] sides = new SpriteRenderer[4];
        readonly SpriteRenderer[] inner = new SpriteRenderer[4];
        readonly SpriteRenderer[] outline = new SpriteRenderer[4];
        // уголки: ЛВ, ПВ, ЛН, ПН; метки: верх, низ, лево, право. [i, 0] — основа, [i, 1] — акцентный слой
        readonly SpriteRenderer[,] corners = new SpriteRenderer[4, 2];
        readonly SpriteRenderer[,] notches = new SpriteRenderer[4, 2];
        SpriteRenderer fill;
        Transform mask;   // маска в размер рамки: пули групп (стена, клетка, волна) видны только внутри неё

        // Фон внутри рамки: тёмная сетка из точек и крестиков, медленно ползущая по диагонали,
        // и кровавое свечение у краёв. Текстура в размер рамки, пересобирается при смене размера/сдвига.
        const int GridStep = 10, CrossStep = 40;
        const float ScrollSpeed = 3f;   // пикселей в секунду
        static readonly Color32 PatternBase = new Color32(6, 3, 8, 255);
        static readonly Color32 PatternDot = new Color32(30, 12, 22, 255);
        static readonly Color32 PatternCross = new Color32(52, 16, 30, 255);
        static readonly Color PatternGlow = new Color(0.27f, 0.02f, 0.06f);
        SpriteRenderer pattern;
        Texture2D patternTex;
        Color32[] patternBuf;
        int patternW, patternH, patternOffset = -1;
        Rect from;
        float resizeT = 1f, telegraphLeft;
        int flashFrames;
        bool intro;

        void Awake()
        {
            fill = CreatePart("Fill", Color.black, -10);
            var maskObject = new GameObject("Mask");
            maskObject.transform.SetParent(transform);
            maskObject.AddComponent<SpriteMask>().sprite = ArtLibrary.Pixel;
            mask = maskObject.transform;
            pattern = CreatePart("Pattern", Color.white, -9);
            for (int i = 0; i < 4; i++) sides[i] = CreatePart("Side", Color.white, 0);
            for (int i = 0; i < 4; i++) inner[i] = CreatePart("Inner", Accent, 0);
            for (int i = 0; i < 4; i++) outline[i] = CreatePart("Outline", Color.black, 0);
            for (int i = 0; i < 4; i++)
            {
                corners[i, 0] = CreateDetail("Corner", ArtIds.BoxCorner, Color.white, 2, i % 2 == 1, i >= 2);
                corners[i, 1] = CreateDetail("CornerAccent", ArtIds.BoxCornerAccent, Accent, 3, i % 2 == 1, i >= 2);
                bool vertical = i >= 2;
                notches[i, 0] = CreateDetail("Notch", vertical ? ArtIds.BoxNotchV : ArtIds.BoxNotchH, Color.white, 2, i == 3, i == 1);
                notches[i, 1] = CreateDetail("NotchAccent", vertical ? ArtIds.BoxNotchVAccent : ArtIds.BoxNotchHAccent, Accent, 3, i == 3, i == 1);
            }
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

        SpriteRenderer CreateDetail(string name, string id, Color color, int order, bool flipX, bool flipY)
        {
            var sr = CreatePart(name, color, order);
            sr.sprite = ArtLibrary.Get(id);
            sr.flipX = flipX;
            sr.flipY = flipY;
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
            Rect r = Snap(Current);
            float th = thicknessPx / ArtLibrary.PPU, px = 1f / ArtLibrary.PPU;
            Place(fill.transform, r.center, r.size);
            Place(mask, r.center, r.size);
            UpdatePattern(r);
            Place(sides[0].transform, new Vector2(r.center.x, r.yMax + th / 2), new Vector2(r.width + 2 * th, th));
            Place(sides[1].transform, new Vector2(r.center.x, r.yMin - th / 2), new Vector2(r.width + 2 * th, th));
            Place(sides[2].transform, new Vector2(r.xMin - th / 2, r.center.y), new Vector2(th, r.height));
            Place(sides[3].transform, new Vector2(r.xMax + th / 2, r.center.y), new Vector2(th, r.height));

            // Чёрный контур 1 px снаружи белой границы — рамка не сливается с фоном
            float o = th + px / 2;
            Place(outline[0].transform, new Vector2(r.center.x, r.yMax + o), new Vector2(r.width + 2 * th + 2 * px, px));
            Place(outline[1].transform, new Vector2(r.center.x, r.yMin - o), new Vector2(r.width + 2 * th + 2 * px, px));
            Place(outline[2].transform, new Vector2(r.xMin - o, r.center.y), new Vector2(px, r.height + 2 * th));
            Place(outline[3].transform, new Vector2(r.xMax + o, r.center.y), new Vector2(px, r.height + 2 * th));

            // Уголки: центр спрайта 10x10 совпадает с углом рамки. Метки: смещены на 2 px наружу,
            // по длине стороны — к сетке пикселей (у спрайта нечётная длина, центр в середине пикселя)
            bool details = r.width > 24 * px && r.height > 24 * px;
            float cx = Mathf.Floor(r.center.x * ArtLibrary.PPU) / ArtLibrary.PPU + px / 2;
            float cy = Mathf.Floor(r.center.y * ArtLibrary.PPU) / ArtLibrary.PPU + px / 2;
            var cornerPos = new[] { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin) };
            var notchPos = new[] { new Vector2(cx, r.yMax + 2 * px), new Vector2(cx, r.yMin - 2 * px), new Vector2(r.xMin - 2 * px, cy), new Vector2(r.xMax + 2 * px, cy) };
            for (int k = 0; k < 4; k++)
                for (int l = 0; l < 2; l++)
                {
                    corners[k, l].transform.position = cornerPos[k];
                    notches[k, l].transform.position = notchPos[k];
                    corners[k, l].enabled = notches[k, l].enabled = details;
                }

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
            for (int k = 0; k < 4; k++)
            {
                corners[k, 1].color = innerColor;
                notches[k, 1].color = innerColor;
            }
        }

        void UpdatePattern(Rect r)
        {
            int w = Mathf.RoundToInt(r.width * ArtLibrary.PPU), h = Mathf.RoundToInt(r.height * ArtLibrary.PPU);
            pattern.enabled = w > 8 && h > 8;
            if (!pattern.enabled) return;
            int offset = Mathf.FloorToInt(Time.time * ScrollSpeed) % CrossStep;
            if (w != patternW || h != patternH)
            {
                if (patternTex != null) { Destroy(pattern.sprite); Destroy(patternTex); }
                patternTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                patternBuf = new Color32[w * h];
                pattern.sprite = Sprite.Create(patternTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ArtLibrary.PPU);
                patternW = w; patternH = h; patternOffset = -1;
            }
            pattern.transform.position = r.center;
            pattern.transform.localScale = Vector3.one;
            if (offset == patternOffset) return;
            patternOffset = offset;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int gx = (x + offset) % CrossStep, gy = (y + offset) % CrossStep;
                    Color32 c = PatternBase;
                    if (gx % GridStep == 0 && gy % GridStep == 0) c = PatternDot;
                    if ((gx == 0 && (gy <= 1 || gy >= CrossStep - 1)) || (gy == 0 && (gx <= 1 || gx >= CrossStep - 1))) c = PatternCross;
                    // свечение у краёв ступенями (у самого края — зазор и акцентная линия рамки)
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(w - 1 - x, h - 1 - y));
                    float glow = d < 2 ? 0 : d < 5 ? 0.45f : d < 9 ? 0.25f : d < 15 ? 0.12f : 0;
                    if (glow > 0) c = Color32.Lerp(c, PatternGlow, glow);
                    patternBuf[y * w + x] = c;
                }
            patternTex.SetPixels32(patternBuf);
            patternTex.Apply(false);
        }

        void OnDestroy()
        {
            if (pattern != null && pattern.sprite != null) Destroy(pattern.sprite);
            if (patternTex != null) Destroy(patternTex);
        }

        // Края рамки — по сетке пикселей, чтобы уголки и метки ложились ровно на полосы
        static Rect Snap(Rect r)
        {
            float p = ArtLibrary.PPU;
            return Rect.MinMaxRect(Mathf.Round(r.xMin * p) / p, Mathf.Round(r.yMin * p) / p,
                                   Mathf.Round(r.xMax * p) / p, Mathf.Round(r.yMax * p) / p);
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
