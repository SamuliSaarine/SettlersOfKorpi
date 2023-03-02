using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Threading.Tasks;
using System;

public class Structure
{
    public int3 position;

    GameObject structureObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private bool _isActive;

    [HideInInspector] public StructureData structureData { get; private set; }
    [HideInInspector] public List<VoxelState> activeVoxels = new();

    MeshStorage meshStorage;

    public Structure(StructureData data)
    {
        if (World.Instance.destroyed) return;

        position = data.position;

        structureObject = data.gameObjectName.Length > 0 ? GameObject.Instantiate(Resources.Load<GameObject>(data.gameObjectName)) : new GameObject();
        meshFilter = structureObject.AddComponent<MeshFilter>();
        meshRenderer = structureObject.AddComponent<MeshRenderer>();

        Material[] materials = new Material[3];
        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        materials[2] = World.Instance.fluidMaterial;
        meshRenderer.materials = materials;
        structureObject.transform.SetParent(World.Instance.transform, false);
        structureObject.transform.position = new Vector3(position.x, position.y, position.z);
        structureObject.name = $"{data.gameObjectName}({position.x}, {position.y}, {position.z})";

        structureData = data;
        structureData.structure = this;
        World.Instance.AddStructureToUpdate(this);
        Place();
    }

    void Place()
    {
        position = World.Instance.RoundToInt3(structureObject.transform.position);
        structureData.position = position;

        WorldData worldData = World.Instance.worldData;

        foreach (var voxel in structureData.map)
        {
            worldData.SetVoxel(position+voxel.Key, 0, 1);
        }
    }

    public void UpdateStructure()
    {
        var meshData = new UpdateStructure.MeshData()
        {
            vertices = new(Allocator.TempJob),
            triangles = new(Allocator.TempJob),
            transparentTriangles = new(Allocator.TempJob),
            fluidTriangles = new(Allocator.TempJob),
            colors = new(Allocator.TempJob),
            normals = new(Allocator.TempJob),
            uvs = new(Allocator.TempJob)
        };

        var jobHandle = new UpdateStructure
        {
            meshData = meshData,
            structureSize = structureData.size,
            map = structureData.map,
            LOD = 0
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

        World.Instance.structuresToDraw.Add(this);

        meshData.vertices.Dispose();
        meshData.triangles.Dispose();
        meshData.transparentTriangles.Dispose();
        meshData.fluidTriangles.Dispose();
        meshData.colors.Dispose();
        meshData.normals.Dispose();
        meshData.uvs.Dispose();
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

    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if (structureObject != null)
            {
                structureObject.SetActive(value);
            }
        }
    }

    public void EditVoxel(Vector3 pos, byte newID, int amount)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= position.x;
        yCheck -= position.y;
        zCheck -= position.z;

        if (xCheck + amount > VoxelData.ChunkSize)
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
                    structureData.ModifyVoxel(new int3(x, y, z), newID, (byte)World.Instance.controlledPlayer.orientation);

                    VoxelState voxel = structureData.map[new int3(x, y, z)];

                    /*for (int i = 0; i < 6; i++)
                    {
                        BlockBehaviour.Active(voxel.neighbours[i]);
                    }*/
                }
            }
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

        mesh.uv = meshStorage.uvs;
        mesh.colors = meshStorage.colors;
        mesh.normals = meshStorage.normals;

        meshFilter.mesh = mesh;
    }
}
