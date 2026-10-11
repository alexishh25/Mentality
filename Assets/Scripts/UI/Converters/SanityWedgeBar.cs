using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.UI.Views
{
    // Barra en cuna del Figma (HUD/BarraCordura): mas baja a la izquierda, degradado mapeado al ancho completo,
    // un segmento por impacto, estela fantasma y contorno. Colores via propiedades USS (--wedge-*).
    [UxmlElement]
    public partial class SanityWedgeBar : VisualElement
    {
        public const string UssClassName = "sanity-wedge";

        private static readonly CustomStyleProperty<Color> TrackColorProperty = new CustomStyleProperty<Color>("--wedge-track-color");
        private static readonly CustomStyleProperty<Color> OutlineColorProperty = new CustomStyleProperty<Color>("--wedge-outline-color");
        private static readonly CustomStyleProperty<float> OutlineWidthProperty = new CustomStyleProperty<float>("--wedge-outline-width");
        private static readonly CustomStyleProperty<Color> DividerColorProperty = new CustomStyleProperty<Color>("--wedge-divider-color");
        private static readonly CustomStyleProperty<Color> GhostColorProperty = new CustomStyleProperty<Color>("--wedge-ghost-color");
        private static readonly CustomStyleProperty<Color> Gradient0Property = new CustomStyleProperty<Color>("--wedge-gradient-0");
        private static readonly CustomStyleProperty<Color> Gradient1Property = new CustomStyleProperty<Color>("--wedge-gradient-1");
        private static readonly CustomStyleProperty<Color> Gradient2Property = new CustomStyleProperty<Color>("--wedge-gradient-2");
        private static readonly CustomStyleProperty<Color> Gradient3Property = new CustomStyleProperty<Color>("--wedge-gradient-3");

        // Paradas del degradado del Figma (0, 0.18, 0.55, 1).
        private static readonly float[] GradientStops = { 0f, 0.18f, 0.55f, 1f };
        private const float DividerWidth = 2f;

        private readonly Color[] _gradientColors =
        {
            new Color32(0x8E, 0x0F, 0x24, 0xFF),
            new Color32(0xE0, 0x26, 0x3F, 0xFF),
            new Color32(0xE8, 0xC5, 0x47, 0xFF),
            new Color32(0x8F, 0xD1, 0x8A, 0xFF)
        };

        private Color _trackColor = new Color(7f / 255f, 7f / 255f, 10f / 255f, 0f);
        private Color _outlineColor = new Color(7f / 255f, 7f / 255f, 10f / 255f, 1f);
        private float _outlineWidth = 1.5f;
        private Color _dividerColor = new Color(7f / 255f, 7f / 255f, 10f / 255f, 0.85f);
        private Color _ghostColor = new Color(237f / 255f, 230f / 255f, 216f / 255f, 0.85f);

        private readonly VisualElement _fillLayer;
        private readonly VisualElement _overlayLayer;

        private float _value = 1f;
        private float _ghostStart;
        private float _ghostWidth;
        private int _segments = 10;
        private float _leftHeightRatio = 10f / 24f;

        [UxmlAttribute]
        [Range(0, 1)]
        public float value 
        {
            get => _value;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, _value)) return;
                _value = clamped;
                _fillLayer.MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public int segments
        {
            get => _segments;
            set
            {
                _segments = Mathf.Max(1, value);
                _fillLayer.MarkDirtyRepaint();
            }
        }

        // Alto del borde izquierdo respecto al derecho (10/24 en el Figma).
        [UxmlAttribute]
        public float leftHeightRatio
        {
            get => _leftHeightRatio;
            set
            {
                _leftHeightRatio = Mathf.Clamp01(value);
                MarkAllDirty();
            }
        }

        public SanityWedgeBar()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += DrawTrack;

            _fillLayer = CreateLayer(DrawFill);
            _overlayLayer = CreateLayer(DrawOverlay);
            Add(_fillLayer);
            Add(_overlayLayer);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public void SetGhostTrail(float start, float width)
        {
            start = Mathf.Clamp01(start);
            width = Mathf.Clamp(width, 0f, 1f - start);
            if (Mathf.Approximately(start, _ghostStart) && Mathf.Approximately(width, _ghostWidth)) return;

            _ghostStart = start;
            _ghostWidth = width;
            _overlayLayer.MarkDirtyRepaint();
        }

        private static VisualElement CreateLayer(Action<MeshGenerationContext> draw)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.generateVisualContent += draw;
            return layer;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle custom = evt.customStyle;
            if (custom.TryGetValue(TrackColorProperty, out Color track)) _trackColor = track;
            if (custom.TryGetValue(OutlineColorProperty, out Color outline)) _outlineColor = outline;
            if (custom.TryGetValue(OutlineWidthProperty, out float outlineWidth)) _outlineWidth = outlineWidth;
            if (custom.TryGetValue(DividerColorProperty, out Color divider)) _dividerColor = divider;
            if (custom.TryGetValue(GhostColorProperty, out Color ghost)) _ghostColor = ghost;
            if (custom.TryGetValue(Gradient0Property, out Color g0)) _gradientColors[0] = g0;
            if (custom.TryGetValue(Gradient1Property, out Color g1)) _gradientColors[1] = g1;
            if (custom.TryGetValue(Gradient2Property, out Color g2)) _gradientColors[2] = g2;
            if (custom.TryGetValue(Gradient3Property, out Color g3)) _gradientColors[3] = g3;
            MarkAllDirty();
        }

        private void MarkAllDirty()
        {
            MarkDirtyRepaint();
            _fillLayer?.MarkDirtyRepaint();
            _overlayLayer?.MarkDirtyRepaint();
        }

        private float TopLeft(float height) => height * (1f - _leftHeightRatio);

        private float TopAt(float x, float width, float height) => TopLeft(height) * (1f - x / width);

        private void DrawTrack(MeshGenerationContext mgc)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            Painter2D painter = mgc.painter2D;
            painter.fillColor = _trackColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, TopLeft(rect.height)));
            painter.LineTo(new Vector2(rect.width, 0f));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(0f, rect.height));
            painter.ClosePath();
            painter.Fill();
        }

        private void DrawFill(MeshGenerationContext mgc)
        {
            Rect rect = _fillLayer.contentRect;
            float w = rect.width;
            float h = rect.height;
            float fillEnd = _value * w;
            if (w <= 0f || h <= 0f || fillEnd <= 0f) return;

            int gradientQuads = 0;
            for (int i = 0; i < GradientStops.Length - 1; i++)
                if (GradientStops[i] * w < fillEnd) gradientQuads++;

            int dividerQuads = 0;
            for (int i = 1; i < _segments; i++)
                if (w * i / _segments - DividerWidth * 0.5f < fillEnd) dividerQuads++;

            int quads = gradientQuads + dividerQuads;
            MeshWriteData mesh = mgc.Allocate(quads * 4, quads * 6);
            ushort vertex = 0;

            for (int i = 0; i < GradientStops.Length - 1; i++)
            {
                float x0 = GradientStops[i] * w;
                if (x0 >= fillEnd) break;
                float x1 = Mathf.Min(GradientStops[i + 1] * w, fillEnd);
                Color c0 = _gradientColors[i];
                Color c1 = Color.Lerp(_gradientColors[i], _gradientColors[i + 1],
                    (x1 / w - GradientStops[i]) / (GradientStops[i + 1] - GradientStops[i]));
                WriteQuad(mesh, ref vertex, x0, x1, w, h, c0, c1);
            }

            for (int i = 1; i < _segments; i++)
            {
                float x0 = w * i / _segments - DividerWidth * 0.5f;
                if (x0 >= fillEnd) break;
                float x1 = Mathf.Min(x0 + DividerWidth, fillEnd);
                WriteQuad(mesh, ref vertex, x0, x1, w, h, _dividerColor, _dividerColor);
            }
        }

        private void WriteQuad(MeshWriteData mesh, ref ushort vertex, float x0, float x1, float w, float h, Color left, Color right)
        {
            mesh.SetNextVertex(new Vertex { position = new Vector3(x0, TopAt(x0, w, h), Vertex.nearZ), tint = left });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x1, TopAt(x1, w, h), Vertex.nearZ), tint = right });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x1, h, Vertex.nearZ), tint = right });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x0, h, Vertex.nearZ), tint = left });

            mesh.SetNextIndex(vertex);
            mesh.SetNextIndex((ushort)(vertex + 1));
            mesh.SetNextIndex((ushort)(vertex + 2));
            mesh.SetNextIndex(vertex);
            mesh.SetNextIndex((ushort)(vertex + 2));
            mesh.SetNextIndex((ushort)(vertex + 3));
            vertex += 4;
        }

        private void DrawOverlay(MeshGenerationContext mgc)
        {
            Rect rect = _overlayLayer.contentRect;
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            Painter2D painter = mgc.painter2D;

            if (_ghostWidth > 0f)
            {
                float x0 = _ghostStart * w;
                float x1 = (_ghostStart + _ghostWidth) * w;
                painter.fillColor = _ghostColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x0, TopAt(x0, w, h)));
                painter.LineTo(new Vector2(x1, TopAt(x1, w, h)));
                painter.LineTo(new Vector2(x1, h));
                painter.LineTo(new Vector2(x0, h));
                painter.ClosePath();
                painter.Fill();
            }

            if (_outlineWidth > 0f)
            {
                float inset = _outlineWidth * 0.5f;
                painter.strokeColor = _outlineColor;
                painter.lineWidth = _outlineWidth;
                painter.lineJoin = LineJoin.Miter;
                painter.BeginPath();
                painter.MoveTo(new Vector2(inset, TopLeft(h) + inset));
                painter.LineTo(new Vector2(w - inset, inset));
                painter.LineTo(new Vector2(w - inset, h - inset));
                painter.LineTo(new Vector2(inset, h - inset));
                painter.ClosePath();
                painter.Stroke();
            }
        }
    }
}
