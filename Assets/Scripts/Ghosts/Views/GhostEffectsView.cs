using System;
using Gameplay.Ghosts.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Ghosts.Views
{
    // Prototype HUD isolated from RhythmHudView; replace generated art without touching gameplay rules.
    public sealed class GhostEffectsView : IDisposable
    {
        private readonly GhostFeatureSettings _settings;
        private readonly GameObject _canvasObject;
        private readonly RawImage _staticImage;
        private readonly Image _sanityFill;
        private readonly Text _sanityLabel;
        private readonly Image _screamerImage;
        private readonly Texture2D _staticTexture;
        private readonly Sprite _screamerSprite;
        private readonly Color32[] _noisePixels;
        private float _noiseTime;
        private float _staticBurstRemaining;
        private float _persistentStaticOpacity;

        public GhostEffectsView(GhostFeatureSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _canvasObject = CreateCanvas();
            Transform root = _canvasObject.transform;

            _staticTexture = new Texture2D(settings.NoiseTextureWidth, settings.NoiseTextureHeight,
                TextureFormat.RGBA32, false)
            {
                name = "Ghost static preview",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            _noisePixels = new Color32[settings.NoiseTextureWidth * settings.NoiseTextureHeight];
            RefreshNoise();
            _staticImage = CreateRawImage("Translucent TV static", root, false);
            _staticImage.texture = _staticTexture;
            _staticImage.color = new Color(1f, 1f, 1f, 0f);

            var barBackground = CreateImage("Sanity bar background", root, false);
            SetAnchoredRect(barBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(330f, 44f));
            barBackground.color = new Color(0.015f, 0.025f, 0.035f, 0.82f);

            var fillObject = CreateImage("Sanity bar fill", barBackground.transform, false);
            SetAnchoredRect(fillObject.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                new Vector2(4f, 4f), new Vector2(-8f, -8f));
            _sanityFill = fillObject;
            _sanityFill.type = Image.Type.Filled;
            _sanityFill.fillMethod = Image.FillMethod.Horizontal;
            _sanityFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _sanityFill.color = new Color(0.15f, 0.85f, 0.53f, 0.95f);

            _sanityLabel = CreateText("Sanity label", barBackground.transform, 22, TextAnchor.MiddleCenter);
            SetAnchoredRect(_sanityLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _sanityLabel.color = Color.white;

            _screamerImage = CreateImage("Screamer preview", root, true);
            SetAnchoredRect(_screamerImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _screamerSprite = CreatePrototypeScreamerSprite();
            _screamerImage.sprite = _screamerSprite;
            _screamerImage.preserveAspect = true;
            _screamerImage.color = Color.white;
            _screamerImage.enabled = false;
        }

        public void SetSanity(float normalized, float current, float maximum)
        {
            float ratio = Mathf.Clamp01(normalized);
            _sanityFill.fillAmount = ratio;
            _sanityFill.color = Color.Lerp(new Color(0.9f, 0.12f, 0.14f),
                new Color(0.15f, 0.85f, 0.53f), ratio);
            _sanityLabel.text = $"CORDURA: {current:0} / {maximum:0}";
        }

        public void SetStaticOpacity(float opacity)
        {
            _persistentStaticOpacity = Mathf.Clamp(opacity, 0f, _settings.MaximumStaticOpacity);
            ApplyStaticOpacity();
        }

        public void TriggerStaticBurst()
        {
            _staticBurstRemaining = 0.45f;
            ApplyStaticOpacity();
        }

        public void SetScreamerVisible(bool visible) => _screamerImage.enabled = visible;

        public void Tick(float deltaTime)
        {
            if (_staticBurstRemaining > 0f)
            {
                _staticBurstRemaining = Mathf.Max(0f, _staticBurstRemaining - deltaTime);
                if (_staticBurstRemaining <= 0f) ApplyStaticOpacity();
            }

            _noiseTime += deltaTime;
            if (_noiseTime < 1f / _settings.NoiseRefreshRate) return;
            _noiseTime = 0f;
            RefreshNoise();
        }

        private void RefreshNoise()
        {
            var random = new System.Random(unchecked(Environment.TickCount * 31 + Time.frameCount));
            for (int i = 0; i < _noisePixels.Length; i++)
            {
                byte value = (byte)random.Next(35, 256);
                _noisePixels[i] = new Color32(value, value, value, 255);
            }
            _staticTexture.SetPixels32(_noisePixels);
            _staticTexture.Apply(false, false);
        }

        private void ApplyStaticOpacity()
        {
            Color color = _staticImage.color;
            color.a = _staticBurstRemaining > 0f
                ? _settings.MaximumStaticOpacity
                : _persistentStaticOpacity;
            _staticImage.color = color;
        }

        private static GameObject CreateCanvas()
        {
            var canvasObject = new GameObject("Ghost Effects HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static RawImage CreateRawImage(string name, Transform parent, bool raycastTarget)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<RawImage>();
            image.raycastTarget = raycastTarget;
            SetAnchoredRect(image.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            return image;
        }

        private static Image CreateImage(string name, Transform parent, bool stretch)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.raycastTarget = false;
            if (stretch)
                SetAnchoredRect(image.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
            return image;
        }

        private static Text CreateText(string name, Transform parent, int size, TextAnchor alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Sprite CreatePrototypeScreamerSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Generated screamer placeholder",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            FillEllipse(pixels, size, 128, 126, 89, 111, new Color32(226, 230, 220, 255));
            FillEllipse(pixels, size, 88, 145, 24, 29, new Color32(115, 0, 18, 255));
            FillEllipse(pixels, size, 168, 145, 24, 29, new Color32(115, 0, 18, 255));
            FillEllipse(pixels, size, 88, 145, 10, 16, new Color32(5, 4, 8, 255));
            FillEllipse(pixels, size, 168, 145, 10, 16, new Color32(5, 4, 8, 255));
            FillEllipse(pixels, size, 128, 76, 31, 40, new Color32(8, 5, 10, 255));
            FillRect(pixels, size, 104, 76, 152, 80, new Color32(235, 230, 210, 255));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void FillEllipse(Color32[] pixels, int size, int cx, int cy, int rx, int ry, Color32 color)
        {
            for (int y = Math.Max(0, cy - ry); y < Math.Min(size, cy + ry); y++)
                for (int x = Math.Max(0, cx - rx); x < Math.Min(size, cx + rx); x++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f) pixels[y * size + x] = color;
                }
        }

        private static void FillRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = Math.Max(0, y0); y < Math.Min(size, y1); y++)
                for (int x = Math.Max(0, x0); x < Math.Min(size, x1); x++)
                    pixels[y * size + x] = color;
        }

        public void Dispose()
        {
            if (_canvasObject != null) UnityEngine.Object.Destroy(_canvasObject);
            if (_staticTexture != null) UnityEngine.Object.Destroy(_staticTexture);
            if (_screamerSprite != null)
            {
                Texture2D texture = _screamerSprite.texture;
                UnityEngine.Object.Destroy(_screamerSprite);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
