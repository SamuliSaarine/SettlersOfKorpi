using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

[CreateAssetMenu(fileName = "Biome", menuName = "SettlersOfKorpi/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;
    public float treshold;
    public int offset;
    public float scale;

    public int minHeight;
    public float heightRange;
    public float terrainScale;

    [Range(0, 1)] public float subTreshold;
    public SubBiome[] subBiomes;

    public SubBiome GetSubBiome(int2 pos)
    {
        int b = 0;

        if (subBiomes.Length > 1)
        {
            for (int i = 1; i < subBiomes.Length; i++)
            {
                if (Noise.Get2DPerlin(pos, subBiomes[i].offset, subBiomes[i].scale) >= subTreshold)
                {
                    b = i;
                }
            }
        }

        return subBiomes[b];
    }
}

[System.Serializable]
public class SubBiome
{
    public string subBiomeName;

    public int offset;
    public float scale;

    [Header("Terrain")]
    public float octave1Scale;
    public float octave1Range;
    public int octave1Offset;
    
    public float octave2Scale;
    public float octave2Range;
    public int octave2Offset;

    [Header("Surface")]
    public byte surfaceBlock;
    public byte subSurfaceBlock;
    public byte subSurfaceDepth;
    public Undergrowth[] undergrowth;

    [Header("Structures")]
    public int highestTreeHeight;
    public Tree[] trees;
    public float zoneScale;
    [Range(0.1f, 1f)]
    public float zoneTreshold;

    [Header("Lodes")]
    public Lode[] lodes;
}

[System.Serializable]
public struct Tree
{
    public float probability;

    [Header("Trunk")]
    public byte w0;
    public byte w1;
    public byte w2;
    public byte w4;
    public int minWidth;
    public int maxWidth;
    public int minHeight;
    public int maxHeight;

    [Header("Foliage")]
    public byte foliage;
    public int minFWidth;
    public int maxFWidth;    
    public int minFHeight;
    public int maxFHeight;
    public float scale;
    public float minTreshold;
    public int fadeStart;
}

[System.Serializable]
public struct Lode
{
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
}

[System.Serializable]
public struct Undergrowth
{
    public byte blockId;
    public int zoneScale;
    public float zoneTreshold;
    public int zoneOffset;
    public float placementProbability;
}



public struct BiomeStruct
{
    public float treshold;
    public int offset;
    public float scale;

    public int minHeight;
    public float heightRange;
    public float terrainScale;

    public float subTreshold;
    public SubBiomeStruct[] subBiomes;
    public SubBiomeStruct GetSubBiome(int2 pos)
    {
        int b = 0;

        if (subBiomes.Length > 1)
        {
            for (int i = 1; i < subBiomes.Length; i++)
            {
                if (Noise.Get2DPerlin(pos, subBiomes[i].offset, subBiomes[i].scale) >= subTreshold)
                {
                    b = i;
                }
            }
        }

        return subBiomes[b];
    }

    public BiomeStruct(Biome b)
    {
        treshold = b.treshold;
        offset = b.offset;
        scale = b.scale;

        minHeight = b.minHeight;
        heightRange = b.heightRange;
        terrainScale = b.terrainScale;
        subTreshold = b.subTreshold;
        subBiomes = new SubBiomeStruct[b.subBiomes.Length];
        for (int i = 0; i < b.subBiomes.Length; i++)
        {
            subBiomes[i] = new SubBiomeStruct(b.subBiomes[i]);
        };
    }
}

public struct SubBiomeStruct
{
    public int offset;
    public float scale;

    public float octave1Scale;
    public float octave1Range;
    public int octave1Offset;

    public float octave2Scale;
    public float octave2Range;
    public int octave2Offset;

    public byte surfaceBlock;
    public byte subSurfaceBlock;
    public byte subSurfaceDepth;
    public NativeArray<Undergrowth> undergrowth;

    public int highestTreeHeight;
    public NativeArray<Tree> trees;
    public float zoneScale;
    public float zoneTreshold;

    public NativeArray<Lode> lodes;

    public SubBiomeStruct(SubBiome s)
    {
        offset = s.offset;
        scale = s.scale;

        octave1Scale = s.octave1Scale;
        octave1Range = s.octave1Range;
        octave1Offset = s.octave1Offset;

        octave2Scale = s.octave2Scale;
        octave2Range = s.octave2Range;
        octave2Offset = s.octave2Offset;

        surfaceBlock = s.surfaceBlock;
        subSurfaceBlock = s.subSurfaceBlock;
        subSurfaceDepth = s.subSurfaceDepth;

        undergrowth = new NativeArray<Undergrowth>(s.undergrowth, Allocator.Persistent);

        highestTreeHeight = s.highestTreeHeight;
        trees = new NativeArray<Tree>(s.trees, Allocator.Persistent);
        zoneScale = s.zoneScale;
        zoneTreshold = s.zoneTreshold;
        
        lodes = new NativeArray<Lode>(s.lodes, Allocator.Persistent);
    }
}