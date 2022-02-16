using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class WatchObjects : EditorWindow
{
    #region Props and fields
    [Header("Add objects to watch")]
    public GameObject[] GameObjects;
    public static WatchObjects Instance;

    private SerializedObject so;
    private List<object> fieldList = new List<object>();
    private List<bool> showHeaderGroups = new List<bool>();
    #endregion

    #region Editor Initialization
    [MenuItem("Window/Watch Objects")]
    public static void ShowWindow()
    {
        Instance = GetWindow<WatchObjects>();
    }

    private void OnEnable()
    {
        ScriptableObject target = this;
        so = new SerializedObject(target);
    }
    #endregion

    #region GUI Render
    void OnGUI()
    {
        so.Update();
        SerializedProperty stringsProperty = so.FindProperty("GameObjects");

        EditorGUILayout.PropertyField(stringsProperty, true); // True means show children
        so.ApplyModifiedProperties(); // Remember to apply modified properties

        if (GameObjects == null || GameObjects.Length == 0)
        {
            return;
        }

        if (GameObjects.Length > showHeaderGroups.Count)
        {
            showHeaderGroups.Add(true);
        }
        else if (GameObjects.Length < showHeaderGroups.Count)
        {
            showHeaderGroups.RemoveAt(showHeaderGroups.Count - 1);
        }

        ProcessGameObjects();
    }

    private void Update()
    {
        Repaint();
    }
    #endregion

    #region Private Methods
    private void ProcessGameObjects()
    {
        EditorGUIUtility.wideMode = true;
        for (int i = 0; i < GameObjects.Length; i++)
        {
            try
            {
                var showPosition = true;
                var g = GameObjects[i];
                if (g == null) continue;

                var groupName = $"{g.GetType().ToString()} - {g.name}";
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                showHeaderGroups[i] = EditorGUILayout.BeginFoldoutHeaderGroup(showHeaderGroups[i], groupName);
                EditorGUILayout.EndHorizontal();

                if (showHeaderGroups[i])
                {
                    var scripts = g.GetComponents<MonoBehaviour>();
                    if (scripts != null)
                    {
                        foreach (var s in scripts)
                        {
                            var param = s.GetType().GetCustomAttribute<WatchedScript>();
                            if (param == null)
                            {
                                continue;
                            }

                            showPosition = param.ShowPosition;

                            if (showPosition)
                            {
                                EditorGUILayout.Vector3Field("Position", g.transform.position);
                            }


                            DisplayProperties(s);
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space(10f, true);
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"Exception Rendering {GameObjects[i].name} {ex.Message}");
            }
        }
    }

    private void DisplayProperties(MonoBehaviour script)
    {
        FieldInfo[] fi = script.GetType().GetFields();
        foreach (var info in fi)
        {
            // Process watched properties
            var pr = info.GetCustomAttribute<WatchedProperty>();
            if (pr != null)
            {

                var name = info.Name;
                var val = info.GetValue(script);

                var style = new GUIStyle(GUI.skin.textField);

                // Set text color
                if (pr.TextColor != null)
                {
                    style.normal.textColor = GetColorFromString(pr.TextColor);
                }

                ProcessBackgroundColor(pr, val, style);

                val = EditorGUILayout.TextField($"{name}", $"{val}", style);
                fieldList.Add(val);
            }
        }

        var mi = script.GetType().GetMethods();
        if (mi == null || mi.Length == 0) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Custom Actions");

        foreach (var m in mi)
        {
            // Process executable actions
            var executable = m.GetCustomAttribute<ExecutableAction>();
            if (executable != null)
            {
                var options = new GUILayoutOption[0];
                if (EditorGUILayout.LinkButton(executable.ActionName, options))
                {
                    m.Invoke(script, null);
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private Color GetColorFromString(string textColor, float alpha = 1f)
    {

        if (string.IsNullOrEmpty(textColor))
        {
            return BuildNewColor(Color.white, alpha);
        }

        textColor = textColor.ToLower();
        switch (textColor)
        {
            case "yellow":
                return BuildNewColor(Color.yellow, alpha);
            case "clear":
                return BuildNewColor(Color.clear, alpha);
            case "grey":
                return BuildNewColor(Color.grey, alpha);
            case "gray":
                return BuildNewColor(Color.gray, alpha);
            case "magenta":
                return BuildNewColor(Color.magenta, alpha);
            case "cyan":
                return BuildNewColor(Color.cyan, alpha);
            case "red":
                return BuildNewColor(Color.red, alpha);
            case "black":
                return BuildNewColor(Color.black, alpha);
            case "white":
                return BuildNewColor(Color.white, alpha);
            case "blue":
                return BuildNewColor(Color.blue, alpha);
            case "green":
                return BuildNewColor(Color.green, alpha);
            default:
                return BuildNewColor(Color.white, alpha);
        }
    }

    private Color BuildNewColor(Color col, float alpha)
    {
        return new Color(col.r, col.g, col.b, alpha);
    }

    private Color BuildNewColor(string color, float alpha)
    {
        var c = GetColorFromString(color);
        return BuildNewColor(c, alpha);
    }

    private void ProcessBackgroundColor(WatchedProperty pr, object val, GUIStyle style)
    {
        // Ignore background overrides and process warning ranges
        if (pr.WarningRangeEnd != 0 || pr.WarningRangeStart != 0)
        {
            if (val.GetType() == typeof(System.Int32) || val.GetType() == typeof(float))
            {
                float fVal = 0;
                float.TryParse(val?.ToString(), out fVal);

                // Mark as warning
                if (pr.WarningRangeStart <= fVal && fVal <= pr.WarningRangeEnd)
                {
                    style.normal.background = MakeTex(600, 1, BuildNewColor(Color.red, 0.2f));
                }
                else
                {
                    style.normal.background = MakeTex(600, 1, BuildNewColor(Color.green, 0.2f));
                }
            }
        }

        // Set background color
        else if (pr.BackgroundColor != null)
        {
            if (pr.BackgroundColor != null)
            {
                // Background color override requested
                style.normal.background = MakeTex(600, 1, BuildNewColor(pr.BackgroundColor, 0.2f));
                style.richText = true;
            }
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
    #endregion

}
