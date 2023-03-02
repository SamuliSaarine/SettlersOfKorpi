using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

public class Clouds : MonoBehaviour
{
    public int cloudHeight = 100;
    public int cloudDepth = 2;

    [SerializeField] private Texture2D cloudPattern = null;
    [SerializeField] private Material cloudMaterial = null;
    [SerializeField] private World world = null;
    bool[,] cloudData;

    int cloudTexWidth;

    int cloudTileSize;
    Vector3Int offset;
    private readonly Dictionary<Vector2Int, GameObject> clouds = new();

    private void Start()
    {
        cloudTexWidth = cloudPattern.width;
        cloudTileSize = VoxelData.ChunkSize;
        offset = new Vector3Int(-(cloudTexWidth / 2), 0, -(cloudTexWidth / 2));

        transform.position = new Vector3(VoxelData.WorldCenter, cloudHeight, VoxelData.WorldCenter);

        LoadCloudData();
        CreateClouds();
    }

    private void LoadCloudData()
    {
        cloudData = new bool[cloudTexWidth, cloudTexWidth];
        Color[] cloudTex = cloudPattern.GetPixels();

        //Loop through colorarray and sets opacity depending on opacity of color
        for (int x = 0; x < cloudTexWidth; x++)
        {
            for (int y = 0; y < cloudTexWidth; y++)
            {
                cloudData[x, y] = (cloudTex[y * cloudTexWidth + x].a > 0);
            }
        }
    }

    private void CreateClouds()
    {
        if(world.settings == null)
        {
            Debug.Log("World null");
        }

        if (world.settings.clouds == CloudStyle.Off)
        {
            return;
        }


        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTexWidth; y += cloudTileSize)
            {
                Mesh cloudMesh;
                if(world.settings.clouds == CloudStyle.TwoD)
                {
                    cloudMesh = Create2DCloudMesh(x, y);
                }
                else
                {
                    cloudMesh = Create3DCloudMesh(x, y);
                }

                Vector3 pos = new(x, cloudHeight, y);
                pos += transform.position - new Vector3(cloudTexWidth / 2f, 0f, cloudTexWidth / 2f);
                clouds.Add(CloudTilePosFromV3(pos), CreateCloudTile(cloudMesh, pos));

            }
        }
    }

    public void UpdateClouds()
    {
        if (world.settings.clouds == CloudStyle.Off)
        {
            return;
        }

        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTexWidth; y += cloudTileSize)
            {
                Vector3 pos = world.controlledPlayer.transform.position + new Vector3(x, 0, y) + offset;
                pos = new Vector3(RoundToCloud(pos.x), cloudHeight, RoundToCloud(pos.z));
                Vector2Int cloudPos = CloudTilePosFromV3(pos);

                clouds[cloudPos].transform.position = pos;
            }
        }
    }

    private int RoundToCloud(float value)
    {
        return Mathf.FloorToInt(value / cloudTileSize) * cloudTileSize;
    }

    private Mesh Create2DCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; xIncrement++)
        {
            for (int zIncrement = 0; zIncrement < cloudTileSize; zIncrement++)
            {
                int xVal = x + xIncrement;
                int zVal = z + zIncrement;

                if(cloudData[xVal, zVal])
                {
                    vertices.Add(new Vector3(xIncrement, 0, zIncrement));
                    vertices.Add(new Vector3(xIncrement, 0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement));

                    for (int i = 0; i < 4; i++)
                    {
                        normals.Add(Vector3.down);
                    }

                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 2);

                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;
                }
            }
        }

        Mesh mesh = new();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    private Mesh Create3DCloudMesh(int x, int z)
    {
        List<int3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; xIncrement++)
        {
            for (int zIncrement = 0; zIncrement < cloudTileSize; zIncrement++)
            {
                int xVal = x + xIncrement;
                int zVal = z + zIncrement;

                if (cloudData[xVal, zVal])
                {
                    for (int p = 0; p < 6; p++)
                    {
                        if (!CheckCloudData(new int3(xVal, 0, zVal) + BlockData.nativeFaceChecks[p]))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                int3 vert = new int3(xIncrement, 0, zIncrement);
                                vert += BlockData.voxelVerts[BlockData.voxelTris[p * 4 + i]];
                                vert.y *= cloudDepth;
                                vertices.Add(vert);
                            }

                            for (int i = 0; i < 4; i++)
                            {
                                normals.Add(new Vector3(BlockData.nativeFaceChecks[p].x, BlockData.nativeFaceChecks[p].y, BlockData.nativeFaceChecks[p].z));
                            }
                            triangles.Add(vertCount);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 3);

                            vertCount += 4;
                        }
                    }
                }
            }
        }

        Mesh mesh = new();
        mesh.vertices = vertices.ToArray().Select(vertex => new Vector3(vertex.x, vertex.y, vertex.z)).ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    bool CheckCloudData(int3 point)
    {
        if(point.y != 0)
        {
            return false;
        }

        int x = point.x;
        int z = point.z;

        if(point.x < 0) x = cloudTexWidth - 1;
        if(point.x > cloudTexWidth - 1) x = 0;

        if (point.z < 0) z = cloudTexWidth - 1;
        if (point.z > cloudTexWidth - 1) z = 0;

        return cloudData[x, z];
    }

    private GameObject CreateCloudTile(Mesh mesh, Vector3 pos)
    {
        GameObject newCloudTile = new();
        newCloudTile.transform.position = pos;
        newCloudTile.transform.parent = transform;
        newCloudTile.name = "Cloud(" + pos.x + "," + pos.z + ")";
        MeshFilter mf = newCloudTile.AddComponent<MeshFilter>();
        MeshRenderer mr = newCloudTile.AddComponent<MeshRenderer>();

        mr.material = cloudMaterial;
        mf.mesh = mesh;

        return newCloudTile;

    }

    private Vector2Int CloudTilePosFromV3(Vector3 pos)
    {
        return new Vector2Int(CloudTileCoordFromFloat(pos.x), CloudTileCoordFromFloat(pos.z));
    }

    private int CloudTileCoordFromFloat(float value)
    {
        float a = value / cloudTexWidth;
        a -= Mathf.FloorToInt(a);
        int b = Mathf.FloorToInt(cloudTexWidth * a);

        return b / VoxelData.ChunkSize;
    }
}

public enum CloudStyle
{
    Off, TwoD, ThreeD
}
