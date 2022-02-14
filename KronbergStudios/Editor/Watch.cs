using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class WatchObjects : EditorWindow
{
    [Header("Add objects to watch")]
    public GameObject[] GameObjects;
    public static WatchObjects Instance;

    private SerializedObject so;
    private List<object> fieldList = new List<object>();
    private List<bool> showHeaderGroups = new List<bool>();

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

    /// <summary>
    /// Process Game Objects
    /// </summary>
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
                showHeaderGroups[i] = EditorGUILayout.BeginFoldoutHeaderGroup(showHeaderGroups[i], groupName);

                if (showHeaderGroups[i])
                {
                    if (showPosition)
                    {
                        EditorGUILayout.Vector3Field("Position", g.transform.position);
                    }

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
                            DisplayProperties(s);
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space(10f);
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
                if (pr.A != -1 || pr.R != -1 || pr.G != -1 || pr.B != -1)
                {
                    // Background color override requested
                    style.normal.background = MakeTex(600, 1, new Color(pr.R, pr.G, pr.B, pr.A));
                }

                val = EditorGUILayout.TextField(name, $"{val}", style);
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

    void OnHierarchyChange()
    {

    }


    private void Update()
    {
        Repaint();
    }
}
