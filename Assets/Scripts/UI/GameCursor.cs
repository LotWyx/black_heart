using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Курсор игры — пиксельный клинок: белое лезвие остриём влево-вверх (точка нажатия), красная гарда
    // и рукоять, чёрный контур. Пока зажата кнопка (идёт разрез), лезвие наливается красным, как след разреза.
    // Рисуется кодом и ставится системным (аппаратным) курсором — без задержки на кадр.
    // На телефонах курсора нет.
    public class GameCursor : MonoBehaviour
    {
        const int Art = 15;        // размер рисунка с контуром, пиксели
        const int Scale = 2;       // во сколько раз увеличен (пиксели остаются ровными)
        const int Size = 32;       // текстура курсора — стандартные 32x32

        static readonly Color Outline = new Color(0.04f, 0.02f, 0.05f);
        static readonly Color Blade = new Color(0.91f, 0.91f, 0.94f);
        static readonly Color BladeShade = new Color(0.55f, 0.55f, 0.62f);
        static readonly Color Hot = new Color(1f, 0.54f, 0.6f);          // лезвие во время разреза
        static readonly Color HotShade = new Color(0.91f, 0.14f, 0.14f);
        static readonly Color Guard = new Color(0.91f, 0.14f, 0.14f);
        static readonly Color Grip = new Color(0.47f, 0.06f, 0.1f);

        Texture2D normal, slicing;
        bool shownSlicing, active;

        void Start()
        {
            if (Application.isMobilePlatform) return;
            normal = Build(false);
            slicing = Build(true);
            active = true;
            Show(false);
        }

        void Update()
        {
            if (!active) return;
            var mouse = Mouse.current;
            bool down = mouse != null && mouse.leftButton.isPressed;
            if (down != shownSlicing) Show(down);
        }

        void Show(bool down)
        {
            shownSlicing = down;
            // точка нажатия — кончик острия (пиксель (1,1) рисунка с контуром)
            Cursor.SetCursor(down ? slicing : normal, new Vector2(1 * Scale, 1 * Scale), CursorMode.Auto);
        }

        void OnDestroy()
        {
            if (active) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        // Рисунок 15x15: клинок по диагонали, гарда поперёк, рукоять и навершие; затем контур в 1 пиксель
        static Texture2D Build(bool hot)
        {
            var px = new Color?[Art, Art];
            void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < Art && y < Art) px[x, y] = c; }

            const int o = 1;   // отступ под контур
            for (int i = 0; i <= 7; i++)                       // лезвие с тенью по правой кромке
            {
                Set(o + i, o + i, hot ? Hot : Blade);
                if (i > 0) Set(o + i + 1, o + i, hot ? HotShade : BladeShade);
            }
            for (int k = -2; k <= 2; k++) Set(o + 8 + k, o + 8 - k, Guard);   // гарда поперёк клинка
            Set(o + 8, o + 8, Grip);
            Set(o + 9, o + 9, Grip); Set(o + 10, o + 10, Grip);              // рукоять
            Set(o + 11, o + 11, Guard); Set(o + 12, o + 11, Guard); Set(o + 11, o + 12, Guard);   // навершие

            // контур: пустые пиксели рядом с рисунком (по 8 направлениям)
            var shape = (Color?[,])px.Clone();
            for (int y = 0; y < Art; y++)
            for (int x = 0; x < Art; x++)
            {
                if (shape[x, y] != null) continue;
                for (int dy = -1; dy <= 1 && px[x, y] == null; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Art || ny >= Art || shape[nx, ny] == null) continue;
                    px[x, y] = Outline;
                    break;
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color32[Size * Size];
            tex.SetPixels32(clear);
            for (int y = 0; y < Art; y++)
            for (int x = 0; x < Art; x++)
            {
                if (px[x, y] == null) continue;
                for (int sy = 0; sy < Scale; sy++)
                for (int sx = 0; sx < Scale; sx++)
                    tex.SetPixel(x * Scale + sx, Size - 1 - (y * Scale + sy), px[x, y].Value);   // y текстуры — снизу вверх
            }
            tex.Apply();
            return tex;
        }
    }
}
