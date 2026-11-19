using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit.Editor
{
    public static class InputReferences
    {
        public static InputActionReference Find(string actionName)
            => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(Actions()))
                .OfType<InputActionReference>()
                .Where(reference => reference.action.name == actionName)
                .OrderBy(reference => MapPriority(reference.action.actionMap.name))
                .FirstOrDefault();

        static InputActionAsset Actions()
        {
            string[] services = AssetDatabase.FindAssets("t:" + nameof(InputServiceSO));
            if (services.Length != 1) return Resources.Load<InputActionAsset>("JamKitInput");
            return AssetDatabase.LoadAssetAtPath<InputServiceSO>(AssetDatabase.GUIDToAssetPath(services[0])).Actions;
        }

        static int MapPriority(string mapName) => mapName switch
        {
            "Gameplay" => 0,
            "UI" => 1,
            _ => 2,
        };
    }
}
