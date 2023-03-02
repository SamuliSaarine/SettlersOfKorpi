using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerManager : MonoBehaviour
{
    public WhoIsInControl[] players;
    [HideInInspector]
    public CharactersData charactersData;
    [HideInInspector]
    public int controlledPlayer = 0;

    private float cooldown = 0.2f;
    private bool readyToChange = true;

    public void SpawnPlayers()
    {
        CharactersData charactersData = SaveSystem.LoadCharacters(VoxelData.worldName);
        if(charactersData == null)
        {
            Debug.Log("Not characterdata found");
            return;
        }

        GameObject player = players[0].gameObject;
        foreach(WhoIsInControl p in players)
        {
            Destroy(p.gameObject);
        }

        int index = 0;

        players = new WhoIsInControl[charactersData.characters.Length];

        for (int i = 0; i < charactersData.characters.Length; i++)
        {
            players[i] = charactersData.LoadCharacterData(player, i);
            if(players[i].playerControlled)
            {
                index = i;
            }
        }

        NewControlled(index);

        Destroy(player);
    }

    public void ChangeControlled(int nextController)
    {
        if (!readyToChange) return;

        int newController = controlledPlayer + nextController;
        if(newController < 0)
        {
            newController = players.Length + nextController;
        }
        else if(newController >= players.Length)
        {
            newController -= players.Length;
        };

        NewControlled(newController);
    }

    void NewControlled(int newController)
    {
        controlledPlayer = newController;

        foreach (WhoIsInControl player in players)
        {
            if (player != players[controlledPlayer])
            {
                player.ChangeController(false);
            }
        }

        players[controlledPlayer].ChangeController(true);
        World.Instance.ChangePlayer(players[controlledPlayer].transform.Find("Character").GetComponent<Player>());

        readyToChange = false;
        Invoke(nameof(SetReady), cooldown);
    }

    void SetReady()
    {
        readyToChange = true;
    }

    public void PlayerDied(Transform parent)
    {
        int index = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].transform == parent)
            {
                index = i;
                break;
            }
        }

        if (index == controlledPlayer)
        {
            ChangeControlled(-1);
        }

        Destroy(players[index].gameObject);
        players[index] = null;
        RebuildPlayers();
    }

    void RebuildPlayers()
    {
        List<WhoIsInControl> list = new();
        int x = 0;
        bool controllerFound = false;
        for (int i = 0; i < players.Length; i++)
        {
            if(players[i]!=null)
            {
                if(i==controlledPlayer && !controllerFound)
                {
                    controlledPlayer = x;
                    controllerFound = true;
                }
                list.Add(players[i]);
                if(!controllerFound)
                {
                    x++;
                }
            }
        }
        players = list.ToArray();
    }
}
