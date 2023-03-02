using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Mathematics;

[System.Serializable]
public class WorldData
{
    public string worldName = "Korpi";
    public int seed;
    public GameTime time;

    [System.NonSerialized]
    public Dictionary<int3, ChunkData> chunks = new();
    [System.NonSerialized]
    public Dictionary<int3, StructureData> structures = new();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new();

    public void AddToModifiedChunkList(ChunkData chunk)
    {
        if(!modifiedChunks.Contains(chunk))
        {
            modifiedChunks.Add(chunk);
        }
    }

    public void SaveStructure(StructureData structure)
    {
        ChunkData c = RequestChunk(structure.position/VoxelData.ChunkSize, false);

        if (!c.structures.Contains(structure))
        {
           c.structures.Add(structure);
        }
    }

    public WorldData (string _worldName, int _seed, GameTime _time)
    {
        worldName = _worldName;
        seed = _seed;
        time = _time;
    }

    public WorldData(WorldData data)
    {
        worldName = data.worldName;
        seed = data.seed;
    }

    public ChunkData RequestChunk(int3 coord, bool create)
    {
        ChunkData c;
        if (chunks.ContainsKey(coord))
        {
            c = chunks[coord];
        }
        else if (!create)
        {
            c = null;
        }
        else
        {
            LoadChunk(coord);
            c = chunks[coord];
        }

        return c;
    }

    public StructureData RequestStructure(int3 pos)
    {
        StructureData c;
        if (chunks.ContainsKey(pos))
        {
            c = structures[pos];
        }
        else
        {
            c = null;
        }

        return c;
    }

    public void LoadChunk(int3 coord)
    {
        if (chunks.ContainsKey(coord)) return;

        ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);

        if(chunk != null)
        {
            chunks.Add(coord, chunk);
            foreach(StructureData sd in chunk.structures)
            {
                structures.Add(sd.position,sd);
                new Structure(sd);
            }
        }
        else
        {
            chunks.Add(coord, new ChunkData(coord, World.Instance));
            chunks[coord].PopulateWithJob();
        }
    }

    public void CreateStructure(StructureData data)
    {
        RequestChunk(data.position / VoxelData.ChunkSize, false)?.structures.Add(data);
        structures.Add(data.position, data);
        new Structure(data);
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.WorldHeightInVoxels && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetVoxel(int3 pos, byte value, byte orientation)
    {
        if(!VoxelData.IsVoxelInWorld(pos))
        {
            return;
        }

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkSize);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkSize);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);

        ChunkData chunk = RequestChunk(new int3(x, y, z), false);

        x *= VoxelData.ChunkSize;
        y *= VoxelData.ChunkSize;
        z *= VoxelData.ChunkSize;

        if (chunk != null)
        {
            chunk.ModifyVoxel(new int3(pos.x - x, pos.y - y, pos.z - z), value, orientation, false);
        }
    }

    public VoxelState GetVoxel(Vector3 pos)
    {
        if (!IsVoxelInWorld(pos))
        {
            return new VoxelState{created=false};
        }

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkSize);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkSize);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);

        ChunkData chunk = RequestChunk(new int3(x, y, z), false);

        if (chunk == null)
        {
            return new VoxelState { created = false }; 
        }


        x *= VoxelData.ChunkSize;
        y *= VoxelData.ChunkSize;
        z *= VoxelData.ChunkSize;

        int3 voxel = new int3(Mathf.FloorToInt(pos.x - x), Mathf.FloorToInt(pos.y - y), Mathf.FloorToInt(pos.z - z));
        if(chunk.map.ContainsKey(voxel))
        {
            VoxelState vox = chunk.map[voxel];
            if(vox.id==0)
            {
                int3 intpos = new int3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

                foreach (int3 structkey in chunk.overlappingStructures)
                {
                    if (structures[structkey].map.ContainsKey(intpos))
                    {
                        return structures[structkey].map[intpos];
                    };
                }

                return vox;
            }
            else
            {
                return vox;
            }
        }
        else
        {
            return new VoxelState { created = false };
        }
    }

    public int3 GetVoxelCoord(Vector3 pos, bool debug = false)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkSize);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkSize);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);

        x *= VoxelData.ChunkSize;
        y *= VoxelData.ChunkSize;
        z *= VoxelData.ChunkSize;

        return new int3(Mathf.FloorToInt(pos.x - x), Mathf.FloorToInt(pos.y - y), Mathf.FloorToInt(pos.z - z));
    }
}