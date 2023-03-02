using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Threading.Tasks;

public class Chunk
{
    public int3 coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    //readonly Material[] materials = new Material[3];

    public Vector3 position;
    public int lod;

    private bool _isActive;

    [HideInInspector] public ChunkData chunkData { get; private set; }
    [HideInInspector] public List<VoxelState> activeVoxels = new();

    MeshStorage meshStorage;

    public Chunk (int3 _coord, int LOD)
    {
        coord = _coord;
        lod = LOD;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        Material[] materials = new Material[3];
        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        materials[2] = World.Instance.fluidMaterial;
        meshRenderer.materials = materials;
        if(!World.Instance.destroyed) chunkObject.transform.SetParent(World.Instance.transform, false);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkSize, coord.y * VoxelData.ChunkSize, coord.z * VoxelData.ChunkSize);
        chunkObject.name = "Chunk(" + coord.x + ", " + coord.y + ", " + coord.z + ")";
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new int3(coord.x, coord.y, coord.z), true);
        chunkData.chunk = this;
        World.Instance.AddChunkToUpdate(this);
    }

    public void UpdateChunk()
    {
        var meshData = new UpdateJob.MeshData()
        {
            vertices = new(Allocator.TempJob),
            triangles = new(Allocator.TempJob),
            transparentTriangles = new(Allocator.TempJob),
            fluidTriangles = new(Allocator.TempJob),
            colors = new(Allocator.TempJob),
            normals = new(Allocator.TempJob),
            uvs = new(Allocator.TempJob)
        };

        var neighbourVoxels = new UpdateJob.NeigbourVoxels()
        {
            back = NeighbourMap(0),
            front = NeighbourMap(1),
            top = NeighbourMap(2),
            bottom = NeighbourMap(3),
            left = NeighbourMap(4),
            right = NeighbourMap(5),
        };

        var jobHandle = new UpdateJob
        {
            meshData = meshData,
            map = chunkData.map,
            neigbours = neighbourVoxels,
            LOD = lod
        }.Schedule();

        jobHandle.Complete();

        meshStorage = new();
        meshStorage.vertices = meshData.vertices.ToArray().Select(vertex => new Vector3(vertex.x, vertex.y, vertex.z)).ToArray();
        meshStorage.normals = meshData.normals.ToArray().Select(vertex => new Vector3(vertex.x, vertex.y, vertex.z)).ToArray();
        meshStorage.colors = meshData.colors.ToArray().Select(vertex => new Color(vertex.x, vertex.y, vertex.z, vertex.w)).ToArray();
        meshStorage.uvs = meshData.uvs.ToArray().Select(vertex => new Vector2(vertex.x, vertex.y)).ToArray();
        meshStorage.triangles = meshData.triangles.ToArray();
        meshStorage.transparentTriangles = meshData.transparentTriangles.ToArray();
        meshStorage.fluidTriangles = meshData.fluidTriangles.ToArray();

        World.Instance.chunksToDraw.Add(this);

        meshData.vertices.Dispose();
        meshData.triangles.Dispose();
        meshData.transparentTriangles.Dispose();
        meshData.fluidTriangles.Dispose();
        meshData.colors.Dispose();
        meshData.normals.Dispose();
        meshData.uvs.Dispose();
    }

    NativeParallelHashMap<int3, VoxelState> NeighbourMap(int direction)
    {
        ChunkData cd = chunkData.neighbours[direction];

        if (cd==null)
        {
            cd = World.Instance.worldData.RequestChunk(chunkData.position + BlockData.nativeFaceChecks[direction], false);
            if(cd!=null)
            {
                chunkData.neighbours[direction] = cd;
            }
            else
            {
                return BlockData.empty;
            }
        }

        return cd.map;
    }

    public void AddActiveVoxel(VoxelState voxel)
    {
        /*if (!activeVoxels.Contains(voxel))
        {
            activeVoxels.Add(voxel);
        }*/
    }

    public void RemoveActiveVoxel(VoxelState voxel)
    {
        /*for (int i = 0; i < activeVoxels.Count; i++)
        {
            if(activeVoxels[i] == voxel)
            {
                activeVoxels.RemoveAt(i);
                return;
            }
        }*/
    }

    public void TickUpdate()
    {
        /*for (int i = activeVoxels.Count - 1; i >= 0; i--)
        {
            if(!BlockBehaviour.Active(activeVoxels[i]))
            {
                RemoveActiveVoxel(activeVoxels[i]);
            }
            else
            {
                BlockBehaviour.Behave(activeVoxels[i]);
            }
        }*/
    }

    /*public void UpdateLighting(NativeArray<VoxelInfo> voxelInfos)
    {
        var job = new LightingJob
        {
            voxels = voxelInfos,
            thisChunkPos = new int3(coord.x, coord.y, coord.z)
        };
        
        var jobHandle = job.Schedule();

        jobHandle.Complete();

        chunkData.SetLight(job.voxels);

        voxelInfos.Dispose();
    }*/

    public bool IsActive
    {
        set
        {
            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    public void EditVoxel (Vector3 pos, byte newID, int amount)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        yCheck -= Mathf.FloorToInt(chunkObject.transform.position.y);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        if(xCheck + amount > VoxelData.ChunkSize)
        {
            amount = VoxelData.ChunkSize - xCheck;
        }

        if (yCheck + amount > VoxelData.ChunkSize)
        {
            amount = VoxelData.ChunkSize - yCheck;
        }

        if (zCheck + amount > VoxelData.ChunkSize)
        {
            amount = VoxelData.ChunkSize - zCheck;
        }

        for (int x = xCheck; x < xCheck + amount; x++)
        {
            for (int y = yCheck; y < yCheck + amount; y++)
            {
                for (int z = zCheck; z < zCheck + amount; z++)
                {
                    chunkData.ModifyVoxel(new int3(x, y, z), newID, (byte)World.Instance.controlledPlayer.orientation, true);                

                    VoxelState voxel = chunkData.map[new int3(x, y, z)];

                    if(newID == 0)
                    {
                        VoxelState aboveVox = chunkData.GetNeighbour(new int3(x, y+1, z));
                        if(aboveVox.created)
                        {
                            if (BlockData.voxelTypes[aboveVox.id].drops)
                            {
                                if(BlockData.OutOfChunk(new int3(x, y + 1, z)))
                                {
                                    World.Instance.GetChunkFromVector3(new Vector3(pos.x, pos.y + 1, pos.z))?.CheckVoxelGravity(x, y+1, z);
                                }
                                else
                                {
                                    CheckVoxelGravity(x, y + 1, z);
                                }
                            }
                        }
                    }
                    else if(BlockData.voxelTypes[voxel.id].drops)
                    {
                        CheckVoxelGravity(x, y, z);
                    }

                    /*for (int i = 0; i < 6; i++)
                    {
                        BlockBehaviour.Active(voxel.neighbours[i]);
                    }*/
                }
            }
        }

        if (xCheck == 0) UpdateSurroundingVoxels(0, VoxelData.ChunkSize / 2, VoxelData.ChunkSize / 2);
        if (yCheck == 0) UpdateSurroundingVoxels(VoxelData.ChunkSize / 2, 0, VoxelData.ChunkSize / 2);
        if (zCheck == 0) UpdateSurroundingVoxels(VoxelData.ChunkSize / 2, VoxelData.ChunkSize / 2, 0);

        if (xCheck + amount == VoxelData.ChunkSize) UpdateSurroundingVoxels(VoxelData.ChunkSize, VoxelData.ChunkSize / 2, VoxelData.ChunkSize / 2);
        if (yCheck + amount == VoxelData.ChunkSize) UpdateSurroundingVoxels(VoxelData.ChunkSize / 2, VoxelData.ChunkSize, VoxelData.ChunkSize / 2);
        if (zCheck + amount == VoxelData.ChunkSize) UpdateSurroundingVoxels(VoxelData.ChunkSize / 2, VoxelData.ChunkSize / 2, VoxelData.ChunkSize);
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        int3 thisVoxel = new(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            int3 currentVoxel = thisVoxel + BlockData.nativeFaceChecks[p];

            if (BlockData.OutOfChunk(currentVoxel))
            {
                World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(new Vector3(currentVoxel.x, currentVoxel.y, currentVoxel.z) + position), true);
            }
        }
    }

    public void CheckVoxelGravity(int voxX, int voxY, int voxZ)
    {
        if (BlockData.voxelTypes[chunkData.GetNeighbour(new int3(voxX, voxY - 1, voxZ)).id].solidity<2)
        {

            //Destroying from old location
            VoxelState oldVoxel = chunkData.map[new int3(voxX, voxY, voxZ)];
            chunkData.map[new int3(voxX, voxY, voxZ)] = new VoxelState {created=true, id=0, orientation=1};

            for (int y = Mathf.FloorToInt(position.y)+(voxY - 1); y > -1; y--)
            {
                if (BlockData.voxelTypes[chunkData.GetNeighbour(new int3(voxX, y - 1, voxZ)).id].solidity>1)
                {
                    //Adding to new location
                    if (BlockData.OutOfChunk(new int3(voxX, y, voxZ)))
                    {
                        ChunkData cd = World.Instance.GetChunkFromVector3(new Vector3(position.x + voxX, y, position.z + voxZ))?.chunkData;
                        if(cd!=null)
                        {
                            cd.map[new int3(voxX, y, voxZ)] = new VoxelState { created = true, id = oldVoxel.id, orientation = oldVoxel.orientation };
                            cd.chunk.UpdateSurroundingVoxels(voxX, y, voxZ);
                        }
                        else
                        {
                            chunkData.map[new int3(voxX, y, voxZ)] = new VoxelState { created = true, id = oldVoxel.id, orientation = oldVoxel.orientation };
                            UpdateSurroundingVoxels(voxX, y, voxZ);
                        }
                    }

                    VoxelState aboveVox = chunkData.GetNeighbour(new int3(voxX, voxY + 1, voxZ));
                    if (aboveVox.created)
                    {
                        if (BlockData.voxelTypes[aboveVox.id].drops)
                        {
                            World.Instance.GetChunkFromVector3(new Vector3(position.x + voxX, y, position.z + voxZ))?.CheckVoxelGravity(voxX, voxY + 1, voxZ);
                        }
                    }
                    break;
                }
            }

            //CheckSurroundingsGravity(VoxX, VoxY, VoxZ);
        }
    }


    public void CreateMesh()
    {
        Mesh mesh = new();
        mesh.vertices = meshStorage.vertices;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(meshStorage.triangles, 0);
        mesh.SetTriangles(meshStorage.transparentTriangles, 1);
        mesh.SetTriangles(meshStorage.fluidTriangles, 2);

        //mesh.triangles = triangles.ToArray();
        mesh.uv = meshStorage.uvs;
        mesh.colors = meshStorage.colors;
        mesh.normals = meshStorage.normals;

        meshFilter.mesh = mesh;
    }
}

public struct MeshStorage
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public Color[] colors;
    public Vector2[] uvs;
    public int[] triangles;
    public int[] transparentTriangles;
    public int[] fluidTriangles;
}
