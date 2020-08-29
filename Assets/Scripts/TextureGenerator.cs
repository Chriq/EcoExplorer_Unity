using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] ColorMap, int Width, int Height)
    {
        Texture2D texture = new Texture2D(Width, Height);
        texture.filterMode = FilterMode.Point;      // makes sharper texture
        texture.wrapMode = TextureWrapMode.Clamp;   // prevents wrapping
        texture.SetPixels(ColorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] HeightMap)
    {
        int Width = HeightMap.GetLength(0);
        int Height = HeightMap.GetLength(1);

        Color[] ColorMap = new Color[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                ColorMap[y * Width + x] = Color.Lerp(Color.black, Color.white, HeightMap[x, y]);
            }
        }

        return TextureFromColorMap(ColorMap, Width, Height);
    }
}
