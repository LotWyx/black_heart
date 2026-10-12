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

        static readonly Color Glow = new Color(0.91f, 0.14f, 0.14f);   // красный акцент, как у рамки боя

        public int StrokeId { get; private set; }
        public bool IsPressed { get; private set; }
        public bool PressedThisFrame { get; private set; }

        public event Action<int> OnStrokeStarted;
        public event Action<Vector2, Vector2, Vector2, int> OnSlice;   // a, b, направление, штрих

        LineRenderer trail, glow;   // белое лезвие поверх красного свечения
        SpriteRenderer tip;         // яркий ромбик на кончике разреза
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<float> times = new List<float>();
        Vector2 prev;

        void Start()
        {
            // Хвост тонкий и прозрачный, к кончику — полная ширина и яркость
            glow = CreateLine("SliceGlow", glowWidthPx, 19, Glow, Glow, 0f, 0.55f);
            trail = CreateLine("SliceTrail", trailWidthPx, 20, new Color(1f, 0.55f, 0.55f), Color.white, 0.3f, 1f);

            tip = new GameObject("SliceTip").AddComponent<SpriteRenderer>();
            tip.sprite = ArtLibrary.Pixel;
            tip.sharedMaterial = GameManager.I.Player.SpriteMaterial;
            tip.sortingOrder = 21;
            tip.transform.localScale = Vector3.one * (3f / ArtLibrary.PPU);
            tip.transform.rotation = Quaternion.Euler(0, 0, 45f);
            tip.enabled = false;
        }

        LineRenderer CreateLine(string name, float widthPx, int order, Color tail, Color head, float tailAlpha, float headAlpha)
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

        void Update()
        {
            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed && !GameManager.I.Paused;
            PressedThisFrame = pressed && !IsPressed;

            float now = Time.time;
            for (int i = times.Count - 1; i >= 0; i--)
                if (now - times[i] > trailLifetime) { times.RemoveAt(i); points.RemoveAt(i); }

            if (pressed)
            {
                Vector2 pos = GameManager.I.Pixel.ScreenToWorld(pointer.position.ReadValue());
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

            var arr = points.ToArray();
            trail.positionCount = glow.positionCount = arr.Length;
            trail.SetPositions(arr);
            glow.SetPositions(arr);
            tip.enabled = pressed && arr.Length > 1;
            if (tip.enabled) tip.transform.position = arr[arr.Length - 1];
        }
    }
}
