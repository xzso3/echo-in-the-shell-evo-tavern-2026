using System.Collections.Generic;
using UnityEngine;

namespace PlagueSurvivor
{
    // All danger outlines use the same geometry as their damage checks.
    public static class BossArenaVisual
    {
        public static readonly Color Warning = new Color(1, .62f, .13f, .85f);
        public static readonly Color Active = new Color(1, .22f, .1f, .95f);
        public static SpriteRenderer Sprite(string name, Sprite sprite, Vector2 position, Transform parent, BossArenaConfig config, int order = 80)
        {
            var view = new GameObject(name).AddComponent<SpriteRenderer>();
            view.transform.SetParent(parent, false); view.transform.position = position;
            view.sprite = sprite; view.sharedMaterial = config.material; view.sortingOrder = order;
            return view;
        }
        public static LineRenderer Line(string name, Transform parent, BossArenaConfig config, Color color, float width = .055f, int order = 60)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = config.material; line.useWorldSpace = true; line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color; line.sortingOrder = order;
            return line;
        }
        public static void Segment(LineRenderer line, Vector2 a, Vector2 b)
        { line.positionCount = 2; line.SetPosition(0, a); line.SetPosition(1, b); }
        public static Transform Circle(string name, Vector2 center, float radius, Transform parent, BossArenaConfig config, Color color)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            // Clip every outline segment to the arena rather than letting off-screen warnings leak outside.
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64, b = (i + 1) * Mathf.PI * 2 / 64;
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Vector2 q = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius;
                if (!Clip(ref p, ref q, config.halfSize)) continue;
                Segment(Line("Range", root, config, color), p, q);
            }
            return root;
        }
        static bool Clip(ref Vector2 a, ref Vector2 b, Vector2 half)
        {
            Vector2 d = b - a; float lo = 0, hi = 1;
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(d[axis]) < .00001f) { if (Mathf.Abs(a[axis]) > half[axis]) return false; continue; }
                float t0 = (-half[axis] - a[axis]) / d[axis], t1 = (half[axis] - a[axis]) / d[axis];
                if (t0 > t1) { float t = t0; t0 = t1; t1 = t; }
                lo = Mathf.Max(lo, t0); hi = Mathf.Min(hi, t1); if (lo > hi) return false;
            }
            b = a + d * hi; a += d * lo; return true;
        }
        public static Transform Grid(bool vertical, Transform parent, BossArenaConfig config, bool active)
        {
            var root = new GameObject(vertical ? "Vertical Grid" : "Horizontal Grid").transform; root.SetParent(parent, false);
            foreach (float sign in new[] { -1f, 1f })
            {
                float length = vertical ? config.halfSize.y * 2 : config.halfSize.x * 2;
                for (float x = -length / 2; x < length / 2; x += 1)
                {
                    var p = vertical ? new Vector2(sign * config.gridCenter, x + .5f) : new Vector2(x + .5f, sign * config.gridCenter);
                    var sr = Sprite("Electric strip", active ? config.gridActiveSprite : config.gridWarningSprite, p, root, config, 30);
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = vertical ? new Vector2(config.gridWidth, 1) : new Vector2(1, config.gridWidth);
                    sr.color = new Color(1, 1, 1, active ? .82f : .48f);
                }
                float c = sign * config.gridCenter, w = config.gridWidth / 2;
                foreach (float edge in new[] { c - w, c + w })
                {
                    var line = Line("Exact strip edge", root, config, active ? Active : Warning);
                    Segment(line, vertical ? new Vector2(edge, -length / 2) : new Vector2(-length / 2, edge),
                        vertical ? new Vector2(edge, length / 2) : new Vector2(length / 2, edge));
                }
            }
            return root;
        }
        public static void Remove(Transform root)
        { if (root) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); } }
    }
}
