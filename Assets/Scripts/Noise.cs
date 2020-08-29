using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int MapWidth, int MapHeight, float Scale, int Octaves, float Persistence, float Lacunarity, int seed, Vector2 Scroll)
    {
        float[,] NoiseMap = new float[MapWidth, MapHeight];

        System.Random Rand = new System.Random(seed);
        Vector2[] OctaveOffsets = new Vector2[Octaves];
        for(int i = 0; i < Octaves; i++)
        {
            float OffsetX = Rand.Next(-100000, 100000) + Scroll.x;
            float OffsetY = Rand.Next(-100000, 100000) + Scroll.y;

            OctaveOffsets[i] = new Vector2(OffsetX, OffsetY);
        }

        if(Scale <= 0)
        {
            Scale = 0.0001f;
        }

        float MaxNoiseHeight = float.MinValue;
        float MinNoiseHeight = float.MaxValue;

        float CenterWidth = MapWidth/2f;
        float CenterHeight = MapHeight/2f;

        for(int y = 0; y < MapHeight; y++)
        {
            for(int x = 0; x < MapWidth; x++)
            {
                float Amplitude = 1;
                float Frequency = 1;
                float NoiseHeight = 0;

                for (int i = 0; i < Octaves; i++)
                {
                    float SampleX = (x - CenterWidth) / Scale * Frequency + OctaveOffsets[i].x * Frequency;
                    float SampleY = (y - CenterHeight) / Scale * Frequency + OctaveOffsets[i].y * Frequency;

                    float Perlin = Mathf.PerlinNoise(SampleX, SampleY) * 2 - 1;
                    NoiseHeight += Perlin * Amplitude;

                    Amplitude *= Persistence;
                    Frequency *= Lacunarity;
                }

                if(NoiseHeight > MaxNoiseHeight)
                {
                    MaxNoiseHeight = NoiseHeight;
                }
                else
                {
                    MinNoiseHeight = NoiseHeight;
                }
                NoiseMap[x, y] = NoiseHeight;
            }
        }

        // Normalize noise map to between 0 and 1
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                NoiseMap[x, y] = Mathf.InverseLerp(MinNoiseHeight, MaxNoiseHeight, NoiseMap[x, y]);
            }
        }

                return NoiseMap;
    }
}
