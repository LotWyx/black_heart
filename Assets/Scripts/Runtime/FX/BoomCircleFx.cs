using UnityEngine;

namespace BlackHeart
{
    // Метка паттерна «бум-круг»: здесь соберутся пули и разлетятся кольцом.
    // Рисуется по пикселям в своём размере (без растяжения): красное пунктирное кольцо с бегущим пунктиром,
    // редкая сетка внутри, засечки-прицел и мигающий «!» в центре. Держится hold секунд, затем сжимается
    // тремя ступенями (сплошное кольцо) перед выстрелом.
    public class BoomCircleFx : MonoBehaviour
    {
        const int ShrinkSteps = 3;
        const float DashSpeed = 10f;     // сдвиг пунктира, пикселей кольца в секунду

        static readonly Color32 Red = new Color32(232, 36, 36, 255);
        static readonly Color32 DarkRed = new Color32(110, 14, 22, 255);
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        float startSize, endSize, hold, shrink, t;
        SpriteRenderer sr;
        Texture2D tex;
        Color32[] buf;
        int texSize, drawnKey = int.MinValue;

        public static BoomCircleFx Spawn(Vector2 pos, float startDiameter, float endDiameter, float hold, float shrink)
        {
            var go = new GameObject("BoomCircle");
            go.transform.position = pos;
            var fx = go.AddComponent<BoomCircleFx>();
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sortingOrder = 2;
            fx.startSize = startDiameter;
            fx.endSize = endDiameter;
            fx.hold = hold;
            fx.shrink = shrink;
            fx.Redraw();
            return fx;
        }

        void Update()
        {
            // вышли из боя (в меню, проигрыш) — метка больше не нужна
            if (GameManager.I == null || GameManager.I.CurrentState != GameManager.State.Playing) { Destroy(gameObject); return; }
            t += Time.deltaTime;
            if (t >= hold + shrink) { Destroy(gameObject); return; }
            Redraw();
        }

        void Redraw()
        {
            float k = t < hold ? 0f : Mathf.Clamp01((t - hold) / shrink);
            float stepped = Mathf.Ceil(k * ShrinkSteps) / ShrinkSteps;
            float diameter = Mathf.Lerp(startSize, endSize, stepped);
            int d = Mathf.Max(5, Mathf.RoundToInt(diameter * ArtLibrary.PPU)) | 1;   // нечётный — центр в пикселе
            int dash = Mathf.FloorToInt(t * DashSpeed);
            bool flash = k > 0f;                                    // сжатие — кольцо сплошное
            bool blink = Mathf.Repeat(t, 0.4f) < 0.25f;
            int key = d * 100000 + (dash % 1000) * 10 + (flash ? 2 : 0) + (blink ? 1 : 0);
            if (key == drawnKey) return;
            drawnKey = key;
            Draw(d, dash, flash, blink);
        }

        void Draw(int d, int dash, bool flash, bool blink)
        {
            if (texSize != d)
            {
                if (tex != null) { Destroy(sr.sprite); Destroy(tex); }
                tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                buf = new Color32[d * d];
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), ArtLibrary.PPU);
                texSize = d;
            }
            Color32 ring = Red;
            float c = (d - 1) / 2f, r = d / 2f - 0.5f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 col = Clear;
                    if (dist <= r && dist > r - 1f)
                    {
                        // пунктир: штрихи 4 px через 2 px, бегут по кругу
                        float a = Mathf.Atan2(dy, dx);
                        int s = Mathf.FloorToInt((a + Mathf.PI) * r) + dash;
                        if (flash || ((s % 6) + 6) % 6 < 4) col = ring;
                    }
                    else if (dist <= r - 1f && dist > r - 2f && !flash)
                        col = DarkRed;                                       // тёмная внутренняя кромка
                    else if (dist <= r - 2f && x % 4 == 0 && y % 4 == 0)
                        col = DarkRed;                                       // редкая сетка
                    buf[y * d + x] = col;
                }
            // засечки прицела на четырёх сторонах
            int tick = Mathf.Max(2, d / 8);
            int ci = d / 2;
            for (int i = 2; i < 2 + tick; i++)
            {
                buf[ci * d + i] = ring; buf[ci * d + (d - 1 - i)] = ring;
                buf[i * d + ci] = ring; buf[(d - 1 - i) * d + ci] = ring;
            }
            // «!» в центре (мигает): палочка 3 px и точка; в текстуре y растёт вверх
            if (blink || flash)
            {
                for (int i = 0; i < 3; i++) buf[(ci + 1 + i) * d + ci] = ring;
                buf[(ci - 1) * d + ci] = ring;
            }
            tex.SetPixels32(buf);
            tex.Apply(false);
        }

        void OnDestroy()
        {
            if (sr != null && sr.sprite != null) Destroy(sr.sprite);
            if (tex != null) Destroy(tex);
        }
    }
}
