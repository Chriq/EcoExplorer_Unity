using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] HeightMap, float HeightMultiplier, AnimationCurve HeightCurve, int LOD)
    {
        int width = HeightMap.GetLength(0);
        int height = HeightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int MeshSimplificationIncrement = (LOD == 0) ? 1 : LOD * 2;
        int VerticesPerLine = ((width - 1) / MeshSimplificationIncrement) + 1;

        MeshData meshData = new MeshData(VerticesPerLine, VerticesPerLine);
        int vertexIndex = 0;

        AnimationCurve heightCurve = new AnimationCurve(HeightCurve.keys);

        for(int y = 0; y < height; y += MeshSimplificationIncrement)
        {
            for(int x = 0; x < width; x += MeshSimplificationIncrement)
            {
                lock (HeightCurve) {
                    Vector3 vertex = new Vector3(topLeftX + x, HeightCurve.Evaluate(HeightMap[x, y]) * HeightMultiplier, topLeftZ - y);
                    meshData.vertices[vertexIndex] = vertex;
                }
                
                Vector2 UV = new Vector2(x / (float)width, y / (float)height);             
                meshData.UVMap[vertexIndex] = UV;

                if(x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + VerticesPerLine + 1, vertexIndex + VerticesPerLine);
                    meshData.AddTriangle(vertexIndex + VerticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] UVMap;

    int triangleIndex;

    public MeshData(int MeshWidth, int MeshHeight)
    {
        vertices = new Vector3[MeshWidth * MeshHeight];
        triangles = new int[(MeshWidth - 1) * (MeshHeight - 1) * 6];
        UVMap = new Vector2[MeshWidth * MeshHeight];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVMap;
        mesh.RecalculateNormals();
        return mesh;
    }
}