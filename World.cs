using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using MathF = UnityEngine.Mathf;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    /* PUBLIC */

    [Header("World Generation")]

    public Biome[] biomes;
    public Clouds clouds;
    [Range(0, 3)]
    public int async = 0;


    [Header("Player")]
    public ControllerManager controllerManager;

    public Player startPlayer;
    public Vector3 spawnPosition;
    public Player controlledPlayer;

    [Header("Lightning")]
    [Range(0, 1)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    [Header("Blocks")]
    public Material material;
    public Material transparentMaterial;
    public Material fluidMaterial;
    public BlockType[] blockTypes;
    public VoxelMeshData[] lodBlocks;

    public WorldData worldData;

    /* PRIVATE */

    [HideInInspector] public GameTime time;

    [HideInInspector]
    public PlayerSettings settings { get; private set; }
    public readonly Chunk[,,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldHeightInChunks, VoxelData.WorldSizeInChunks];
    readonly List<int3> activeChunks = new();

    public int highestCreatedHeight { get; private set; }

    public int3 playerChunkCoord;

    int3 playerLastChunkCoord;
    private List<Chunk> chunksToUpdate = new();
    private List<Structure> structuresToUpdate = new();
    public int chunksWaitingUpdate { get { return chunksToUpdate.Count; } }
    public List<Chunk> chunksToDraw = new();
    public List<Structure> structuresToDraw = new();

    bool generatingStructures = false;

    static readonly Queue<(int3, Tree)> modifications = new();
    [ReadOnly] static Biome[] _biomes;

    private static World _instance;
    public static World Instance { get { return _instance; } }

    public string appPath;

    private GameObject loadingPanel;

    public bool checkingViewdistance { get; private set; } = false;

    public bool worldReady { private set; get; }

    public bool destroyed = false;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        worldReady = false;

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<PlayerSettings>(jsonImport);

        appPath = Application.persistentDataPath;

        _biomes = biomes;
    }

    private void Start()
    {
        destroyed = false;

        InitVoxeltypes();

        worldData = SaveSystem.LoadWorld(VoxelData.worldName, VoxelData.seed);
        Random.InitState(worldData.seed);

        ChangePlayer(startPlayer);
        loadingPanel.SetActive(true);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        SetupLighting();
        spawnPosition = new Vector3(VoxelData.WorldCenter, VoxelData.WorldCenter + 8f, VoxelData.WorldCenter);
        spawnPosition = new Vector3(spawnPosition.x, SpawnHeight(spawnPosition)+10, spawnPosition.z);
        foreach(WhoIsInControl p in controllerManager.players)
        {
            Vector3 area = new(Random.Range(-8, 8), 0, Random.Range(-8, 8));
            p.player.transform.position = spawnPosition+area;
        }
        controllerManager.SpawnPlayers();
        playerChunkCoord = RoundToInt3(controlledPlayer.transform.position/VoxelData.ChunkSize);
        //GenerateWorld();

        time = worldData.time;
    }

    public void InitVoxeltypes()
    {
        for (int i = 0; i < blockTypes.Length; i++)
        {
            BlockData.voxelTypes[i] = blockTypes[i].voxelType;
        }
    }

    public void ChangePlayer(Player controlled)
    {
        controlledPlayer = controlled;
        Debug.Log("Playerscript found");
        loadingPanel = controlledPlayer.loadingPanel;
    }

    public void SetupLighting()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        controlledPlayer.GetComponentInChildren<Camera>().backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }


    private void Update()
    {
        if (!checkingViewdistance)
        {
            playerChunkCoord = RoundToInt3(controlledPlayer.transform.position / VoxelData.ChunkSize);

            if (!playerChunkCoord.Equals(playerLastChunkCoord))
            {
                playerLastChunkCoord = playerChunkCoord;
                CheckViewDistance();
            }

            if (!generatingStructures)
            {
                GenerateStructures();
            }
        }

        if (chunksToUpdate.Count > 0)
        {
            UpdateChunks();     
        }

        if (structuresToUpdate.Count > 0)
        {
            UpdateStructures();
        }

        if (chunksToDraw.Count > 0)
        {
            chunksToDraw[0].CreateMesh();
            chunksToDraw.RemoveAt(0);
        }

        if (structuresToDraw.Count > 0)
        {
            structuresToDraw[0].CreateMesh();
            structuresToDraw.RemoveAt(0);
        }
    }

    async void LoadWorld()
    {
        float start = Time.realtimeSinceStartup;

        for (int x = (VoxelData.WorldSizeInChunks / 2) - (settings.viewDistance+settings.loadDistance); x < (VoxelData.WorldSizeInChunks / 2) + (settings.viewDistance + settings.loadDistance); x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - (settings.viewDistance + settings.loadDistance); z < (VoxelData.WorldSizeInChunks / 2) + (settings.viewDistance + settings.loadDistance); z++)
            {
                for (int y = (VoxelData.WorldHeightInChunks / 2) - (settings.viewDistance + settings.loadDistance); y < (VoxelData.WorldHeightInChunks / 2) + (settings.viewDistance + settings.loadDistance); y++)//((VoxelData.WorldHeightInChunks / 2 <= settings.loadDistance) ? 0 : ((VoxelData.WorldHeightInChunks / 2) - settings.loadDistance)); y < ((VoxelData.WorldHeightInChunks / 2 <= settings.loadDistance) ? VoxelData.WorldHeightInChunks : ((VoxelData.WorldHeightInChunks / 2) + settings.loadDistance)); y++)
                {
                    int3 pos = new(x, y, z);

                    if(IsChunkInWorld(pos))
                    {
                        worldData.LoadChunk(pos);
                    }

                    await Task.Yield();
                }
            }
        }

        Debug.Log("Loading time: " + (Time.realtimeSinceStartup - start).ToString("F3"));
    }

    void GenerateWorld()
    {
        //LoadWorld();
        CheckViewDistance();
        Debug.Log("World ready");
    }

    void UpdateChunks()
    {
        chunksToUpdate[0].UpdateChunk();
        if(!activeChunks.Contains(chunksToUpdate[0].coord))
        {
            activeChunks.Add(chunksToUpdate[0].coord);
        }
        chunksToUpdate.RemoveAt(0);
    }

    void UpdateStructures()
    {
        structuresToUpdate[0].UpdateStructure();
        structuresToUpdate.RemoveAt(0);
    }

    void GenerateStructures()
    {
        generatingStructures = true;

        while(modifications.Count > 0)
        {
            var input = modifications.Dequeue();
            StructureData data = StructureGeneration.Tree(input.Item1, input.Item2);
            worldData.CreateStructure(data);
        }
        generatingStructures = false;
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkSize);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkSize);

        return chunks[x, y, z];
    }

    void CheckViewDistance()
    {
        checkingViewdistance = true;

        //float start = Time.realtimeSinceStartup;
        int3 coord = RoundToInt3(controlledPlayer.transform.position/VoxelData.ChunkSize);
        clouds.UpdateClouds();

        List<int3> previouslyActiveChunks = new(activeChunks);       

        activeChunks.Clear();

        int chunkAmount = 0; 
        if(!worldReady)
        {
            chunkAmount = (int)MathF.Pow(settings.viewDistance * 2, 3);
        }

        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            /*if (async == 1)
            {
                await Task.Yield();
            }*/

            int xdis = MathF.Abs(x - coord.x);

            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {

                /*if (async == 2)
                {
                    await Task.Yield();
                }*/

                int zdis = MathF.Abs(z - coord.z);

                for (int y = coord.y - settings.viewDistance; y < coord.y + settings.viewDistance; y++)
                {
                    if (destroyed)
                    {
                        break;
                    }

                    /*if (async == 3)
                    {
                        await Task.Yield();
                    }*/

                    int3 thisCoord = new(x, y, z);   

                    if (IsChunkInWorld(thisCoord))
                    {
                        int LOD = CalculateLOD(xdis, MathF.Abs(y - coord.y), zdis);

                        if (chunks[x, y, z] == null)
                        {                           
                            chunks[x, y, z] = new Chunk(thisCoord, LOD);

                            for (int p = 0; p < 6; p++)
                            {
                                int3 n = new int3(x, y, z) + BlockData.nativeFaceChecks[p];
                                if(IsChunkInWorld(new int3(n.x,n.y,n.z)) && chunks[n.x, n.y, n.z]!=null)
                                {
                                    AddChunkToUpdate(chunks[n.x, n.y, n.z]);
                                }
                            }
                        }
                        else if(chunks[x, y, z].lod != LOD)
                        {
                            chunks[x, y, z].lod = LOD;
                            AddChunkToUpdate(chunks[x, y, z]);
                        }                    

                        chunks[x, y, z].IsActive = true;
                        activeChunks.Add(thisCoord);
                    }

                    for (int i = 0; i < previouslyActiveChunks.Count; i++)
                    {
                        if (previouslyActiveChunks[i].Equals(thisCoord))
                        {
                            previouslyActiveChunks.RemoveAt(i);
                        }
                    }

                    if(!worldReady)
                    {
                        chunkAmount--;
                        loadingPanel.GetComponent<LoadingScreen>().UpdateSlider(chunkAmount);
                    }
                }
            }
        }

        foreach(int3 c in previouslyActiveChunks)
        {
            chunks[c.x, c.y, c.z].IsActive = false;          
        }

        if(!worldReady)
        {
            worldReady = true;
            loadingPanel.SetActive(false);
            StartCoroutine(Tick());
        }
        //Debug.Log("Update time: " + (Time.realtimeSinceStartup - start).ToString("F3"));
        checkingViewdistance = false;
    }

    int CalculateLOD(int xDistance, int yDistance, int zDistance)
    {
        int distance = xDistance;
        if(distance<yDistance)
        {
            distance = yDistance;
        }
        if(distance<zDistance)
        {
            distance = zDistance;
        }

        int lod;


        if(distance>8)
        {
            lod = 2;
        }
        else if(distance>4)
        {
            lod = 1;
        }
        else
        {
            lod = 0;
        }

        return lod;
    }

    int SpawnHeight(Vector3 pos)
    {
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        int2 v2 = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));

        for (int i = 0; i < _biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(v2, _biomes[i].offset, _biomes[i].scale);

            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }
        }

        Biome biome = _biomes[strongestBiomeIndex];
        SubBiome subBiome = biome.GetSubBiome(v2);

        return Mathf.FloorToInt(biome.minHeight + Noise.Get2DPerlin(v2, 0, biome.scale) * biome.heightRange
             + Noise.Get2DPerlin(v2, subBiome.octave1Offset, subBiome.octave1Scale) * subBiome.octave1Range
             + Noise.Get2DPerlin(v2, subBiome.octave2Offset, subBiome.octave2Scale) * subBiome.octave2Range); ;
    }

    IEnumerator Tick()
    {
        while(true)
        {
            yield return new WaitForSeconds(VoxelData.tickLength);

            foreach (int3 c in activeChunks)
            {
                chunks[c.x, c.y ,c.z].TickUpdate();
            }

            foreach (WhoIsInControl player in controllerManager.players)
            {
                player.player.TickUpdate();
            }            

            time.Add(1);
        }
    }

    public static (int,SubBiome) GetHeightAndBiome(int2 pos)
    {
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;
        int secondStrongestBiomeIndex = 0;

        if (_biomes.Length > 1)
        {
            for (int i = 1; i < _biomes.Length; i++)
            {
                float weight = Noise.Get2DPerlin(pos, _biomes[i].offset, _biomes[i].scale);

                if (weight >= _biomes[i].treshold)
                {
                    secondStrongestBiomeIndex = strongestBiomeIndex;
                    strongestBiomeIndex = i;
                    strongestWeight = weight;
                }

            }
        }

        Biome biome = _biomes[strongestBiomeIndex];
        SubBiome subBiome = biome.GetSubBiome(pos);

        int terrainHeight = Mathf.FloorToInt(biome.minHeight + Noise.Get2DPerlin(pos, 0, biome.terrainScale) * biome.heightRange
    + Noise.Get2DPerlin(pos, subBiome.octave1Offset, subBiome.octave1Scale) * subBiome.octave1Range);

        if (secondStrongestBiomeIndex != strongestBiomeIndex)
        {
            Biome biome2 = _biomes[secondStrongestBiomeIndex];
            SubBiome subBiome2 = biome2.GetSubBiome(pos);

            // Interpolate between the terrain heights of the two biomes
            float t = strongestWeight / (1 - biome.treshold) - biome.treshold / (1 - biome.treshold);

            float height2 = biome2.minHeight + Noise.Get2DPerlin(pos, 0, biome2.terrainScale) * biome2.heightRange
                + Noise.Get2DPerlin(pos, subBiome2.octave1Offset, subBiome2.octave1Scale) * subBiome2.octave1Range;

            terrainHeight = MathF.FloorToInt(MathF.Lerp(height2, terrainHeight, t));
        }

        terrainHeight += Mathf.FloorToInt(Noise.Get2DPerlin(pos, subBiome.octave2Offset, subBiome.octave2Scale) * subBiome.octave2Range);

        return new (terrainHeight, subBiome);
    }

    public static byte GetVoxel(int3 pos, int terrainHeight, SubBiome subBiome)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        //Return air outside world
        if(!VoxelData.IsVoxelInWorld(pos))
        {
            return 0;
        }

        int2 v2 = new(pos.x, pos.z);



        byte voxelValue;

        //Firs Pass

        if (yPos == terrainHeight)
        {
            if (yPos > VoxelData.seaLevel)
            {
                voxelValue = subBiome.surfaceBlock;
            }
            else
            {
                voxelValue = subBiome.subSurfaceBlock;
            }
            
        } 
        else if(yPos < terrainHeight && yPos > terrainHeight - subBiome.subSurfaceDepth)
        {
            voxelValue = subBiome.subSurfaceBlock;
        }
        else if(yPos > terrainHeight)
        {
            if (yPos <= VoxelData.seaLevel)
            {
                voxelValue = 2;
            }
            else
            {
                voxelValue = 0;
            }
        }
        else
        {
            voxelValue = 1;
            //Stone
        }

        //Lode pass
        for(int i=0; i<subBiome.lodes.Length; i++)
        {
            //Caves and holes
            if(yPos > subBiome.lodes[i].minHeight && yPos < subBiome.lodes[i].maxHeight)
            {
                if (Noise.Get3DPerlin(pos, 0, subBiome.lodes[i].scale, subBiome.lodes[i].threshold))
                {
                    voxelValue = subBiome.lodes[i].blockID;
                }
            }
        }

        //Tree pass
        if (yPos == terrainHeight && voxelValue != 0 && subBiome.trees.Length > 0 && yPos > VoxelData.seaLevel+1)
        {
            if(pos.x < 20 || pos.x > VoxelData.WorldSizeInVoxels - 20 || pos.z < 20 || pos.z > VoxelData.WorldSizeInVoxels - 20 || yPos < 4 || yPos > VoxelData.WorldHeightInVoxels - subBiome.highestTreeHeight-8)
            {
                return 0;
            }

            for (int i = 0; i < subBiome.trees.Length; i++)
            {
                if (VoxelData.random.NextFloat() < subBiome.trees[i].probability)
                {
                    modifications.Enqueue((pos, subBiome.trees[i]));
                    break;
                }
            }
        }

        //Undergrowth pass
        if(yPos == terrainHeight+1 && voxelValue == 0 && subBiome.undergrowth.Length>0)
        {
            for (int i = 0; i < subBiome.undergrowth.Length; i++)
            {                
                if (Noise.Get2DPerlin(v2, subBiome.undergrowth[i].zoneOffset, subBiome.undergrowth[i].zoneScale) > subBiome.undergrowth[i].zoneTreshold)
                {
                    if (VoxelData.random.NextFloat() < subBiome.undergrowth[i].placementProbability)
                    {
                        voxelValue = subBiome.undergrowth[i].blockId;
                        break;
                    }
                }
            }
        }

        return voxelValue;
    }

    public bool IsChunkInWorld(int3 coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.y >= 0 && coord.y < VoxelData.WorldHeightInChunks && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddChunkToUpdate(Chunk chunk, bool insert=false)
    {
        if(!chunksToUpdate.Contains(chunk))
        {
            if(insert)
            {
                chunksToUpdate.Insert(0, chunk);
            }
            else
            {
                chunksToUpdate.Add(chunk);
            }
        }
    }

    public void AddStructureToUpdate(Structure structure, bool insert = false)
    {
        if (!structuresToUpdate.Contains(structure))
        {
            if (insert)
            {
                structuresToUpdate.Insert(0, structure);
            }
            else
            {
                structuresToUpdate.Add(structure);
            }
        }
    }

    public int3 RoundToInt3(Vector3 vector)
    {
        return new int3(MathF.FloorToInt(vector.x), MathF.FloorToInt(vector.y), MathF.FloorToInt(vector.z));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        foreach(ChunkData data in worldData.chunks.Values)
        {
            data.Dispose();
        }

        foreach (StructureData data in worldData.structures.Values)
        {
            data.Dispose();
        }

        worldData.chunks.Clear();

        BlockData.Dispose();

        destroyed = true;
    }
}

[System.Serializable]
public struct BlockType
{
    public string blockName;
    public VoxelType voxelType;
    public VoxelMeshData meshData;
    public AudioClip stepSound;
    public Sprite icon;
    public int stackSize;

    [Header("Texture Values")]

    [Tooltip("Put value only to backFaceTexture")]
    public bool everyFaceIsSame;

    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureID(int faceIndex, int3 pos)
    {

        if(everyFaceIsSame)
        {
            return Variant(backFaceTexture, pos, faceIndex);
        }
        else
        {
            switch (faceIndex)
            {
                case 0:
                    return Variant(backFaceTexture, pos, faceIndex);
                case 1:
                    return Variant(frontFaceTexture, pos, faceIndex);
                case 2:
                    return Variant(topFaceTexture, pos, faceIndex);
                case 3:
                    return Variant(bottomFaceTexture, pos, faceIndex);
                case 4:
                    return Variant(leftFaceTexture, pos, faceIndex);
                case 5:
                    return Variant(rightFaceTexture, pos, faceIndex);
                default:
                    Debug.Log("Error in GetTextureId, invalid face index");
                    return 0;
            }
        }
    }

    int Variant(int first, int3 pos, int faceIndex)
    {
        int variantCount = VoxelData.VariantCounts(first);

        if(variantCount>1)
        {
            int random = MathF.Clamp(MathF.RoundToInt(variantCount*Noise.Get2DPerlin(new int2(pos.x, pos.z), pos.y, 30f)), 0, variantCount-1);

            return first + Mathf.FloorToInt(random*meshData.faces[faceIndex].GetVertData(2).uv.x); 
            //VoxelData.random.NextInt(first, first+variantCount);
        }
        else
        {
            return first;
        }
    }
}

[System.Serializable]
public struct VoxelType
{
    public byte solidity;
    public bool isFluid;
    public bool isActive;
    public bool renderNeighborFaces;
    public byte opacity;
    public bool drops;
}

public struct VoxelMod
{
    public int3 pos;
    public byte id;
    public byte orientation;
}

[System.Serializable]
public struct VoxLight
{
    public Vector4 color;
    public Vector3 direction;
    public float range;
    public Vector3 position;
    public float spotAngle;
    public Vector3 attenuation;
}

public struct GameTime
{
    public int days;
    public int hours;
    public int seconds;

    public static GameTime operator -(GameTime newTime, GameTime oldTime)
    {
        int s = newTime.seconds - oldTime.seconds;
        int h = newTime.hours - oldTime.hours;
        int d = newTime.days - oldTime.days;

        if(s<0)
        {
            s += 64;
            h -= 1;
        }

        if(h<0)
        {
            h += 16;
            d -= 1;
        }

        return new GameTime
        {
            seconds = s,
            hours = h,
            days = d
        };
    }

    public void Add(int _seconds)
    {
        seconds += _seconds;
        hours += MathF.FloorToInt(seconds / 64);
        if(hours >= 16)
        {
            days += MathF.FloorToInt(hours / 16);
            hours %= 16;
        }
    }
}