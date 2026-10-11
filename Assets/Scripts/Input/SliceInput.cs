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
        public float minSwipeSpeed = 6f;     // юнитов в секунду
        public float trailLifetime = 0.1f;
        public float trailWidthPx = 3f;

        public int StrokeId { get; private set; }
        public bool IsPressed { get; private set; }
        public bool PressedThisFrame { get; private set; }

        public event Action<int> OnStrokeStarted;
        public event Action<Vector2, Vector2, Vector2, int> OnSlice;   // a, b, направление, штрих

        LineRenderer trail;
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<float> times = new List<float>();
        Vector2 prev;

        void Start()
        {
            trail = new GameObject("SliceTrail").AddComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.sharedMaterial = GameManager.I.Player.SpriteMaterial;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(1, 1f));
            trail.widthMultiplier = trailWidthPx / ArtLibrary.PPU;
            trail.startColor = trail.endColor = Color.white;
            trail.sortingOrder = 20;
            trail.positionCount = 0;
        }

        void Update()
        {
            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
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

            trail.positionCount = points.Count;
            trail.SetPositions(points.ToArray());
        }
    }
}
