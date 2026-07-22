using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Base for components that bind runtime data to a named element inside a <see cref="UIDocument"/>.
    /// Handles the awkward part: the UIDocument may build its visual tree after this component's
    /// OnEnable, so the initial bind is deferred until the root exists.
    /// </summary>
    public abstract class UIBindingBase : MonoBehaviour
    {
        [Tooltip("UIDocument that hosts the target element. Defaults to a UIDocument on this GameObject.")]
        public UIDocument Document;

        protected VisualElement Root => Document != null ? Document.rootVisualElement : null;

        protected virtual void OnEnable()
        {
            if (Document == null) Document = GetComponent<UIDocument>();
            Subscribe();
            StartCoroutine(BindWhenReady());
        }

        protected virtual void OnDisable() => Unsubscribe();

        bool _warnedUnresolved;

        IEnumerator BindWhenReady()
        {
            // Wait for the UIDocument to construct its tree, then resolve + draw once.
            while (Root == null) yield return null;
            Resolve();
            Apply();
        }

        /// <summary>
        /// Warn once that a named element wasn't found. A [Required] can't guard a string field, so a
        /// typo in the element name would otherwise be a permanently blank HUD with no diagnostic —
        /// and <see cref="Apply"/> retries <see cref="Resolve"/> forever, silently. Call this from a
        /// subclass's Resolve when the tree is built but the query came back null.
        /// </summary>
        protected void WarnUnresolved(string elementName)
        {
            if (_warnedUnresolved) return;
            _warnedUnresolved = true;
            Debug.LogWarning($"[JamKit] {GetType().Name}: no element named '{elementName}' in the UIDocument — this binding shows nothing. " +
                             "Check the name matches the UXML (case-sensitive), or that the right UIDocument is assigned.", this);
        }

        /// <summary>Subscribe to the data source(s). Each change should call <see cref="Apply"/>.</summary>
        protected abstract void Subscribe();
        /// <summary>Unsubscribe from the data source(s).</summary>
        protected abstract void Unsubscribe();
        /// <summary>Locate the target element(s) in <see cref="Root"/>.</summary>
        protected abstract void Resolve();
        /// <summary>Push the current value(s) to the element(s). Must tolerate being called before Resolve.</summary>
        protected abstract void Apply();
    }
}
