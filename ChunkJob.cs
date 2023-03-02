using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

//[BurstCompile(CompileSynchronously = false, FloatMode = FloatMode.Fast, DisableSafetyChecks = true)]
public struct UpdateJob : IJob
{
    public struct MeshData
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeList<int> transparentTriangles;
        public NativeList<int> fluidTriangles;
        public NativeList<float2> uvs;
        public NativeList<float4> colors;
        public NativeList<float3> normals;
    }

    public struct NeigbourVoxels
    {
        public NativeParallelHashMap<int3, VoxelState> back;
        public NativeParallelHashMap<int3, VoxelState> front;
        public NativeParallelHashMap<int3, VoxelState> top;
        public NativeParallelHashMap<int3, VoxelState> bottom;
        public NativeParallelHashMap<int3, VoxelState> left;
        public NativeParallelHashMap<int3, VoxelState> right;
    }

    int vertexIndex;
    int LODStep;

    [WriteOnly] public MeshData meshData;

    [ReadOnly] public NeigbourVoxels neigbours;
    [ReadOnly] public NativeParallelHashMap<int3, VoxelState> map;
    [ReadOnly] public int LOD;

    public void Execute()
    {
        UpdateChunck();
    }

    private void UpdateChunck()
    {
        ClearMeshData();

        LODStep = (int)Mathf.Pow(2, LOD);

        for (int z = 0; z < VoxelData.ChunkSize; z+=LODStep)
        {
            for (int x = 0; x < VoxelData.ChunkSize; x+=LODStep)
            {
                for (int y = 0; y < VoxelData.ChunkSize; y+=LODStep)
                {
                    int3 pos = new int3(x, y, z);

                    if (map[pos].id != 0)
                    {
                        UpdateMeshData(pos);
                    }
                }
            }
        }
    }

    void UpdateMeshData(int3 pos)
    {

        VoxelState voxel = map[pos];
        VoxelMeshData voxelMesh = World.Instance.blockTypes[voxel.id].meshData;
        if(LOD > 0)
        {
            if(voxelMesh !=World.Instance.lodBlocks[0])
            {
                return;
            }
            else
            {
                voxelMesh = World.Instance.lodBlocks[LOD];
            }
        }
        VoxelType type = BlockData.voxelTypes[voxel.id];

        int rot = 0;
        switch (voxel.orientation)
        {
            case 0:
                rot = 180;
                break;
            case 1:
                rot = 0;
                break;
            case 4:
                rot = 90;
                break;
            case 5:
                rot = 270;
                break;
        }

        for (int p = 0; p < 6; p++)
        {
            int tp = p;
            if (voxel.orientation != 1)
            {
                if (voxel.orientation == 0)
                {
                    if (p == 0) tp = 1;
                    else if (p == 1) tp = 0;
                    else if (p == 4) tp = 5;
                    else if (p == 5) tp = 4;
                }
                else if (voxel.orientation == 5)
                {
                    if (p == 0) tp = 5;
                    else if (p == 1) tp = 4;
                    else if (p == 4) tp = 0;
                    else if (p == 5) tp = 1;
                }
                else if (voxel.orientation == 4)
                {
                    if (p == 0) tp = 4;
                    else if (p == 1) tp = 5;
                    else if (p == 4) tp = 1;
                    else if (p == 5) tp = 0;
                }
            }

            int3 faceCheck = BlockData.nativeFaceChecks[tp]*LODStep;
            /*if(faceCheck.x>0||faceCheck.y>0||faceCheck.z>0)
            {
                faceCheck *= LOD;
            }*/

            VoxelState neighbor = GetNeighbour(pos + faceCheck, tp);

            if (neighbor.created && BlockData.voxelTypes[neighbor.id].renderNeighborFaces && voxelMesh.faces[p].triangles.Length>0)             
            {
                if (neighbor.id == voxel.id && voxelMesh.dontRenderSameNeigbour) continue;
                int faceVertCount = 0;

                for (int i = 0; i < voxelMesh.faces[p].vertData.Length; i++)
                {
                    VertData vertData = voxelMesh.faces[p].GetVertData(i);
                    meshData.vertices.Add(pos + vertData.GetRotatedPosition(new int3(0, rot, 0)));
                    meshData.normals.Add(BlockData.nativeFaceChecks[p]);
                    AddTexture(World.Instance.blockTypes[voxel.id].GetTextureID(p,pos), vertData.uv);
                    faceVertCount++;
                }

                if (!type.renderNeighborFaces||LOD>1)
                {
                    for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                    {
                        meshData.triangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                    }
                }
                else
                {
                    if(type.isFluid)
                    {
                        for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                        {
                            meshData.fluidTriangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                        {
                            meshData.transparentTriangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                        }
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    void AddTexture(int textureID, float2 uv)
    {
        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        x += VoxelData.NormalizedBlockTextureSize * uv.x;
        y += VoxelData.NormalizedBlockTextureSize * uv.y;

        meshData.uvs.Add(new float2(x, y));
    }

    public VoxelState GetNeighbour(int3 pos, int direction)
    {
        if (BlockData.OutOfChunk(pos))
        {
            switch (direction)
            {
                case 0:
                    if (neigbours.back.ContainsKey(new int3(pos.x, pos.y, VoxelData.ChunkSize - 1))) return neigbours.back[new int3(pos.x, pos.y, VoxelData.ChunkSize - 1)];
                    break;
                case 1:
                    if (neigbours.front.ContainsKey(new int3(pos.x, pos.y, 0))) return neigbours.front[new int3(pos.x, pos.y, 0)];
                    break;
                case 2:
                    if (neigbours.top.ContainsKey(new int3(pos.x, 0, pos.z))) return neigbours.top[new int3(pos.x, 0, pos.z)];
                    break;
                case 3:
                    if (neigbours.bottom.ContainsKey(new int3(pos.x, VoxelData.ChunkSize - 1, pos.z))) return neigbours.bottom[new int3(pos.x, VoxelData.ChunkSize - 1, pos.z)];
                    break;
                case 4:
                    if (neigbours.left.ContainsKey(new int3(VoxelData.ChunkSize - 1, pos.y, pos.z))) return neigbours.left[new int3(VoxelData.ChunkSize - 1, pos.y, pos.z)];
                    break;
                case 5:
                    if (neigbours.right.ContainsKey(new int3(0, pos.y, pos.z))) return neigbours.right[new int3(0, pos.y, pos.z)];
                    break;
            }

            return new();
        }
        else
        {
            if (map.ContainsKey(pos))
            {
                return map[pos];
            }
            else
            {
                return new();
            }
        }
    }

    public VoxelType GetProperties(int id)
    {
        return BlockData.voxelTypes[id];
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        meshData.vertices.Clear();
        meshData.triangles.Clear();
        meshData.transparentTriangles.Clear();
        meshData.fluidTriangles.Clear();
        meshData.uvs.Clear();
        meshData.colors.Clear();
        meshData.normals.Clear();
    }
}

public struct UpdateStructure : IJob
{
    public struct MeshData
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeList<int> transparentTriangles;
        public NativeList<int> fluidTriangles;
        public NativeList<float2> uvs;
        public NativeList<float4> colors;
        public NativeList<float3> normals;
    }

    int vertexIndex;
    int LODStep;


    [WriteOnly] public MeshData meshData;

    [ReadOnly] public int3 structureSize;
    [ReadOnly] public NativeParallelHashMap<int3, VoxelState> map;
    [ReadOnly] public int LOD;

    public void Execute()
    {
        UpdateChunck();
    }

    private void UpdateChunck()
    {
        ClearMeshData();


        LODStep = (int)Mathf.Pow(2, LOD);
        for (int z = 0; z < structureSize.z; z += LODStep)
        {
            for (int x = 0; x < structureSize.x; x += LODStep)
            {
                for (int y = 0; y < structureSize.y; y += LODStep)
                {
                    int3 pos = new int3(x, y, z);

                    if (!map.ContainsKey(pos)) continue;                    

                    if (map[pos].id != 0)
                    {
                        UpdateMeshData(pos);
                    }
                }
            }
        }
    }

    void UpdateMeshData(int3 pos)
    {

        VoxelState voxel = map[pos];
        VoxelMeshData voxelMesh = World.Instance.blockTypes[voxel.id].meshData;
        if (LOD > 0)
        {
            if (voxelMesh != World.Instance.lodBlocks[0])
            {
                return;
            }
            else
            {
                voxelMesh = World.Instance.lodBlocks[LOD];
            }
        }
        VoxelType type = BlockData.voxelTypes[voxel.id];

        int rot = 0;
        switch (voxel.orientation)
        {
            case 0:
                rot = 180;
                break;
            case 1:
                rot = 0;
                break;
            case 4:
                rot = 90;
                break;
            case 5:
                rot = 270;
                break;
        }

        for (int p = 0; p < 6; p++)
        {
            int tp = p;
            if (voxel.orientation != 1)
            {
                if (voxel.orientation == 0)
                {
                    if (p == 0) tp = 1;
                    else if (p == 1) tp = 0;
                    else if (p == 4) tp = 5;
                    else if (p == 5) tp = 4;
                }
                else if (voxel.orientation == 5)
                {
                    if (p == 0) tp = 5;
                    else if (p == 1) tp = 4;
                    else if (p == 4) tp = 0;
                    else if (p == 5) tp = 1;
                }
                else if (voxel.orientation == 4)
                {
                    if (p == 0) tp = 4;
                    else if (p == 1) tp = 5;
                    else if (p == 4) tp = 1;
                    else if (p == 5) tp = 0;
                }
            }

            VoxelState neighbor = GetNeighbour(pos + BlockData.nativeFaceChecks[tp]*LODStep);

            if (BlockData.voxelTypes[neighbor.id].renderNeighborFaces && voxelMesh.faces[p].triangles.Length > 0)
            {
                //Debug.Log($"{voxel.id}|{neighbor.id}");
                if (neighbor.id == voxel.id && voxelMesh.dontRenderSameNeigbour) continue;
                int faceVertCount = 0;

                for (int i = 0; i < voxelMesh.faces[p].vertData.Length; i++)
                {
                    VertData vertData = voxelMesh.faces[p].GetVertData(i);
                    meshData.vertices.Add(pos + vertData.GetRotatedPosition(new int3(0, rot, 0)));
                    meshData.normals.Add(BlockData.nativeFaceChecks[p]);
                    AddTexture(World.Instance.blockTypes[voxel.id].GetTextureID(p, pos), vertData.uv);
                    faceVertCount++;
                }

                if (!type.renderNeighborFaces || LOD > 1)
                {
                    for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                    {
                        meshData.triangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                    }
                }
                else
                {
                    if (type.isFluid)
                    {
                        for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                        {
                            meshData.fluidTriangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < voxelMesh.faces[p].triangles.Length; i++)
                        {
                            meshData.transparentTriangles.Add(vertexIndex + voxelMesh.faces[p].triangles[i]);
                        }
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    void AddTexture(int textureID, float2 uv)
    {
        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        x += VoxelData.NormalizedBlockTextureSize * uv.x;
        y += VoxelData.NormalizedBlockTextureSize * uv.y;

        meshData.uvs.Add(new float2(x, y));
    }

    public VoxelState GetNeighbour(int3 pos)
    {

        if (map.ContainsKey(pos))
        {
            return map[pos];
        }
        else
        {
            return new();
        }
    }

    public VoxelType GetProperties(int id)
    {
        return BlockData.voxelTypes[id];
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        meshData.vertices.Clear();
        meshData.triangles.Clear();
        meshData.transparentTriangles.Clear();
        meshData.fluidTriangles.Clear();
        meshData.uvs.Clear();
        meshData.colors.Clear();
        meshData.normals.Clear();
    }
}

public struct PopulateJob : IJob
{
    [ReadOnly] public int3 position;
    [WriteOnly] public NativeParallelHashMap<int3, VoxelState> map;


    public void Execute()
    {
        for (int z = 0; z < VoxelData.ChunkSize; z++)
        {
            for (int x = 0; x < VoxelData.ChunkSize; x++)
            {
                (int,SubBiome) hab = World.GetHeightAndBiome(new int2(x + position.x, z + position.z));

                for (int y = 0; y < VoxelData.ChunkSize; y++)
                {
                    int3 mapos = new int3(x, y, z);

                    map.Add(mapos, new VoxelState
                    {
                        created = true,
                        id = World.GetVoxel(mapos+position, hab.Item1, hab.Item2),
                        orientation = 1,
                    });
                    
                    /*if(voxel.properties.isActive)
                    {
                        chunkData.chunk.AddActiveVoxel(voxel);
                    }*/
                    //int index = x + z * VoxelData.ChunkWidth + y * VoxelData.ChunkWidth * VoxelData.ChunkHeight;
                    //voxelInfos[index] = map[x, y, z].structInfo;
                }
            }
        }
    }
}