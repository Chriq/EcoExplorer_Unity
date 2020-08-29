using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class EditorUtilities : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator Generator = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            if (Generator.AutoUpdate)
            {
                Generator.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            Generator.DrawMapInEditor();
        }
    }
}
