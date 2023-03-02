using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

public class DebugScreen : MonoBehaviour
{
    public bool CheatDebugs;
    public bool DevDebugs;

    World world;
    Text text;
    public Player player;

    float frameRate;
    float timer;

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = gameObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = player.transform.position;
        Vector3 selectedPos = player.highlightBlock.position;

        string debugText = frameRate + " fps";
        debugText += "\n\n";
        debugText += "Seed:" + VoxelData.seed;
        debugText += $"\nChecking view distance: {World.Instance.checkingViewdistance}\n";
        debugText += $"{World.Instance.chunksWaitingUpdate} chunks waiting for update";
        if (CheatDebugs)
        {
            debugText += "\n\n";
            debugText += "Coordinates(m): " + GameCoordinateRounded(pos.x) + ", " + Mathf.FloorToInt(pos.y) / 8 + ", " + GameCoordinateRounded(pos.z) + "\n";
            debugText += $"Selected block: {player.HiglightedVoxel}({GameCoordinate(selectedPos.x, true):F1}, {GameCoordinate(selectedPos.y , false):F1}, {GameCoordinate(selectedPos.z, true):F1})\n";

            string direction = "";
            switch(world.controlledPlayer.orientation)
            {
                case 0:
                    direction = "North";
                    break;
                case 1:
                    direction = "South";
                    break;
                case 4:
                    direction = "West";
                    break;
                case 5:
                    direction = "East";
                    break;
            }
            debugText += "Facing: " + direction;
        }

        if(DevDebugs)
        {
            debugText += "\n\n";
            debugText += "XYZ(" + Mathf.FloorToInt(pos.x) + ", " + Mathf.FloorToInt(pos.y) + ", " + Mathf.FloorToInt(pos.z) + ")";
            debugText += "\n";
            debugText += "Chunk: " + world.playerChunkCoord.x + ", " + world.playerChunkCoord.y + ", " + world.playerChunkCoord.z;
            debugText += "\n";
            int3 voxelPos = world.worldData.GetVoxelCoord(player.highlightBlock.position);
            debugText += "Voxel(" + voxelPos.x + ", " + voxelPos.y + ", " + voxelPos.z +")";
        }

        text.text = debugText;

        if(timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }

    int GameCoordinateRounded(float realCoordinate)
    {
        return (Mathf.FloorToInt(realCoordinate) - VoxelData.WorldSizeInVoxels / 2) / 8;
    }

    float GameCoordinate(float realCoordinate, bool center)
    {
        float newCoord;

        if(center)
        {
            newCoord = GameCoordinateRounded(realCoordinate) + ((((Mathf.FloorToInt(realCoordinate) - VoxelData.WorldSizeInVoxels / 2) / 8f) - GameCoordinateRounded(realCoordinate)) * (4f / 5f));
        }
        else
        {
            newCoord = GameCoordinateRounded(realCoordinate) + (((Mathf.FloorToInt(realCoordinate) / 8f) - GameCoordinateRounded(realCoordinate)) * (4f / 5f));
        }

        return newCoord;
    }
}
