using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GTGrass
{
    [CustomEditor(typeof(GTGrassPainter))]
    [CanEditMultipleObjects]
    public class GrassPainterEditor : Editor
    {
        GTGrassPainter grassPainter;
        readonly string[] toolbarStrings = { "Add", "Remove", "Edit" };

        SerializedProperty grassLimit;
        SerializedProperty toolBarInt;
        SerializedProperty hitMask;
        SerializedProperty paintMask;
        SerializedProperty brushSize;
        SerializedProperty density;
        SerializedProperty normalLimit;
        SerializedProperty roughness;
        SerializedProperty sizeWidth;
        SerializedProperty sizeLength;
        SerializedProperty adjustedColor;
        SerializedProperty rangeR;
        SerializedProperty rangeG;
        SerializedProperty rangeB;

        SerializedProperty grassData;

        string fileName = "NewGrassData";
        string dataFolderPath = "Assets/YanArt/GTGrass/Examples/Data/";

        private void OnEnable()
        {
            grassPainter = (GTGrassPainter)target;

            grassLimit = serializedObject.FindProperty("grassLimit");
            toolBarInt = serializedObject.FindProperty("toolbarInt");
            hitMask = serializedObject.FindProperty("hitMask");
            paintMask = serializedObject.FindProperty("paintMask");
            brushSize = serializedObject.FindProperty("brushSize");
            density = serializedObject.FindProperty("density");
            normalLimit = serializedObject.FindProperty("normalLimit");
            roughness = serializedObject.FindProperty("roughness");
            sizeWidth = serializedObject.FindProperty("sizeWidth");
            sizeLength = serializedObject.FindProperty("sizeLength");
            adjustedColor = serializedObject.FindProperty("AdjustedColor");
            rangeR = serializedObject.FindProperty("rangeR");
            rangeG = serializedObject.FindProperty("rangeG");
            rangeB = serializedObject.FindProperty("rangeB");

            grassData = serializedObject.FindProperty("grassData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Grass Limit", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(grassPainter.vertexCount + "/", EditorStyles.label, GUILayout.Width(100));
            EditorGUILayout.IntSlider(grassLimit, 7, 15000, "");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Paint Options", EditorStyles.boldLabel);
            toolBarInt.intValue = GUILayout.Toolbar(toolBarInt.intValue, toolbarStrings, GUILayout.Height(40));
            EditorGUILayout.Space();

            if (GUILayout.Button("Clear Mesh"))
            {
                if (EditorUtility.DisplayDialog("Clear Painted Mesh?",
                   "Are you sure you want to clear the mesh?", "Clear", "Don't Clear"))
                {
                    var linkedObject = serializedObject.targetObject as GTGrassPainter;
                    linkedObject.ClearMesh();
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
            LayerMask tempMask = EditorGUILayout.MaskField("Hit Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(hitMask.intValue), InternalEditorUtility.layers);
            hitMask.intValue = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            LayerMask tempMask2 = EditorGUILayout.MaskField("Painting Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(paintMask.intValue), InternalEditorUtility.layers);
            paintMask.intValue = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);

            EditorGUILayout.Slider(brushSize, 0.1f, 10f, "Brush Size");
            EditorGUILayout.IntSlider(density, 1, 7, "Density");
            EditorGUILayout.Slider(normalLimit, 0.01f, 1f, "Normal Limit");

            var typeRect = GUILayoutUtility.GetLastRect();
            GUI.Label(typeRect, new GUIContent("", "Limit the surface normal. 0 means you can only draw on surface with normal (0,1,0), 1 means you can draw on any surface"));
            EditorGUILayout.Slider(roughness, 0.01f, 1f, "Roughness Limit");

            var typeRect1 = GUILayoutUtility.GetLastRect();
            GUI.Label(typeRect1, new GUIContent("", "Limit the surface roughness. The greater the roughness is, the more uneven surface you can draw on"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
            EditorGUILayout.Slider(sizeWidth, 0f, 2f, "Grass Width");
            EditorGUILayout.Slider(sizeLength, 0f, 2f, "Grass Height");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            adjustedColor.colorValue = EditorGUILayout.ColorField("Main Color", adjustedColor.colorValue);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
            EditorGUILayout.Slider(rangeR, 0f, 1f, "Red");
            EditorGUILayout.Slider(rangeG, 0f, 1f, "Green");
            EditorGUILayout.Slider(rangeB, 0f, 1f, "Blue");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(grassData);

            if (!grassPainter.IsDataInitialized())
            {
                fileName = EditorGUILayout.TextField("Data Asset Name", fileName);
                dataFolderPath = EditorGUILayout.TextField("Data Folder Path", dataFolderPath);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("LOAD", GUILayout.Height(40), GUILayout.Width((EditorGUIUtility.currentViewWidth - 40) * 0.5f)))
            {
                if (EditorUtility.DisplayDialog("Load Grass Data?",
                   "Are you sure you want to load grass data from file " + grassData.name + "?", "Load", "Don't Load"))
                {
                    grassPainter.LoadData();
                }
            }

            if (!grassPainter.IsDataInitialized())
            {
                GUI.backgroundColor = Color.green;

                if (GUILayout.Button("INITIALIZE", GUILayout.Height(40), GUILayout.Width((EditorGUIUtility.currentViewWidth - 40) * 0.5f)))
                {
                    if (EditorUtility.DisplayDialog("Create new Grass Data?",
                       $"Are you sure you want to create a new grass data at {dataFolderPath}{fileName}.asset?", "Create", "Don't Create"))
                    {
                        grassPainter.CreateNewGrassData(fileName, dataFolderPath);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("SAVE COPY", GUILayout.Height(40), GUILayout.Width((EditorGUIUtility.currentViewWidth - 40) * 0.5f)))
                {
                    if (EditorUtility.DisplayDialog("Save Grass Data?",
                       "Are you sure you want to save a copy of the grass data?", "Save Copy", "Don't Save"))
                    {
                        grassPainter.SaveCopy();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

