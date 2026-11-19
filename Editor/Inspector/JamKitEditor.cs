using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Metz.JamKit.Inspector;
using Ripple;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public abstract class JamKitEditor : UnityEditor.Editor
    {
        public const string EventsName = "jk-inspector-events";
        public const string DebugName = "jk-inspector-debug";

        const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const long RefreshMilliseconds = 100;
        const string NotPlaying = "—";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var events = new Foldout { name = EventsName, text = "Events", value = false, viewDataKey = ViewKey("events") };

            SerializedProperty property = serializedObject.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                var field = new PropertyField(property.Copy());
                if (property.propertyPath == "m_Script")
                {
                    field.SetEnabled(false);
                    root.Add(field);
                    continue;
                }

                FieldInfo info = Field(target.GetType(), property.name);
                if (info != null && IsSignal(info.FieldType)) events.Add(field);
                else root.Add(field);
                if (info != null) ShowWhen(field, info);
            }

            if (events.contentContainer.childCount > 0) root.Add(events);
            VisualElement debug = DebugSection();
            if (debug != null) root.Add(debug);
            return root;
        }

        public static IEnumerable<MethodInfo> Buttons(Type type)
        {
            var seen = new HashSet<(Type, int)>();
            foreach (Type each in Hierarchy(type))
                foreach (MethodInfo method in each.GetMethods(Declared))
                {
                    MethodInfo definition = method.GetBaseDefinition();
                    if (!seen.Add((definition.DeclaringType, definition.MetadataToken))) continue;
                    if (method.IsDefined(typeof(ButtonAttribute), true) && method.GetParameters().Length <= 1) yield return method;
                }
        }

        public static IEnumerable<MemberInfo> LiveValues(Type type)
            => Hierarchy(type)
                .SelectMany(each => each.GetProperties(Declared).Cast<MemberInfo>().Concat(each.GetFields(Declared)))
                .Where(member => member.IsDefined(typeof(ShowInInspectorAttribute), true));

        string ViewKey(string part) => $"{nameof(JamKitEditor)}.{target.GetType().FullName}.{part}";

        void ShowWhen(VisualElement field, FieldInfo info)
        {
            var show = info.GetCustomAttribute<ShowIfAttribute>();
            var hide = info.GetCustomAttribute<HideIfAttribute>();
            string conditionName = show?.Condition ?? hide?.Condition;
            if (conditionName == null) return;

            SerializedProperty condition = serializedObject.FindProperty(conditionName);
            if (condition == null || condition.propertyType != SerializedPropertyType.Boolean) return;

            bool shownWhen = show != null;
            void Apply(SerializedProperty flag) => field.style.display = flag.boolValue == shownWhen ? DisplayStyle.Flex : DisplayStyle.None;
            Apply(condition);
            field.TrackPropertyValue(condition, Apply);
        }

        VisualElement DebugSection()
        {
            List<MemberInfo> values = LiveValues(target.GetType()).ToList();
            List<MethodInfo> buttons = Buttons(target.GetType()).ToList();
            if (values.Count == 0 && buttons.Count == 0) return null;

            var foldout = new Foldout { name = DebugName, text = "Debug", value = false, viewDataKey = ViewKey("debug") };
            var readers = new List<Action>();
            foreach (MemberInfo member in values)
            {
                var field = new TextField(ObjectNames.NicifyVariableName(member.Name)) { isReadOnly = true };
                field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                foldout.Add(field);
                readers.Add(() => field.SetValueWithoutNotify(EditorApplication.isPlaying ? Format(Read(member, target)) : NotPlaying));
            }

            var actions = new List<VisualElement>();
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            foldout.Add(row);
            foreach (MethodInfo method in buttons)
            {
                VisualElement action = Control(method);
                if (action == null) continue;
                actions.Add(action);
                if (method.GetParameters().Length == 0) row.Add(action);
                else foldout.Add(action);
            }

            void Refresh()
            {
                foreach (Action read in readers) read();
                foreach (VisualElement action in actions) action.SetEnabled(EditorApplication.isPlaying);
            }

            Refresh();
            foldout.schedule.Execute(Refresh).Every(RefreshMilliseconds);
            return foldout;
        }

        VisualElement Control(MethodInfo method)
        {
            string label = method.GetCustomAttribute<ButtonAttribute>(true)?.Label ?? ObjectNames.NicifyVariableName(method.Name);
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0) return new Button(() => Invoke(method, null)) { text = label };

            VisualElement input = Input(parameters[0].ParameterType);
            if (input == null) return null;
            input.style.flexGrow = 1f;
            var line = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            line.Add(input);
            line.Add(new Button(() => Invoke(method, new[] { Value(input) })) { text = label });
            return line;
        }

        void Invoke(MethodInfo method, object[] arguments)
        {
            foreach (Object each in targets)
            {
                try
                {
                    method.Invoke(each, arguments);
                }
                catch (TargetInvocationException exception)
                {
                    Debug.LogException(exception.InnerException, each);
                }
            }
        }

        static VisualElement Input(Type type)
        {
            if (type == typeof(string)) return new TextField();
            if (type == typeof(float)) return new FloatField { value = 1f };
            if (type == typeof(int)) return new IntegerField { value = 1 };
            if (type == typeof(bool)) return new Toggle();
            return null;
        }

        static object Value(VisualElement input) => input switch
        {
            TextField text => text.value,
            FloatField number => number.value,
            IntegerField integer => integer.value,
            Toggle toggle => toggle.value,
            _ => null,
        };

        static object Read(MemberInfo member, object owner)
        {
            try
            {
                return member switch
                {
                    PropertyInfo property => property.GetValue(owner),
                    FieldInfo field => field.GetValue(owner),
                    _ => null,
                };
            }
            catch (TargetInvocationException exception)
            {
                return exception.InnerException?.GetType().Name;
            }
        }

        static string Format(object value) => value switch
        {
            null => "None",
            float number => number.ToString("0.###"),
            Object unityObject => unityObject == null ? "None" : unityObject.name,
            _ => value.ToString(),
        };

        static bool IsSignal(Type type)
            => type == typeof(Signal) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Signal<>));

        static FieldInfo Field(Type type, string name)
            => Hierarchy(type).Select(each => each.GetField(name, Declared)).FirstOrDefault(field => field != null);

        static IEnumerable<Type> Hierarchy(Type type)
        {
            for (Type each = type; each != null && each != typeof(MonoBehaviour) && each != typeof(ScriptableObject); each = each.BaseType)
                yield return each;
        }
    }

    [CustomEditor(typeof(ServiceSO), true), CanEditMultipleObjects] sealed class ServiceSOEditor : JamKitEditor { }
    [CustomEditor(typeof(Sound), true), CanEditMultipleObjects] sealed class SoundEditor : JamKitEditor { }
    [CustomEditor(typeof(Music), true), CanEditMultipleObjects] sealed class MusicEditor : JamKitEditor { }
    [CustomEditor(typeof(GameFlow)), CanEditMultipleObjects] sealed class GameFlowEditor : JamKitEditor { }
    [CustomEditor(typeof(AutoDespawn)), CanEditMultipleObjects] sealed class AutoDespawnEditor : JamKitEditor { }
    [CustomEditor(typeof(ChaseMover)), CanEditMultipleObjects] sealed class ChaseMoverEditor : JamKitEditor { }
    [CustomEditor(typeof(Damager)), CanEditMultipleObjects] sealed class DamagerEditor : JamKitEditor { }
    [CustomEditor(typeof(FirstPersonLook)), CanEditMultipleObjects] sealed class FirstPersonLookEditor : JamKitEditor { }
    [CustomEditor(typeof(GameTimer)), CanEditMultipleObjects] sealed class GameTimerEditor : JamKitEditor { }
    [CustomEditor(typeof(Health)), CanEditMultipleObjects] sealed class HealthEditor : JamKitEditor { }
    [CustomEditor(typeof(HealthDrain)), CanEditMultipleObjects] sealed class HealthDrainEditor : JamKitEditor { }
    [CustomEditor(typeof(HighScoreTracker)), CanEditMultipleObjects] sealed class HighScoreTrackerEditor : JamKitEditor { }
    [CustomEditor(typeof(HitStop)), CanEditMultipleObjects] sealed class HitStopEditor : JamKitEditor { }
    [CustomEditor(typeof(Interactable)), CanEditMultipleObjects] sealed class InteractableEditor : JamKitEditor { }
    [CustomEditor(typeof(Interactor)), CanEditMultipleObjects] sealed class InteractorEditor : JamKitEditor { }
    [CustomEditor(typeof(LifecycleSignals)), CanEditMultipleObjects] sealed class LifecycleSignalsEditor : JamKitEditor { }
    [CustomEditor(typeof(Mover2D)), CanEditMultipleObjects] sealed class Mover2DEditor : JamKitEditor { }
    [CustomEditor(typeof(Mover3D)), CanEditMultipleObjects] sealed class Mover3DEditor : JamKitEditor { }
    [CustomEditor(typeof(PatrolMover)), CanEditMultipleObjects] sealed class PatrolMoverEditor : JamKitEditor { }
    [CustomEditor(typeof(ProjectileShooter)), CanEditMultipleObjects] sealed class ProjectileShooterEditor : JamKitEditor { }
    [CustomEditor(typeof(Respawner)), CanEditMultipleObjects] sealed class RespawnerEditor : JamKitEditor { }
    [CustomEditor(typeof(SpawnBurst)), CanEditMultipleObjects] sealed class SpawnBurstEditor : JamKitEditor { }
    [CustomEditor(typeof(Spawner)), CanEditMultipleObjects] sealed class SpawnerEditor : JamKitEditor { }
    [CustomEditor(typeof(TriggerZone)), CanEditMultipleObjects] sealed class TriggerZoneEditor : JamKitEditor { }
    [CustomEditor(typeof(CameraShake)), CanEditMultipleObjects] sealed class CameraShakeEditor : JamKitEditor { }
    [CustomEditor(typeof(ColorFlash)), CanEditMultipleObjects] sealed class ColorFlashEditor : JamKitEditor { }
    [CustomEditor(typeof(PunchScale)), CanEditMultipleObjects] sealed class PunchScaleEditor : JamKitEditor { }
    [CustomEditor(typeof(MixerVolume)), CanEditMultipleObjects] sealed class MixerVolumeEditor : JamKitEditor { }
    [CustomEditor(typeof(SceneLoader)), CanEditMultipleObjects] sealed class SceneLoaderEditor : JamKitEditor { }
    [CustomEditor(typeof(GameFlowDriver)), CanEditMultipleObjects] sealed class GameFlowDriverEditor : JamKitEditor { }
    [CustomEditor(typeof(DebugPanel)), CanEditMultipleObjects] sealed class DebugPanelEditor : JamKitEditor { }
    [CustomEditor(typeof(MenuController)), CanEditMultipleObjects] sealed class MenuControllerEditor : JamKitEditor { }
    [CustomEditor(typeof(MenuView)), CanEditMultipleObjects] sealed class MenuViewEditor : JamKitEditor { }
    [CustomEditor(typeof(Toast)), CanEditMultipleObjects] sealed class ToastEditor : JamKitEditor { }
    [CustomEditor(typeof(DisplaySettings)), CanEditMultipleObjects] sealed class DisplaySettingsEditor : JamKitEditor { }
    [CustomEditor(typeof(PopupText)), CanEditMultipleObjects] sealed class PopupTextEditor : JamKitEditor { }
}
