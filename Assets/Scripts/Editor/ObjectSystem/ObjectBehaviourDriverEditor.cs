using FallGuys.ObjectSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace FallGuys.Editor.ObjectSystem
{
    [CustomEditor(typeof(ObjectBehaviourDriver))]
    [CanEditMultipleObjects]
    public class ObjectBehaviourDriverEditor : UnityEditor.Editor
    {
        private SerializedProperty _netObjProp;
        private SerializedProperty _netTransProp;
        private SerializedProperty _netRbProp;
        private SerializedProperty _netAnimProp;

        private void OnEnable()
        {
            _netObjProp = serializedObject.FindProperty("_needsNetworkObject");
            _netTransProp = serializedObject.FindProperty("_needsNetworkTransform");
            _netRbProp = serializedObject.FindProperty("_needsNetworkRigidbody");
            _netAnimProp = serializedObject.FindProperty("_needsNetworkAnimator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default inspector properly for multi-edit
            DrawPropertiesExcluding(serializedObject, "m_Script");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Smart Network Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Network Components", GUILayout.Height(30)))
            {
                foreach (var t in targets)
                {
                    SetupNetwork((ObjectBehaviourDriver)t);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupNetwork(ObjectBehaviourDriver driver)
        {
            GameObject go = driver.gameObject;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Network Components");
            int group = Undo.GetCurrentGroup();

            // Use the properties from the serializedObject of THIS specific target
            // We use a fresh SerializedObject for each target in the loop to get their specific values
            SerializedObject so = new SerializedObject(driver);
            bool needsNetObj = so.FindProperty("_needsNetworkObject").boolValue;
            bool needsNetTrans = so.FindProperty("_needsNetworkTransform").boolValue;
            bool needsNetRb = so.FindProperty("_needsNetworkRigidbody").boolValue;
            bool needsNetAnim = so.FindProperty("_needsNetworkAnimator").boolValue;

            if (needsNetObj) EnsureComponent<NetworkObject>(go);
            if (needsNetTrans) EnsureComponent<NetworkTransform>(go);
            if (needsNetRb) EnsureComponent<NetworkRigidbody>(go);
            if (needsNetAnim) EnsureComponent<NetworkAnimator>(go);

            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(go);

            Debug.Log($"[ObjectBehaviourDriver] Network setup completed for {go.name}");
        }

        private void EnsureComponent<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
            {
                Undo.AddComponent<T>(go);
            }
        }

        private object GetFieldValue(object obj, string name)
        {
            var field = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj);
        }
    }
}
