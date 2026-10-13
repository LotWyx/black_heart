using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHeart
{
    // Ввод разрезов. Pointer.current покрывает и мышь (ПК), и палец (мобильные).
    // Каждое новое нажатие — новый «штрих» (StrokeId).
    public class SliceInput : MonoBehaviour
    {
        [System.NonSerialized] public float minSwipeSpeed = 6f;     // юнитов в секунду
        [System.NonSerialized] public float trailLifetime = 0.12f;
        [System.NonSerialized] public float trailWidthPx = 3f;
        [System.NonSerialized] public float glowWidthPx = 8f;

        public int StrokeId { get; private set; }
        public bool IsPressed { get; private set; }
        public bool PressedThisFrame { get; private set; }

        public event Action<int> OnStrokeStarted;
        public event Action<Vector2, Vector2, Vector2, int> OnSlice;   // a, b, направление, штрих

        SliceVisual visual;
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<float> times = new List<float>();
        Vector2 prev;

        void Start() => visual = new SliceVisual("Slice", trailWidthPx, glowWidthPx);

        void Update()
        {
            var gm = GameManager.I;
            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed && !gm.Paused;
            Vector2 pos = pressed ? gm.Pixel.ScreenToWorld(pointer.position.ReadValue()) : Vector2.zero;
            // В фазе защиты резать можно только внутри рамки: за ней нет ни разреза, ни его следа.
            // Вернувшись в рамку с зажатой кнопкой, игрок начинает новый штрих.
            if (pressed && gm.Battle.Phase == BattlePhase.Defense && !gm.Box.Current.Contains(pos)) pressed = false;
            PressedThisFrame = pressed && !IsPressed;

            float now = Time.time;
            for (int i = times.Count - 1; i >= 0; i--)
                if (now - times[i] > trailLifetime) { times.RemoveAt(i); points.RemoveAt(i); }

            if (pressed)
            {
                if (PressedThisFrame)
                {
                    StrokeId++;
                    points.Clear();
                    times.Clear();
                    prev = pos;
                    OnStrokeStarted?.Invoke(StrokeId);
                }

                Vector2 delta = pos - prev;
                if (delta.magnitude / Mathf.Max(Time.deltaTime, 1e-4f) > minSwipeSpeed)
                    OnSlice?.Invoke(prev, pos, delta.normalized, StrokeId);

                points.Add(new Vector3(pos.x, pos.y, -1f));
                times.Add(now);
                prev = pos;
            }
            IsPressed = pressed;

            visual.Show(points, pressed);
        }
    }

    // Вид разреза: белое лезвие поверх красного свечения, хвост тонкий и прозрачный, к кончику — полная
    // ширина и яркость; на кончике яркий ромбик. Им же рисуется разрез-подсказка в обучении.
    public class SliceVisual
    {
        static readonly Color Glow = new Color(0.91f, 0.14f, 0.14f);   // красный акцент, как у рамки боя

        readonly LineRenderer trail, glow;
        readonly SpriteRenderer tip;

        public SliceVisual(string name, float trailWidthPx = 3f, float glowWidthPx = 8f, int order = 19)
        {
            glow = CreateLine(name + "Glow", glowWidthPx, order, Glow, Glow, 0f, 0.55f);
            trail = CreateLine(name + "Trail", trailWidthPx, order + 1, new Color(1f, 0.55f, 0.55f), Color.white, 0.3f, 1f);
            tip = new GameObject(name + "Tip").AddComponent<SpriteRenderer>();
            tip.sprite = ArtLibrary.Pixel;
            tip.sharedMaterial = GameManager.I.Player.SpriteMaterial;
            tip.sortingOrder = order + 2;
            tip.transform.localScale = Vector3.one * (3f / ArtLibrary.PPU);
            tip.transform.rotation = Quaternion.Euler(0, 0, 45f);
            tip.enabled = false;
        }

        static LineRenderer CreateLine(string name, float widthPx, int order, Color tail, Color head, float tailAlpha, float headAlpha)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = GameManager.I.Player.SpriteMaterial;
            line.widthCurve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(1, 1f));
            line.widthMultiplier = widthPx / ArtLibrary.PPU;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(tail, 0f), new GradientColorKey(head, 1f) },
                             new[] { new GradientAlphaKey(tailAlpha, 0f), new GradientAlphaKey(headAlpha, 1f) });
            line.colorGradient = gradient;
            line.numCapVertices = 2;
            line.sortingOrder = order;
            line.positionCount = 0;
            return line;
        }

        Vector3[] buffer = new Vector3[32];   // общий массив точек: растёт при нужде, заново не создаётся

        // points — от хвоста к кончику; tipOn — показать ромбик на кончике
        public void Show(List<Vector3> points, bool tipOn)
        {
            if (buffer.Length < points.Count) buffer = new Vector3[Mathf.NextPowerOfTwo(points.Count)];
            points.CopyTo(buffer);
            Apply(points.Count, tipOn);
        }

        public void Show(Vector3[] points, bool tipOn)
        {
            if (buffer.Length < points.Length) buffer = new Vector3[Mathf.NextPowerOfTwo(points.Length)];
            System.Array.Copy(points, buffer, points.Length);
            Apply(points.Length, tipOn);
        }

        void Apply(int count, bool tipOn)
        {
            trail.positionCount = glow.positionCount = count;
            trail.SetPositions(buffer);   // берутся первые positionCount точек
            glow.SetPositions(buffer);
            tip.enabled = tipOn && count > 1;
            if (tip.enabled) tip.transform.position = buffer[count - 1];
        }

        public void Hide() => Apply(0, false);
    }
}
