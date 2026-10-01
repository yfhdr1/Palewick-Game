using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.PostProcessing;
namespace UnityEditor.PostProcessing
{
    public class PostProcessingModelEditor
    {
        public PostProcessingModel target { get; internal set; }
        public SerializedProperty serializedProperty { get; internal set; }
        protected SerializedProperty m_SettingsProperty;
        protected SerializedProperty m_EnabledProperty;
        internal bool alwaysEnabled = false;
        internal PostProcessingProfile profile;
        internal PostProcessingInspector inspector;
        internal void OnPreEnable()
        {
            if (serializedProperty == null)
                return;
            m_SettingsProperty = serializedProperty.FindPropertyRelative("m_Settings");
            m_EnabledProperty = serializedProperty.FindPropertyRelative("m_Enabled");
            OnEnable();
        }
        public virtual void OnEnable()
        {
        }
        public virtual void OnDisable()
        {
        }
        internal void OnGUI()
        {
            if (serializedProperty == null || m_SettingsProperty == null || m_EnabledProperty == null)
                return;
            GUILayout.Space(5f);
            var display = alwaysEnabled ? EditorGUIHelper.Header(serializedProperty.displayName, m_SettingsProperty, Reset) : EditorGUIHelper.Header(serializedProperty.displayName, m_SettingsProperty, m_EnabledProperty, Reset);
            if (!display)
                return;
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledGroupScope(!m_EnabledProperty.boolValue))
            {
                OnInspectorGUI();
            }
            EditorGUI.indentLevel--;
        }
        void Reset()
        {
            if (serializedProperty == null || target == null)
                return;
            var obj = serializedProperty.serializedObject;
            Undo.RecordObject(obj.targetObject, "Reset");
            target.Reset();
            EditorUtility.SetDirty(obj.targetObject);
        }
        public virtual void OnInspectorGUI()
        {
        }
        public void Repaint()
        {
            if (inspector != null)
                inspector.Repaint();
        }
        protected SerializedProperty FindSetting<T, TValue>(Expression<Func<T, TValue>> expr)
        {
            if (m_SettingsProperty == null)
                return null;
            return m_SettingsProperty.FindPropertyRelative(ReflectionUtils.GetFieldPath(expr));
        }
        protected SerializedProperty FindSetting<T, TValue>(SerializedProperty prop, Expression<Func<T, TValue>> expr)
        {
            if (prop == null)
                return null;
            return prop.FindPropertyRelative(ReflectionUtils.GetFieldPath(expr));
        }
    }
}
