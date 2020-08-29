using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelStreaming : MonoBehaviour
{

    const float ChunkRefreshThreshold = 25f;
    const float SquareChunkRefreshThreshold = ChunkRefreshThreshold * ChunkRefreshThreshold;

    public LODInfo[] DetailLevels;
    public static float MaxViewDist;
    public Transform Viewer;
    public Material MapMaterial;

    public static Vector2 ViewerPosition;
    Vector2 PreviousViewerPosition;
    static MapGenerator mapGenerator;
    int ChunkSize;
    int ChunksVisible;

    Dictionary<Vector2, TerrainChunk> ChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> ChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        MaxViewDist = DetailLevels[DetailLevels.Length - 1].VisibilityThreshold;
        ChunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        ChunksVisible = Mathf.RoundToInt(MaxViewDist / ChunkSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z);

        if((PreviousViewerPosition - ViewerPosition).sqrMagnitude > SquareChunkRefreshThreshold) {
            PreviousViewerPosition = ViewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {

        for (int i = 0; i < ChunksVisibleLastUpdate.Count; i++)
        {
            ChunksVisibleLastUpdate[i].SetVisible(false);
        }
        ChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / ChunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / ChunkSize);

        for(int yOffset = -ChunksVisible; yOffset <= ChunksVisible; yOffset++)
        {
            for (int xOffset = -ChunksVisible; xOffset <= ChunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (ChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    ChunkDictionary[viewedChunkCoord].UpdateChunks();
                    if (ChunkDictionary[viewedChunkCoord].isVisible())
                    {
                        ChunksVisibleLastUpdate.Add(ChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    ChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, ChunkSize, DetailLevels, transform, MapMaterial));
                }
            }
        }
    }

    public class TerrainChunk

    {
        GameObject MeshObject;
        Vector2 Position;
        Bounds ChunkBounds;

        MapData mapData;
        bool MapDataReceived;

        MeshRenderer MeshRenderer;
        MeshFilter MeshFilter;
        LODInfo[] DetailLevels;
        LODMesh[] LODMeshes;
        int PreviousLODIndex = -1;

        public TerrainChunk(Vector2 Coordinate, int Size, LODInfo[] DetailLevels, Transform Parent, Material material)
        {
            Position = Coordinate * Size;
            this.DetailLevels = DetailLevels;
            ChunkBounds = new Bounds(Position, Vector2.one * Size);
            Vector3 positionV3 = new Vector3(Position.x, 0, Position.y);

            MeshObject = new GameObject("Terrain Chunk");

            MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
            MeshRenderer.material = material;
            MeshFilter = MeshObject.AddComponent<MeshFilter>();

            MeshObject.transform.position = positionV3;
            MeshObject.transform.parent = Parent;
            SetVisible(false);

            LODMeshes = new LODMesh[DetailLevels.Length];
            for(int i = 0; i < DetailLevels.Length; i++) {
                LODMeshes[i] = new LODMesh(DetailLevels[i].LOD, UpdateChunks);
            }

            mapGenerator.RequestMapData(Position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            MapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap, MapGenerator.MAP_CHUNK_SIZE, MapGenerator.MAP_CHUNK_SIZE);
            MeshRenderer.material.mainTexture = texture;

            UpdateChunks();
        }

        public void UpdateChunks()
        {
            if (MapDataReceived) { 
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(ChunkBounds.SqrDistance(ViewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= MaxViewDist;

                if (visible) {
                    int LODIndex = 0;

                    for(int i = 0; i < DetailLevels.Length; i++) {
                        if(viewerDistanceFromNearestEdge > DetailLevels[i].VisibilityThreshold) {
                            LODIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    if (LODIndex != PreviousLODIndex) {
                        LODMesh lodMesh = LODMeshes[LODIndex];
                        if (lodMesh.hasMesh) {
                            PreviousLODIndex = LODIndex;
                            MeshFilter.mesh = lodMesh.mesh;
                        }else if(!lodMesh.hasRequestedMesh){
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool Visible)
        {
            MeshObject.SetActive(Visible);
        }

        public bool isVisible()
        {
            return MeshObject.activeSelf;
        }
    }

    public class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int LOD;
        System.Action UpdateCallback;

        public LODMesh(int LOD, System.Action UpdateCallback) {
            this.LOD = LOD;
            this.UpdateCallback = UpdateCallback;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, LOD, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int LOD;
        public float VisibilityThreshold;
    }

}

