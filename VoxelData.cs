using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public static class VoxelData
{
    [ReadOnly] public static readonly int ChunkSize = 32;
    [ReadOnly] public static readonly int WorldSizeInChunks = 32;
    [ReadOnly] public static readonly int WorldHeightInChunks = 4;

    public static float minLightLevel = 0.4f;
    public static float maxLightLevel = 0.9f;
    [ReadOnly] public static readonly int seaLevel = 80;

    public static float tickLength = 1f;

    [ReadOnly] public static int seed;
    [ReadOnly] public static Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)seed);
    public static string worldName = "Korpi";

    public static int WorldCenter
    {
        get { return (WorldSizeInChunks * ChunkSize) / 2; }
    }

    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * ChunkSize; }

    }

    public static int VoxelsInChunk
    {
        get { return ChunkSize * ChunkSize * ChunkSize; }
    }

    public static int WorldHeightInVoxels
    {
        get { return WorldHeightInChunks * ChunkSize; }
    }

    public static bool IsVoxelInWorld(int3 pos)
    {
        if (pos.x >= 0 && pos.x < WorldSizeInVoxels && pos.y >= 0 && pos.y < WorldHeightInVoxels && pos.z >= 0 && pos.z <WorldSizeInVoxels)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [ReadOnly] public static readonly int textureAtlasSizeInBlocks = 8;

    public static float NormalizedBlockTextureSize
    {
        get { return 1f / textureAtlasSizeInBlocks; }
    }

    /*public static readonly Vector3Int[] faceChecks = new Vector3Int[6]
    {
        new Vector3Int(0, 0, -1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0)
    */

    public static readonly int[] revFaceChecks = new int[6] { 1, 0, 3, 2, 5, 4 };

    public static int VariantCounts(int firstVariant)
    {
        switch(firstVariant)
        {
            case 0:
                return 4;
            case 4:
                return 4;
            case 8:
                return 4;
            case 12:
                return 4;
            case 16:
                return 4;
            case 20:
                return 4;
            case 24:
                return 1;
            case 28:
                return 4;
            case 32:
                return 4;
            case 36:
                return 2;
            case 52:
                return 2;
        }

        return 1;
    }
}


public readonly struct BlockData
{
    [ReadOnly]
    public static readonly NativeArray<int3> voxelVerts = new NativeArray<int3>(8, Allocator.Persistent)
    {
        [0] = new int3(0, 0, 0),
        [1] = new int3(1, 0, 0),
        [2] = new int3(1, 1, 0),
        [3] = new int3(0, 1, 0),
        [4] = new int3(0, 0, 1),
        [5] = new int3(1, 0, 1),
        [6] = new int3(1, 1, 1),
        [7] = new int3(0, 1, 1),
    };

    [ReadOnly]
    public static NativeArray<int3> nativeFaceChecks = new NativeArray<int3>(6, Allocator.Persistent)
    {
        [0] = new int3(0, 0, -1),
        [1] = new int3(0, 0, 1),
        [2] = new int3(0, 1, 0),
        [3] = new int3(0, -1, 0),
        [4] = new int3(-1, 0, 0),
        [5] = new int3(1, 0, 0)
    };

    [ReadOnly]
    public static readonly NativeArray<int> voxelTris = new NativeArray<int>(24, Allocator.Persistent)
    {
        [0] = 0,
        [1] = 3,
        [2] = 1,
        [3] = 2,
        [4] = 5,
        [5] = 6,
        [6] = 4,
        [7] = 7,
        [8] = 3,
        [9] = 7,
        [10] = 2,
        [11] = 6,
        [12] = 1,
        [13] = 5,
        [14] = 0,
        [15] = 4,
        [16] = 4,
        [17] = 7,
        [18] = 0,
        [19] = 3,
        [20] = 1,
        [21] = 2,
        [22] = 5,
        [23] = 6
    };

    [ReadOnly]
    public static readonly NativeArray<float2> voxelUvs = new NativeArray<float2>(4, Allocator.Persistent)
    {
        [0] = new float2(0f, 0f),
        [1] = new float2(0f, 1f),
        [2] = new float2(1f, 0f),
        [3] = new float2(1f, 1f)
    };

    public static bool OutOfChunk(int3 pos)
    {
        if (pos.x < 0 || pos.x >= VoxelData.ChunkSize ||
           pos.y < 0 || pos.y >= VoxelData.ChunkSize ||
           pos.z < 0 || pos.z >= VoxelData.ChunkSize)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [ReadOnly] public static NativeArray<VoxelType> voxelTypes = new NativeArray<VoxelType>(World.Instance.blockTypes.Length, Allocator.Persistent);
    //[ReadOnly] public static NativeArray<BlockType> blockTypes = new NativeArray<BlockType>(World.Instance.blockTypes.Length, Allocator.Persistent);

    [ReadOnly] public static readonly NativeParallelHashMap<int3, VoxelState> empty = new(VoxelData.ChunkSize* VoxelData.ChunkSize* VoxelData.ChunkSize, Allocator.Persistent);

    public static void Dispose()
    {
        voxelVerts.Dispose();
        voxelTris.Dispose();
        nativeFaceChecks.Dispose();
        voxelUvs.Dispose();
        voxelTypes.Dispose();
        empty.Dispose();
    }
}


