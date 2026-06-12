using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Minimal Game Over screen: a title, optional final-score readout, and Retry / Main Menu
    /// buttons wired to the <see cref="SceneServiceSO"/>. Builds itself in code using the bundled
    /// JamKitMenu USS classes, so it matches the menu look without a separate UXML asset.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameOverController : MonoBehaviour
    {
        [Header("Services")]
        public SceneServiceSO SceneService;
        [Tooltip("Optional — when set, shows the final score and best.")]
        public ScoreServiceSO ScoreService;

        [Header("Behaviour")]
        public string RetrySceneName = "Game";
        public string MainMenuSceneName = "Bootstrap";
        public string TitleText = "Game Over";

        UIDocument _doc;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null) _doc.panelSettings = JamKitUI.LoadOrCreateMenuPanelSettings();
            else JamKitUI.ApplyDefaultTheme(_doc.panelSettings);
        }

        void OnEnable()
        {
            var root = _doc.rootVisualElement;
            if (root == null) return;

            var ss = Resources.Load<StyleSheet>("JamKitMenu");
            if (ss != null && !root.styleSheets.Contains(ss)) root.styleSheets.Add(ss);

            root.Clear();
            root.AddToClassList("jk-root");

            var view = new VisualElement();
            view.AddToClassList("jk-view");
            view.AddToClassList("jk-view--start"); // solid background
            root.Add(view);

            var panel = new VisualElement();
            panel.AddToClassList("jk-panel");
            view.Add(panel);

            var title = new Label(TitleText);
            title.AddToClassList("jk-title");
            panel.Add(title);

            if (ScoreService != null)
            {
                var score = new Label($"Score: {ScoreService.Score}    Best: {ScoreService.HighScore}");
                score.AddToClassList("jk-section-title");
                score.style.unityTextAlign = TextAnchor.MiddleCenter;
                panel.Add(score);
            }

            var column = new VisualElement();
            column.AddToClassList("jk-button-column");
            panel.Add(column);

            var retry = new Button(() => SceneService?.LoadAsync(RetrySceneName)) { text = "Retry" };
            retry.AddToClassList("jk-button");
            column.Add(retry);

            var menu = new Button(() => SceneService?.LoadAsync(MainMenuSceneName)) { text = "Main Menu" };
            menu.AddToClassList("jk-button");
            column.Add(menu);

            retry.schedule.Execute(() => retry.Focus());
        }
    }
}
