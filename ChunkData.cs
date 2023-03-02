using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using System.Threading.Tasks;

[System.Serializable]
public class ChunkData
{
    int x;
    int y;
    int z;
    public int3 position
    {
        get { return new int3(x, y, z); }
        set
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }
    }

    public ChunkData(int3 pos, World _world) { position = pos; world = _world; }

    public NativeParallelHashMap<int3, VoxelState> map = new(VoxelData.ChunkSize * VoxelData.ChunkSize * VoxelData.ChunkSize, Allocator.Persistent);
    public List<StructureData> structures = new();

    [NonSerialized] public Chunk chunk;
    [NonSerialized] public ChunkData[] neighbours = new ChunkData[6];
    [NonSerialized] public World world;
    [NonSerialized] public List<int3> overlappingStructures = new();

    public void PopulateWithJob()
    {
        var job = new PopulateJob
        {
            position = new int3(position.x, position.y, position.z)*VoxelData.ChunkSize,
            map = map
        };

        var jobHandle = job.Schedule();

        jobHandle.Complete();

        map = job.map;
    }

    public void ModifyVoxel(int3 pos, byte _id, byte direction, bool list)
    {
        if (map[pos].id == _id) return;


        VoxelState voxel = map[pos];

        voxel.id = _id;
        voxel.orientation = direction;
        voxel.created = true;

        map[pos] = voxel;

        if (list)
        {
            world.worldData.AddToModifiedChunkList(this);
        }

        if(chunk != null)
        {
            world.AddChunkToUpdate(chunk, list);
        }
    }


    public VoxelState GetNeighbour(int3 pos)
    {
        if (BlockData.OutOfChunk(pos))
        {
            return World.Instance.worldData.GetVoxel(new Vector3(position.x + pos.x, position.y + pos.z, position.z + pos.z));
        }
        else
        {
            return map[pos];
        }
    }

    public void Dispose()
    {
        map.Dispose();
    }
}

[System.Serializable]
public struct VoxelState
{
    public bool created;
    public byte id;
    public byte orientation;
}
