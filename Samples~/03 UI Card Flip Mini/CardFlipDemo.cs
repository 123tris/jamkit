using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 03 — UI Card Flip Mini (UI Toolkit). Click any of nine cards to flip it (scale.x
    /// tween, swap face color halfway). Each flip increments a counter; the best run persists
    /// via the assigned <see cref="SaveServiceSO"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CardFlipDemo : MonoBehaviour
    {
        const string ScoreKey = "Samples.CardFlip.HighScore";

        [Tooltip("SaveServiceSO used to persist the high score across runs. Required.")]
        public SaveServiceSO SaveService;

        UIDocument _doc;
        Label _scoreLabel;
        readonly List<VisualElement> _cards = new();
        int _score;
        int _highScore;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null)
                _doc.panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, sortingOrder: 0);
        }

        void OnEnable()
        {
            BuildHierarchy();
            _highScore = SaveService != null ? SaveService.Read(ScoreKey, 0) : 0;
            RefreshScore();
        }

        void BuildHierarchy()
        {
            var root = _doc.rootVisualElement;
            root.Clear();
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.flexGrow = 1f;

            _scoreLabel = new Label("Flips: 0    Best: 0")
            {
                style =
                {
                    fontSize = 42,
                    color = new StyleColor(Color.white),
                    marginBottom = 32,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            root.Add(_scoreLabel);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.width = 660;
            grid.style.justifyContent = Justify.Center;
            root.Add(grid);

            _cards.Clear();
            for (int i = 0; i < 9; i++)
            {
                var card = new VisualElement();
                card.style.width = 180; card.style.height = 180;
                card.style.marginLeft = 12; card.style.marginRight = 12;
                card.style.marginTop = 12; card.style.marginBottom = 12;
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 12;
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 12;
                card.style.backgroundColor = new Color(0.3f, 0.4f, 0.6f);
                card.RegisterCallback<ClickEvent>(_ => Flip(card));
                grid.Add(card);
                _cards.Add(card);
            }
        }

        void Flip(VisualElement card)
        {
            _score++;
            if (_score > _highScore)
            {
                _highScore = _score;
                if (SaveService != null) SaveService.Write(ScoreKey, _highScore);
            }
            RefreshScore();
            StartCoroutine(FlipRoutine(card));
        }

        IEnumerator FlipRoutine(VisualElement card)
        {
            // Squish to a thin sliver.
            yield return LerpScale(card, 1f, 0f, 0.18f);
            card.style.backgroundColor = new Color(Random.value, Random.value, Random.value);
            // Snap back.
            yield return LerpScale(card, 0f, 1f, 0.22f);
        }

        static IEnumerator LerpScale(VisualElement el, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(from, to, p);
                el.style.scale = new Scale(new Vector3(v, 1f, 1f));
                yield return null;
            }
            el.style.scale = new Scale(new Vector3(to, 1f, 1f));
        }

        void RefreshScore()
        {
            if (_scoreLabel != null) _scoreLabel.text = $"Flips: {_score}    Best: {_highScore}";
        }
    }
}
