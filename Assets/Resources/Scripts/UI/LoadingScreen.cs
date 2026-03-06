using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts.UI
{
    /// <summary>
    /// Self-building loading screen Canvas shown during world initialisation.
    /// Call <see cref="SetStatus"/> and <see cref="SetProgress"/> each frame to
    /// reflect the current stage, then call <see cref="Hide"/> when loading is done.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float ReferenceWidth  = 1920f;
        private const float ReferenceHeight = 1080f;

        // ── UI references ─────────────────────────────────────────────────────
        private Canvas          _canvas;
        private TextMeshProUGUI _statusText;
        private Image           _progressFill;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake() => BuildUI();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Updates the descriptive status line (e.g. "Generating chunks 3/125").</summary>
        public void SetStatus(string status)
        {
            if (_statusText != null)
                _statusText.text = status;
        }

        /// <summary>Updates the progress bar. <paramref name="progress"/> is clamped to [0, 1].</summary>
        public void SetProgress(float progress)
        {
            if (_progressFill != null)
                _progressFill.fillAmount = Mathf.Clamp01(progress);
        }

        public void Hide() { if (_canvas) _canvas.gameObject.SetActive(false); }
        public void Show() { if (_canvas) _canvas.gameObject.SetActive(true);  }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            // ── Canvas root ──────────────────────────────────────────────────
            var canvasGO = new GameObject("LoadingCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Background panel ─────────────────────────────────────────────
            CreateStretchedImage(canvasGO.transform, "Background", new Color(0.05f, 0.05f, 0.08f, 1f));

            // ── Title: "Creating World" ──────────────────────────────────────
            var title = CreateText(
                parent:    canvasGO.transform,
                name:      "Title",
                text:      "Creating World",
                anchorY:   0.60f,
                fontSize:  64f,
                color:     Color.white,
                height:    90f);
            title.fontStyle = FontStyles.Bold;

            // ── Status text ──────────────────────────────────────────────────
            _statusText = CreateText(
                parent:    canvasGO.transform,
                name:      "Status",
                text:      "",
                anchorY:   0.45f,
                fontSize:  26f,
                color:     new Color(0.72f, 0.72f, 0.72f),
                height:    50f);

            // ── Progress bar ─────────────────────────────────────────────────
            _progressFill = CreateProgressBar(canvasGO.transform, anchorY: 0.37f);
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string text,
            float anchorY, float fontSize, Color color, float height)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin       = new Vector2(0.5f, anchorY);
            rect.anchorMax       = new Vector2(0.5f, anchorY);
            rect.pivot           = new Vector2(0.5f, 0.5f);
            rect.sizeDelta       = new Vector2(700f, height);
            rect.anchoredPosition = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;

            return tmp;
        }

        private static Image CreateProgressBar(Transform parent, float anchorY)
        {
            // Track (background)
            var track = new GameObject("ProgressBarTrack");
            track.transform.SetParent(parent, false);

            var trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin       = new Vector2(0.5f, anchorY);
            trackRect.anchorMax       = new Vector2(0.5f, anchorY);
            trackRect.pivot           = new Vector2(0.5f, 0.5f);
            trackRect.sizeDelta       = new Vector2(560f, 18f);
            trackRect.anchoredPosition = Vector2.zero;

            var trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            // Fill
            var fill = new GameObject("ProgressBarFill");
            fill.transform.SetParent(track.transform, false);

            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImg = fill.AddComponent<Image>();
            fillImg.color      = new Color(0.20f, 0.58f, 1.00f, 1f);
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            return fillImg;
        }

        private static void CreateStretchedImage(Transform parent, string name, Color color)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.AddComponent<Image>().color = color;
        }
    }
}
