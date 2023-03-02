using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Unity.Mathematics;

public static class SaveSystem
{
    public static int chunksToSave;
    public static int unsavedChunksLeft;

    public static void SaveWorld (WorldData world)
    {
        string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        Debug.Log("Saving " + world.worldName + " to " + savePath);

        BinaryFormatter formatter = new();
        FileStream stream = new(savePath + world.worldName + ".world", FileMode.Create);

        formatter.Serialize(stream, world);
        stream.Close();

        SaveCharacters(world.worldName);

        bool threadChunksaving = true;

        if(threadChunksaving)
        {
            Thread thread = new(() => SaveChunks(world));
            thread.Start();
        }
        else
        {
            SaveChunks(world);
        }
    }

    public static WorldData LoadWorld(string worldName, int seed)
    {
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        if(File.Exists(loadPath + worldName + ".world"))
        {
            Debug.Log(worldName + " found. Loading save");

            BinaryFormatter formatter = new();
            FileStream stream = new(loadPath + worldName + ".world", FileMode.Open);

            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();
            return new WorldData(world);
        }
        else
        {
            Debug.Log(worldName + " not found. Creating a new world.");
            WorldData world = new(worldName, seed, new GameTime {days=0, hours=0, seconds=0 });
            //SaveWorld(world);

            return world;
        }
    }

    public static void SaveChunks(WorldData world)
    {
        List<ChunkData> chunks = new(world.modifiedChunks);
        world.modifiedChunks.Clear();

        chunksToSave = chunks.Count;
        unsavedChunksLeft = chunksToSave;
        foreach (ChunkData chunk in chunks)
        {
            SaveChunk(chunk, world.worldName);
            unsavedChunksLeft--;
        }

        Debug.Log(chunksToSave + " chunks saved");
    }

    public static void SaveChunk(ChunkData chunk, string worldName)
    {
        string chunkName = chunk.position.x / VoxelData.ChunkSize + "-" + chunk.position.y / VoxelData.ChunkSize + "-" + chunk.position.z / VoxelData.ChunkSize;
        Debug.Log(chunkName);

        string savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new();
        FileStream stream = new(savePath + chunkName + ".chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    public static ChunkData LoadChunk(string worldName, int3 position)
    {
        string chunkName = position.x / VoxelData.ChunkSize + "-" + position.y / VoxelData.ChunkSize + "-" + position.z / VoxelData.ChunkSize;

        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new();
            FileStream stream = new(loadPath, FileMode.Open);

            ChunkData chunk = formatter.Deserialize(stream) as ChunkData;
            stream.Close();
            return chunk;
        }

        return null;
    }

    public static void SaveCharacters(string worldName)
    {
        ControllerManager manager = World.Instance.controllerManager;
        manager.charactersData.ListCharacters(manager.players);

        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        string jsonExport = JsonUtility.ToJson(manager.charactersData);
        File.WriteAllText(loadPath + "characters.cfg", jsonExport);
    }

    public static CharactersData LoadCharacters(string worldName)
    {
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        if (File.Exists(loadPath + "characters.cfg"))
        {
            string jsonImport = File.ReadAllText(loadPath + "characters.cfg");
            return JsonUtility.FromJson<CharactersData>(jsonImport);
        }
        else
        {
            return null;
        }
    }
}
