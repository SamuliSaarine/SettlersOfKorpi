using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Noise
{
    public static float Get2DPerlin(int2 position, int offset, float scale)
    {
        position.x += offset + VoxelData.seed;
        position.y += offset + VoxelData.seed;

        return Mathf.PerlinNoise((float)position.x / VoxelData.ChunkSize * scale, (float)position.y / VoxelData.ChunkSize * scale);
    }

    public static bool Get3DPerlin(int3 position, float offset, float scale, float treshold)
    {
        float x = (position.x + offset + VoxelData.seed + 0.1f) * scale;
        float y = (position.y + offset + VoxelData.seed + 0.1f) * scale;
        float z = (position.z + offset + VoxelData.seed + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if((AB + BC + AC + BA + CB + CA) / 6f > treshold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
