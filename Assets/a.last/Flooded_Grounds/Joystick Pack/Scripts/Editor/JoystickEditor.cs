using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Joystick), true)]
public class JoystickEditor : Editor
{
    private SerializedProperty handleRange;
    private SerializedProperty deadZone;
    private SerializedProperty background;
    private SerializedProperty handle;
    protected virtual void OnEnable()
    {
        handleRange = serializedObject.FindProperty("handleRange");
        deadZone = serializedObject.FindProperty("deadZone");
        background = serializedObject.FindProperty("background");
        handle = serializedObject.FindProperty("handle");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (handleRange != null) EditorGUILayout.PropertyField(handleRange);
        if (deadZone != null) EditorGUILayout.PropertyField(deadZone);
        if (background != null) EditorGUILayout.PropertyField(background);
        if (handle != null) EditorGUILayout.PropertyField(handle);
        serializedObject.ApplyModifiedProperties();
    }
}