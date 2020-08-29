using System.Collections;
using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

    public enum RenderMode { Noise, Colors, Mesh };
    public RenderMode Mode;

    public const int MAP_CHUNK_SIZE = 241;
    [Range(0, 6)]
    public int PreviewLOD;

    public float NoiseScale;
    public int Octaves;
    public float Persistence;
    public float Lacunarity;
    public int seed;
    public Vector2 Scroll;

    public float MeshHeightMultiplier;
    public AnimationCurve MeshHeightCurve;

    public bool AutoUpdate;

    public TerrainType[] Regions;

    Queue<MapThreadInfo<MapData>> MapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> MeshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay Display = FindObjectOfType<MapDisplay>();
        if (Mode == RenderMode.Noise) {
            Display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
        } else if (Mode == RenderMode.Colors) {
            Display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.ColorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        } else if (Mode == RenderMode.Mesh) {
            Display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, MeshHeightMultiplier, MeshHeightCurve, PreviewLOD),
                             TextureGenerator.TextureFromColorMap(mapData.ColorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
    }

    public void RequestMapData(Vector2 Center, Action<MapData> Callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(Center, Callback);
        };

        new Thread(threadStart).Start();
    }

    public void MapDataThread(Vector2 Center, Action<MapData> Callback) {
        MapData mapData = GenerateMapData(Center);
        lock (MapDataThreadQueue) {
            MapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(Callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int LOD, Action<MeshData> Callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, LOD, Callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int LOD, Action<MeshData> Callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, MeshHeightMultiplier, MeshHeightCurve, LOD);
        lock (MeshDataThreadQueue) {
            MeshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(Callback, meshData));
        }
    }

    void Update() {
        if(MapDataThreadQueue.Count > 0) {
            for(int i = 0; i < MapDataThreadQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = MapDataThreadQueue.Dequeue();
                threadInfo.Callback(threadInfo.Param);
            }
        }

        if (MeshDataThreadQueue.Count > 0) {
            for (int i = 0; i < MeshDataThreadQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = MeshDataThreadQueue.Dequeue();
                threadInfo.Callback(threadInfo.Param);
            }
        }
    }

    MapData GenerateMapData(Vector2 Center) {
        float[,] NoiseMap = Noise.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, NoiseScale, Octaves, Persistence, Lacunarity, seed, Center + Scroll);

        Color[] ColorMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];
        for (int y = 0; y < MAP_CHUNK_SIZE; y++) {
            for (int x = 0; x < MAP_CHUNK_SIZE; x++) {
                float currentHeight = NoiseMap[x, y];
                for (int i = 0; i < Regions.Length; i++) {
                    if (currentHeight <= Regions[i].height) {
                        ColorMap[y * MAP_CHUNK_SIZE + x] = Regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(NoiseMap, ColorMap);

    }

    private void OnValidate() {
        if (Lacunarity < 1) {
            Lacunarity = 1;
        }
        if (Octaves < 0) {
            Octaves = 0;
        }
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> Callback;
        public readonly T Param;

        public MapThreadInfo(Action<T> callback, T param) {
            this.Callback = callback;
            this.Param = param;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] HeightMap;
    public readonly Color[] ColorMap;

    public MapData(float[,] HeightMap, Color[] ColorMap) {
        this.HeightMap = HeightMap;
        this.ColorMap = ColorMap;
    }
}