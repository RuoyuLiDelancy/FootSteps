using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace GTGrass
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class GTGrassPainter : MonoBehaviour
    {
        [Range(7, 10000)]
        public int grassLimit = 5000;
        public int toolbarInt = 0;
        public bool painting;
        public bool removing;
        public bool editing;
        public int vertexCount = 0;
        public float sizeWidth = 0.02f;
        public float sizeLength = 1f;
        public int density = 1;
        public float normalLimit = 1;
        public float roughness = 0.5f;
        public float rangeR, rangeG, rangeB;
        public LayerMask hitMask = 1;
        public LayerMask paintMask = 1;
        public float brushSize = 1;
        public Color AdjustedColor = Color.green;
        [SerializeField] private Mesh mesh;
        [SerializeField] GrassData grassData;

        [HideInInspector]
        public Vector3 hitPosGizmo;

        [HideInInspector]
        public Vector3 hitNormal;

        List<Vector3> positions = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> length = new List<Vector2>();
        List<Cell> cellList = new List<Cell>();
        Dictionary<Vector3, Vector3> hitList = new Dictionary<Vector3, Vector3>();
        Vector3 mousePos;
        Vector3 lastPosition = Vector3.zero;
        MeshFilter filter;

#if UNITY_EDITOR
        void OnFocus()
        {
            // Remove delegate listener if it has previously
            // been assigned.
            SceneView.duringSceneGui -= this.OnScene;
            // Add (or re-add) the delegate.
            SceneView.duringSceneGui += this.OnScene;
        }

        private void OnDestroy()
        {
            // When the window is destroyed, remove the delegate
            // so that it will no longer do any drawing.
            SceneView.duringSceneGui -= this.OnScene;
            EditorSceneManager.sceneSaving -= this.SaveData;
            EditorSceneManager.sceneOpened -= this.LoadData;
            EditorApplication.playModeStateChanged -= ModeChanged;
        }

        private void OnEnable()
        {
            filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh != null)
            {
                filter.sharedMesh.RecalculateNormals();
                filter.sharedMesh.RecalculateTangents();
            }

            mesh = new Mesh();
            mesh.name = "Temp";
            SceneView.duringSceneGui += this.OnScene;
            EditorSceneManager.sceneSaving += this.SaveData;
            EditorSceneManager.sceneOpened += this.LoadData;
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        void ModeChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
                SaveData();
        }

        public void ClearMesh()
        {
            Undo.RecordObject(this, "Clear Mesh");
            vertexCount = 0;
            positions = new List<Vector3>();
            triangles = new List<int>();
            colors = new List<Color>();
            normals = new List<Vector3>();
            length = new List<Vector2>();
            cellList = new List<Cell>();
            hitList = new Dictionary<Vector3, Vector3>();

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            WriteMeshData();
        }

        #region [Core Grass Editing]
        void OnScene(SceneView scene)
        {
            // only allow painting while this object is selected
            if (this == null || filter.sharedMesh == null || EditorApplication.isPlaying)
            {
                return;
            }

            if (cellList.Count == 0 && filter.sharedMesh.vertexCount > 7)
            {
                Debug.LogWarning($"[GameObject : {gameObject.name}] Data was lost or not loaded successfully! Loading Grass Data again!", gameObject);
                LoadData();
            }

            if ((Selection.Contains(gameObject)))
            {
                DrawGrassGizmos();
                int controlID = GUIUtility.GetControlID(FocusType.Passive);

                Event e = Event.current;

                mousePos = e.mousePosition;
                float ppp = EditorGUIUtility.pixelsPerPoint;
                mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
                mousePos.x *= ppp;

                // ray for gizmo(disc)
                Ray rayGizmo = scene.camera.ScreenPointToRay(mousePos);
                RaycastHit hitGizmo;

                if (Physics.Raycast(rayGizmo, out hitGizmo, 200f, hitMask.value))
                {
                    hitPosGizmo = hitGizmo.point;
                    hitNormal = hitGizmo.normal;
                }

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.alt && e.button == 0 && toolbarInt == 0)
                {
                    AddGrass(scene.camera);
                    e.Use();
                }

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.alt && e.button == 0 && toolbarInt == 1)
                {
                    RemoveGrass(scene.camera);
                    e.Use();
                }

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.alt && e.button == 0 && toolbarInt == 2)
                {
                    EditGrass(scene.camera);
                    e.Use();
                }

                if (Event.current.type == EventType.Layout)
                    HandleUtility.AddDefaultControl(controlID);
            }
        }

        void AddGrass(Camera camera)
        {
            RaycastHit terrainHit;
            float r = brushSize * (density < 3 ? 0.5f : 0.333f);
            Ray ray = camera.ScreenPointToRay(mousePos);
            Vector3 V = ray.direction.normalized;

            float randomRadius = Random.Range(0f, 360f);

            for (int k = 0; k < density; k++)
            {
                int initialIndex = vertexCount;
                int cellVertMax = 7;

                float centerOffsetLength = (density % 2 == 1) ? r : r * 0.5f * Mathf.Sqrt(3);
                float centerOffsetAngle = 180 + (density - 1) * 30;
                Vector3 cellCenterOffset = density > 1 && density < 6 ? PolarToCartesian(new Vector2(centerOffsetAngle, centerOffsetLength), hitNormal) : Vector3.zero;

                //Calculate the center offset of the cell
                //if we are drawing the middle cell, vertex max is 7. Otherwise, we only need to draw the left vertices.
                if (k != 0)
                    cellCenterOffset += PolarToCartesian(new Vector2(60 * (k - 1) + randomRadius, r * Mathf.Sqrt(3) * Random.Range(1, 1.5f)), hitNormal);

                Vector3 centerPos = Vector3.zero;
                Vector3 centerNormal = Vector3.zero;
                bool isPlaceable = true;
                hitList.Clear();
                var vertexList = new List<VertexData>();

                for (int j = 0; j < cellVertMax; j++)
                {
                    Vector3 originOffset;
                    // Calculate each vertex offset if this is the middle cell
                    Vector3 offset = PolarToCartesian(new Vector2(60 * (j - 1), r), hitNormal);
                    originOffset = j == 0 ? cellCenterOffset : cellCenterOffset + offset;

                    var origin = ray.origin + originOffset;
                    var direction = ray.direction;

                    if (Physics.Raycast(origin, direction, out terrainHit, 400f, hitMask.value) && vertexCount < grassLimit && terrainHit.normal.y <= 1 && terrainHit.normal.y >= (1 - 2 * normalLimit))
                    {
                        // Debug.Log("RaycastHit!");
                        if ((paintMask.value & (1 << terrainHit.transform.gameObject.layer)) > 0)
                        {
                            Vector3 pos = terrainHit.point;

                            //to not place everything at once, check if the first placed point far enough away from the last placed first one
                            if (k == 0 && j == 0)
                            {
                                //i > 0 allows you to draw when there is no vertex
                                float distance = density == 1 ? brushSize * 0.6f : brushSize * 1.2f;
                                if (Vector3.Distance(pos, lastPosition) <= distance)
                                    return;
                                else
                                    lastPosition = pos;
                            }

                            if (j == 0)
                            {
                                centerPos = pos;
                                centerNormal = terrainHit.normal.normalized;
                                hitList.Add(pos, terrainHit.normal);
                            }
                            else
                            {
                                Vector3 planeN = Vector3.Cross(offset.normalized, V).normalized;
                                Vector3 Nproj = centerNormal - Vector3.Dot(planeN, centerNormal) * planeN;
                                Nproj = Nproj.normalized;
                                float OffsetdotN = Vector3.Dot(offset, Nproj);
                                float NdotDir = Mathf.Abs(Vector3.Dot(Nproj, V));

                                float distanceReference = (offset - OffsetdotN * Nproj).magnitude + Mathf.Abs(OffsetdotN) * Mathf.Tan(Mathf.Acos(NdotDir));
                                float distanceToCenter = Vector3.Distance(pos, centerPos);
                                // Debug.Log(distanceToCenter + " | " + distanceReference);
                                if (distanceToCenter > distanceReference - roughness & distanceToCenter < distanceReference + roughness)
                                    hitList.Add(pos, terrainHit.normal);
                                else
                                    isPlaceable = false;
                            }
                        }
                    }
                    else
                    {
                        isPlaceable = false;
                    }
                }

                //Detect if we can place all of the vertices
                if (isPlaceable)
                {
                    Cell cell = new Cell(centerPos - transform.position, vertexList);
                    cellList.Add(cell);

                    foreach (var hit in hitList)
                        AddPoint(hit.Key, hit.Value, vertexList);

                    AddTriangles(initialIndex, vertexList);
                }
            }
            WriteMeshData();
        }

        void RemoveGrass(Camera camera)
        {
            RaycastHit terrainHit;
            Ray ray = camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out terrainHit, 400f, hitMask.value))
            {
                List<VertexData> vertToRemove = new List<VertexData>();
                List<int> triangleIndxToRemove = new List<int>();
                List<Cell> cellToRemove = new List<Cell>();
                foreach (var cell in cellList)
                {

                    Vector3 pos = cell.pos;
                    pos += transform.position;
                    float dist = Vector3.Distance(terrainHit.point, pos);

                    // if its within the radius of the brush, remove all info
                    if (dist <= brushSize)
                    {
                        cellToRemove.Add(cell);
                        for (int i = 0; i < cell.vertices.Count; i++)
                        {
                            vertToRemove.Add(cell.vertices[i]);
                        }
                    }
                    else
                    {
                        for (int i = 1; i < cell.vertices.Count; i++)
                        {
                            float distance = Vector3.Distance(terrainHit.point, cell.vertices[i].pos + transform.position);

                            if (distance <= brushSize)
                            {
                                //Cache vertices index to remove
                                vertToRemove.Add(cell.vertices[i]);
                            }
                        }
                    }
                }

                //Sort vertToRemove by the index
                vertToRemove.Sort();

                //Remove vertices 
                for (int j = vertToRemove.Count - 1; j >= 0; j--)
                {
                    VertexData vert = vertToRemove[j];
                    int vertIndx = vert.index;
                    positions.RemoveAt(vertIndx);
                    colors.RemoveAt(vertIndx);
                    normals.RemoveAt(vertIndx);
                    length.RemoveAt(vertIndx);
                    vertexCount--;

                    //Cache triangles index to remove
                    for (int k = 0; k < vert.triangleIndxList.Count; k++)
                        if (!triangleIndxToRemove.Contains(vert.triangleIndxList[k]))
                            triangleIndxToRemove.Add(vert.triangleIndxList[k]);
                }

                //Sort triangleToRemove by the index
                triangleIndxToRemove.Sort();

                //Remove triangles
                for (int i = triangleIndxToRemove.Count - 1; i >= 0; i--)
                    triangles.RemoveAt(triangleIndxToRemove[i]);

                //Update the vertex index in triangle list
                for (int i = vertToRemove.Count - 1; i >= 0; i--)
                    for (int j = 0; j < triangles.Count; j++)
                        if (triangles[j] > vertToRemove[i].index) triangles[j] -= 1;

                UpdateCellList(vertToRemove, triangleIndxToRemove, cellToRemove);
            }
            WriteMeshData();
        }

        void EditGrass(Camera camera)
        {
            RaycastHit terrainHit;
            Ray ray = camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector3 pos = positions[i];
                    pos += this.transform.position;
                    float dist = Vector3.Distance(terrainHit.point, pos);

                    // if its within the radius of the brush, update color, width and length
                    if (dist <= brushSize)
                    {
                        var newColor = new Color(AdjustedColor.r + (Random.Range(0, 1.0f) * rangeR), AdjustedColor.g + (Random.Range(0, 1.0f) * rangeG), AdjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1);
                        colors[i] = newColor;
                        length[i] = new Vector2(sizeWidth, sizeLength);
                    }
                }
            }
            WriteMeshData();
        }

        void AddPoint(Vector3 position, Vector3 normal, List<VertexData> vertexList)
        {
            var grassPosition = position;// + Vector3.Cross(origin, hitNormal);
            grassPosition -= this.transform.position;
            // Debug.Log("hit pos: " + position + " transform.position: " + transform.position + " grassPosition: " + grassPosition);

            positions.Add(grassPosition);
            length.Add(new Vector2(sizeWidth, sizeLength));
            // add random color variations                          
            colors.Add(new Color(AdjustedColor.r + (Random.Range(0, 1.0f) * rangeR), AdjustedColor.g + (Random.Range(0, 1.0f) * rangeG), AdjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));
            normals.Add(normal.normalized);
            vertexCount++;

            VertexData vert = new VertexData(positions.Count - 1, grassPosition, new List<int>());
            vertexList.Add(vert);
        }

        void AddTriangles(int initialIndex, List<VertexData> vertexList)
        {
            //Setting triangles
            int t = initialIndex;

            List<int[]> newTris = new List<int[]>();
            for (int i = 1; i < vertexList.Count - 1; i++)
            {
                newTris.Add(new int[] { t, t + i + 1, t + i });
            }
            newTris.Add(new int[] { t, t + 1, t + vertexList.Count - 1 });

            foreach (var tri in newTris)
            {
                for (int i = 0; i < 3; i++)
                {
                    int vertIndx = tri[i];
                    triangles.Add(vertIndx);
                    int triangleIndx = triangles.Count - 1;
                    for (int j = 0; j < 3; j++)
                    {
                        int vertIndx2 = tri[j];
                        VertexData vert = vertexList.Find(x => x.index == vertIndx2);
                        if (!vert.triangleIndxList.Contains(triangleIndx)) vert.triangleIndxList.Add(triangleIndx);
                    }
                }
            }
        }

        void UpdateCellList(List<VertexData> vertexToRemove, List<int> triangleIndxToRemove, List<Cell> cellToRemove)
        {

            //Remove cells in our cellList.m_CellList
            foreach (var cell in cellToRemove)
                cellList.Remove(cell);

            UpdateVertexIndexInCellList(vertexToRemove);
            UpdateTriangleIndexInCellList(triangleIndxToRemove);
        }

        void UpdateVertexIndexInCellList(List<VertexData> vertexToRemove)
        {
            for (int i = vertexToRemove.Count - 1; i >= 0; i--)
            {
                foreach (var cell in cellList)
                {
                    for (int j = 0; j < cell.vertices.Count; j++)
                    {
                        VertexData vertex = cell.vertices[j];
                        if (vertex.index > vertexToRemove[i].index)
                        {
                            vertex.index--; // Update vertex index in the list
                        }
                        else if (vertex.index == vertexToRemove[i].index)
                        {
                            cell.vertices.RemoveAt(j); // Remove the vertex in the list
                            j--; // Keep for-loop going
                        }
                    }
                }
            }
        }

        void UpdateTriangleIndexInCellList(List<int> triangleIndxToRemove)
        {
            for (int i = triangleIndxToRemove.Count - 1; i >= 0; i--)
            {
                foreach (var cell in cellList)
                {
                    foreach (var vert in cell.vertices)
                    {
                        for (int j = 0; j < vert.triangleIndxList.Count; j++)
                        {
                            if (vert.triangleIndxList[j] > triangleIndxToRemove[i])
                            {
                                vert.triangleIndxList[j]--; // Update triangle index list
                            }
                            else if (vert.triangleIndxList[j] == triangleIndxToRemove[i])
                            {
                                vert.triangleIndxList.RemoveAt(j); // Remove triangle index in the list
                                j--; // Keep for-loop going
                            }
                        }
                    }
                }
            }
        }

        void DrawGrassGizmos()
        {
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(hitPosGizmo, hitNormal, brushSize);
            Handles.color = new Color(0, 0.5f, 0.5f, 0.4f);
            Handles.DrawSolidDisc(hitPosGizmo, hitNormal, brushSize);
            if (toolbarInt == 1)
            {
                Handles.color = Color.red;
                Handles.DrawWireDisc(hitPosGizmo, hitNormal, brushSize);
                Handles.color = new Color(0.5f, 0f, 0f, 0.4f);
                Handles.DrawSolidDisc(hitPosGizmo, hitNormal, brushSize);
            }
            if (toolbarInt == 2)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hitPosGizmo, hitNormal, brushSize);
                Handles.color = new Color(0.5f, 0.5f, 0f, 0.4f);
                Handles.DrawSolidDisc(hitPosGizmo, hitNormal, brushSize);
            }
        }
        #endregion

        private void WriteMeshData()
        {
            mesh.Clear();

            mesh.SetVertices(positions);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, length);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            mesh.name = "Temp";
            mesh.RecalculateTangents();
            filter.sharedMesh = mesh;

            EditorUtility.SetDirty(filter);
        }

        #region [Save / Load Data]
        void SaveData(Scene scene, string path)
        {
            SaveData();
        }

        void SaveData()
        {
            if (grassData == null)
                Debug.LogError($"[GameObject : {gameObject.name}] Failed saving GrassData because grassData is null!", gameObject);
            if (filter.sharedMesh == null)
                Debug.LogError($"[GameObject : {gameObject.name}] Failed saving GrassData because MeshFilter doesn't have mesh!", gameObject);
            if (cellList.Count == 0)
            {
                Debug.LogWarning($"[GameObject : {gameObject.name}] Failed saving GrassData! Grass Count cannot be 0 or Data was lost or not cached! Please load Grass Data again!", gameObject);
                EditorUtility.SetDirty(filter);
                LoadData();
            }


            if (grassData != null && filter.sharedMesh != null && cellList.Count != 0)
            {
                string meshPath = AssetDatabase.GetAssetPath(grassData.m_Mesh);

                if (grassData.m_Mesh == null)
                {
                    string GrassFolderName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(grassData));
                    string fileName = grassData.name;
                    meshPath = $"{GrassFolderName}/{fileName}mesh.asset";
                }

                Debug.Log($"[GameObject : {gameObject.name}] Saving MeshData to {meshPath}. CellList size: {cellList.Count}", gameObject);
                AssetDatabase.DeleteAsset(meshPath);

                var mesh = new Mesh();
                mesh.SetVertices(filter.sharedMesh.vertices);
                mesh.SetTriangles(filter.sharedMesh.triangles, 0);
                mesh.SetUVs(0, filter.sharedMesh.uv);
                mesh.SetColors(filter.sharedMesh.colors);
                mesh.SetNormals(filter.sharedMesh.normals);

                AssetDatabase.CreateAsset(mesh, meshPath);
                grassData.m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                grassData.m_CellList = new List<Cell>(cellList);
                Debug.Log($"[GameObject : {gameObject.name}] Saving GrassData to {AssetDatabase.GetAssetPath(grassData)}. CellList size: {cellList.Count}", gameObject);
                EditorUtility.SetDirty(grassData);
                // AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        void LoadData(Scene scene, OpenSceneMode mode)
        {
            LoadData();
        }

        public void LoadData()
        {
            if (grassData != null)
            {
                Debug.Log($"[GameObject : {gameObject.name}] Loading Grass Data", gameObject);
                //Read Mesh Data
                grassData.m_Mesh.GetVertices(positions);
                grassData.m_Mesh.GetTriangles(triangles, 0);
                grassData.m_Mesh.GetUVs(0, length);
                grassData.m_Mesh.GetColors(colors);
                grassData.m_Mesh.GetNormals(normals);
                vertexCount = grassData.m_Mesh.vertices.Length;

                //Set Mesh Data to filter
                mesh.Clear();
                mesh.SetVertices(positions);
                mesh.SetTriangles(triangles, 0);
                mesh.SetUVs(0, length);
                mesh.SetColors(colors);
                mesh.SetNormals(normals);
                mesh.name = "Temp";
                filter.sharedMesh = mesh;

                //Load Grass Data
                cellList = new List<Cell>(grassData.m_CellList);
                Debug.Log($"[GameObject : {gameObject.name}] Loading GrassData Successful! CellList size : {cellList.Count}", gameObject);
            }
            else
            {
                Debug.LogError($"[GameObject : {gameObject.name}] Failed loading GrassData because GrassData is null!", gameObject);
            }
        }
        #endregion

        #region [Initialize Data / Save Copy]
        public void CreateNewGrassData(string fileName, string dataFolderPath)
        {
            if (!Directory.Exists(dataFolderPath))
            {
                Debug.Log($"{dataFolderPath} doesn't exist! Trying to create directory...");
                if (dataFolderPath.StartsWith("Assets/"))
                {
                    if (!dataFolderPath.EndsWith("/"))
                        dataFolderPath += "/";

                    Directory.CreateDirectory(dataFolderPath);
                }
                else
                {
                    Debug.LogError("Data Path must start with \"Assets/\"!");
                    return;
                }
            }

            string path = dataFolderPath + fileName + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            AssetDatabase.Refresh();

            var grassDataPath = path + fileName + ".asset";
            Debug.Log($"[GameObject : {gameObject.name}] Creating GrassData to {grassDataPath}", gameObject);

            GrassData newData = ScriptableObject.CreateInstance<GrassData>();
            AssetDatabase.CreateAsset(newData, grassDataPath);
            AssetDatabase.Refresh();

            grassData = AssetDatabase.LoadAssetAtPath<GrassData>(grassDataPath);

            Mesh savedMesh = new Mesh();

            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = mesh;
            }
            var meshDataPath = path + grassData.name + "mesh.asset";
            Debug.Log($"[GameObject : {gameObject.name}] Creating Grass Mesh to {grassDataPath}", gameObject);

            AssetDatabase.CreateAsset(savedMesh, meshDataPath);
            grassData.m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshDataPath);
            grassData.m_CellList = new List<Cell>(cellList);
            EditorUtility.SetDirty(grassData);
            AssetDatabase.Refresh();
        }

        public void SaveCopy()
        {
            string path = AssetDatabase.GetAssetPath(grassData);
            path = Path.GetDirectoryName(path);
            string timeStamp = DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss");

            GrassData copyData = ScriptableObject.CreateInstance<GrassData>();
            string copiedGrassDataPath = $"{path}/{grassData.name}-Copy-{timeStamp}.asset";
            Debug.Log($"[GameObject : {gameObject.name}] Saving a copy of the GrassData to {copiedGrassDataPath}", gameObject);

            AssetDatabase.CreateAsset(copyData, copiedGrassDataPath);
            var copy = AssetDatabase.LoadAssetAtPath<GrassData>(copiedGrassDataPath);

            var copiedMesh = new Mesh();
            copiedMesh.SetVertices(filter.sharedMesh.vertices);
            copiedMesh.SetTriangles(filter.sharedMesh.triangles, 0);
            copiedMesh.SetUVs(0, filter.sharedMesh.uv);
            copiedMesh.SetColors(filter.sharedMesh.colors);
            copiedMesh.SetNormals(filter.sharedMesh.normals);

            var copiedMeshPath = $"{path}/{grassData.name}-Mesh-Copy-{timeStamp}.asset";
            Debug.Log($"[GameObject : {gameObject.name}] Saving a copy of the Grass Mesh to {copiedMeshPath}", gameObject);

            AssetDatabase.CreateAsset(copiedMesh, copiedMeshPath);
            copy.m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(copiedMeshPath);
            copy.m_CellList = new List<Cell>(cellList);
            EditorUtility.SetDirty(copy);
            AssetDatabase.Refresh();
        }
        #endregion

        #region [Help functions]
        public bool IsDataInitialized()
        {
            return filter.sharedMesh != null || grassData != null;
        }

        Vector2 CartesianToPolar(Vector2 point)
        {
            var polar = new Vector2();

            //calc longitude
            polar.x = Mathf.Atan2(point.x, point.y);
            polar.x *= Mathf.Rad2Deg;
            polar.y = point.magnitude;

            return polar;
        }

        // Vector3 offset = PolarToCartesian(new Vector2(60 * (j - 1), r), hitNormal);
        Vector3 PolarToCartesian(Vector2 polar, Vector3 normal)
        {
            //an origin vector, representing angle,length of 0,1. 
            var origin = normal == Vector3.up ? Vector3.right : new Vector3(-normal.z, 0, normal.x);
            //build a quaternion using euler angles for angle,length
            var rotation = Quaternion.AngleAxis(-polar.x, normal);
            //transform our reference vector by the rotation. Easy-peasy!
            var point = rotation * origin.normalized;

            return point * polar.y;
        }
        #endregion
#endif
    }
}

