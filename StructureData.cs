using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using System.Threading.Tasks;

[System.Serializable]
public class StructureData
{
    public int3 position;
    public int3 size;

    public NativeParallelHashMap<int3, VoxelState> map;
    public string gameObjectName;

    public StructureData(int3 pos, int3 _size, NativeParallelHashMap<int3, VoxelState> _map, string objectName="")
    {
        position = pos; 
        map = _map;
        gameObjectName = objectName;
        size = _size;
    }

    [NonSerialized] public Structure structure;

    public void ModifyVoxel(int3 pos, byte _id, byte direction)
    {
        if (map[pos].id == _id) return;


        VoxelState voxel = map[pos];

        voxel.id = _id;
        voxel.orientation = direction;
        voxel.created = true;

        map[pos] = voxel;

        if(structure != null)
        {
            World.Instance.AddStructureToUpdate(structure);
        }
    }

    public void Dispose()
    {
        map.Dispose();
    }
}
