using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    public abstract class UIBindingBase : MonoBehaviour
    {
        [Required] public UIDocument Document;

        void Reset() => Document = GetComponent<UIDocument>();

        void OnEnable() => StartCoroutine(BindWhenReady());

        IEnumerator BindWhenReady()
        {
            while (Document.rootVisualElement == null) yield return null;
            Bind(Document.rootVisualElement);
        }

        protected abstract void Bind(VisualElement root);
    }
}
