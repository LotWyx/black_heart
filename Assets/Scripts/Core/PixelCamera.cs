using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlackHeart
{
    // Пиксельный рендер: камера рисует в маленькую текстуру, которая
    // растягивается на экран целым множителем без сглаживания.
    public class PixelCamera : MonoBehaviour
    {
        public int targetPixelHeight = 180;
        public float cameraY = 0f;

        public Camera Cam { get; private set; }
        public int PixelScale => px;
        public int RtWidth => rtW;
        public int RtHeight => rtH;

        // Пиксель низкого разрешения (от левого верхнего угла) -> координаты IMGUI
        public Vector2 RtToGui(float x, float y) => new Vector2(offset.x + x * px, offset.y + y * px);
        // Масштаб интерфейса: один пиксель шрифта = 1/180 высоты экрана
        public int UiScale => Mathf.Max(1, Mathf.RoundToInt(Screen.height / 180f));

        RenderTexture rt;
        int px = 1, rtW, rtH, lastW, lastH;
        Vector2 offset;
        Vector3 basePos;
        float shake;

        bool initialized;

        void Awake() => Init();

        // Вызывается и из GameManager: остальным системам камера нужна уже в их Awake
        public void Init()
        {
            if (initialized) return;
            initialized = true;

            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            Cam.orthographic = true;
            Cam.transform.SetPositionAndRotation(new Vector3(0, cameraY, -10), Quaternion.identity);
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Color.black;
            Cam.allowHDR = false;
            Cam.allowMSAA = false;
            var data = Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            basePos = Cam.transform.position;

            // Пустая камера, чтобы экран очищался в чёрный
            var screenCam = new GameObject("ScreenCamera").AddComponent<Camera>();
            screenCam.cullingMask = 0;
            screenCam.clearFlags = CameraClearFlags.SolidColor;
            screenCam.backgroundColor = Color.black;
            screenCam.depth = Cam.depth + 1;

            Rebuild();
        }

        void Rebuild()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            px = Mathf.Max(1, Mathf.RoundToInt(Screen.height / (float)targetPixelHeight));
            rtW = Mathf.CeilToInt(Screen.width / (float)px);
            rtH = Mathf.CeilToInt(Screen.height / (float)px);
            if (rtW % 2 == 1) rtW++;
            if (rtH % 2 == 1) rtH++;
            offset = new Vector2((Screen.width - rtW * px) * 0.5f, (Screen.height - rtH * px) * 0.5f);

            Release();
            rt = new RenderTexture(rtW, rtH, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1
            };
            rt.Create();
            Cam.targetTexture = rt;
            Cam.orthographicSize = rtH / (2f * ArtLibrary.PPU);
        }

        void Release()
        {
            if (rt == null) return;
            if (Cam != null) Cam.targetTexture = null;
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        void OnDestroy() => Release();

        // Видимая область мира (без учёта тряски)
        public Rect ViewRect
        {
            get
            {
                float halfH = Cam.orthographicSize, halfW = halfH * rtW / rtH;
                return new Rect(basePos.x - halfW, basePos.y - halfH, halfW * 2, halfH * 2);
            }
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void LateUpdate()
        {
            if (Screen.width != lastW || Screen.height != lastH) Rebuild();
            shake = Mathf.Max(0, shake - Time.deltaTime);
            Cam.transform.position = basePos + (Vector3)(Random.insideUnitCircle * shake * 0.6f);
        }

        // Экран (низ-лево, как у Pointer) -> мир
        public Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector2 r = (screen - offset) / px;
            Vector3 w = Cam.ScreenToWorldPoint(new Vector3(r.x, r.y, -basePos.z));
            return w - (Cam.transform.position - basePos);
        }

        // Мир -> координаты IMGUI (верх-лево)
        public Vector2 WorldToGui(Vector2 world)
        {
            Vector3 r = Cam.WorldToScreenPoint((Vector3)world + (Cam.transform.position - basePos));
            return new Vector2(offset.x + r.x * px, Screen.height - (offset.y + r.y * px));
        }

        void OnGUI()
        {
            GUI.depth = 10;   // рисуется под HUD
            if (Event.current.type == EventType.Repaint && rt != null)
                GUI.DrawTexture(new Rect(offset.x, offset.y, rtW * px, rtH * px), rt, ScaleMode.StretchToFill, false);
        }
    }
}
