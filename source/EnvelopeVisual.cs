using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dressmaker.DeskMail
{
    // Small vector UI geometry. No native Animation, quest controller, event,
    // texture readback, or prefab is needed for this cosmetic envelope.
    internal sealed class EnvelopeVisual : IDisposable
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<CanvasRenderer> renderers = new List<CanvasRenderer>();
        private readonly RectTransform flap, seal;
        private readonly CanvasGroup group, sealGroup;
        private readonly Material material;
        public RectTransform Root { get; }

        public EnvelopeVisual(Transform parent)
        {
            Root = Rect("OpeningEnvelope", parent, new Vector2(570, 330));
            Root.gameObject.SetActive(false);
            try
            {
                material = Reflect.Type("UnityEngine.UI.Graphic")
                    .GetProperty("defaultGraphicMaterial", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null, null) as Material;
                if (!material) throw new InvalidOperationException("The UI drawing material is not available.");
                group = Root.gameObject.AddComponent<CanvasGroup>();
                group.interactable = group.blocksRaycasts = false;

                var body = new Drawing();
                body.Quad(-291, -173, 291, 163, new Color(0.13f, 0.06f, 0.04f, 0.22f));
                body.Quad(-285, -165, 285, 165, new Color(0.59f, 0.43f, 0.27f));
                body.Quad(-282, -162, 282, 162, new Color(0.84f, 0.74f, 0.58f));
                // Side folds and the front pocket keep the envelope readable
                // once its top flap turns away.
                body.Triangle(new Vector2(-282, 162), new Vector2(-282, -162), new Vector2(30, -15), new Color(0.96f, 0.89f, 0.75f));
                body.Triangle(new Vector2(282, 162), new Vector2(-30, -15), new Vector2(282, -162), new Color(0.93f, 0.84f, 0.69f));
                body.Triangle(new Vector2(-282, -162), new Vector2(282, -162), new Vector2(0, 28), new Color(0.71f, 0.55f, 0.35f));
                body.Triangle(new Vector2(-279, -160), new Vector2(279, -160), new Vector2(0, 24), new Color(0.98f, 0.92f, 0.80f));
                Render(Root, body);

                flap = Rect("TopFlap", Root, new Vector2(570, 210));
                flap.pivot = new Vector2(0.5f, 1f);
                flap.anchoredPosition = new Vector2(0, 164);
                var fold = new Drawing();
                fold.Triangle(new Vector2(-284, 0), new Vector2(0, -210), new Vector2(284, 0), new Color(0.66f, 0.47f, 0.29f));
                fold.Triangle(new Vector2(-279, -2), new Vector2(0, -206), new Vector2(279, -2), new Color(0.99f, 0.94f, 0.83f));
                Render(flap, fold);

                seal = Rect("RoseWaxSeal", Root, new Vector2(62, 62));
                seal.anchoredPosition = new Vector2(0, -32);
                sealGroup = seal.gameObject.AddComponent<CanvasGroup>();
                var wax = new Drawing();
                wax.Circle(31, new Color(0.36f, 0.13f, 0.12f));
                wax.Circle(28, new Color(0.66f, 0.26f, 0.27f));
                wax.Circle(22, new Color(0.80f, 0.46f, 0.39f));
                wax.Circle(20, new Color(0.61f, 0.23f, 0.24f));
                var gold = new Color(0.92f, 0.71f, 0.47f);
                wax.Triangle(new Vector2(0, 12), new Vector2(-7, 0), new Vector2(0, -12), gold);
                wax.Triangle(new Vector2(0, 12), new Vector2(0, -12), new Vector2(7, 0), gold);
                Render(seal, wax);
            }
            catch { Dispose(); throw; }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private void Render(RectTransform rect, Drawing drawing)
        {
            var mesh = new Mesh { name = "DeskMail." + rect.name };
            meshes.Add(mesh);
            mesh.SetVertices(drawing.Vertices);
            mesh.SetColors(drawing.Colours);
            mesh.SetUVs(0, drawing.Uvs);
            mesh.SetTriangles(drawing.Indices, 0);
            mesh.RecalculateBounds();
            var renderer = rect.gameObject.AddComponent<CanvasRenderer>();
            renderers.Add(renderer);
            renderer.SetColor(Color.white);
            renderer.SetMaterial(material, Texture2D.whiteTexture);
            renderer.SetMesh(mesh);
        }

        public void Show()
        {
            Root.gameObject.SetActive(true);
            // Rebind owned geometry after activation; no reliance on a native
            // CanvasRenderer retaining its draw state across disable/enable.
            for (int i = 0; i < renderers.Count; i++)
            {
                renderers[i].SetMaterial(material, Texture2D.whiteTexture);
                renderers[i].SetMesh(meshes[i]);
            }
        }

        public void Pose(float flapOpening, float alpha, float sealAlpha, float scale, float y)
        {
            Root.anchoredPosition = new Vector2(0, y);
            Root.localScale = Vector3.one * scale;
            // A flat hinge, using the UI material's two-sided triangles.
            flap.localScale = new Vector3(1, Mathf.Cos(flapOpening * Mathf.PI), 1);
            group.alpha = alpha;
            sealGroup.alpha = sealAlpha;
            seal.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, sealAlpha);
        }

        public void Dispose()
        {
            if (Root) UnityEngine.Object.Destroy(Root.gameObject);
            foreach (var mesh in meshes) if (mesh) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
            renderers.Clear();
            // The default UI material is shared with the game and is never destroyed.
        }

        private sealed class Drawing
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Color> Colours = new List<Color>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Indices = new List<int>();
            public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                int start = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
                for (int i = 0; i < 3; i++)
                {
                    Colours.Add(color); Uvs.Add(Vector2.zero); Indices.Add(start + i);
                }
            }
            public void Quad(float left, float bottom, float right, float top, Color color)
            {
                Triangle(new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), color);
                Triangle(new Vector2(left, bottom), new Vector2(right, top), new Vector2(right, bottom), color);
            }
            public void Circle(float radius, Color color)
            {
                for (int i = 0; i < 40; i++)
                {
                    float a = i * Mathf.PI * 2f / 40f, b = (i + 1) * Mathf.PI * 2f / 40f;
                    Triangle(Vector2.zero, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                        new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
                }
            }
        }
    }
}
