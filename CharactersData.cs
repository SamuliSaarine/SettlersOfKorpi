using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharactersData
{
    public CharacterData[] characters;

    public WhoIsInControl LoadCharacterData(GameObject player, int index)
    {
        GameObject character = GameObject.Instantiate(player);
        character.transform.Find("Character").position = characters[index].position;

        UIItemSlot[] slots = character.transform.GetComponentInChildren<Toolbar>().slots;

        for (int i = 0; i < slots.Length; i++)
        {
            ItemStack stack = characters[index].toolbar[i];
            if(stack.amount == 0)
            {
            }
            ItemSlot slot = new ItemSlot(slots[i], stack);
            if (stack.amount == 0)
            {
                slots[i].Clear();
            }
            else
            {
                Debug.Log(slot.stack.amount + "of" + slot.stack.id);
                slots[i].itemSlot = slot;
            }
        }
        character.transform.GetComponentInChildren<Toolbar>().slots = slots;

        return character.GetComponent<WhoIsInControl>();
    }

    public void ListCharacters(WhoIsInControl[] players)
    {
        characters = new CharacterData[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            characters[i] = CreateCharacterData(players[i]);
        }
    }

    public CharacterData CreateCharacterData(WhoIsInControl player)
    {
        CharacterData data = new();
        data.playercontrolled = player.playerControlled;
        data.position = player.transform.Find("Character").position;
        Toolbar toolbar = player.transform.GetComponentInChildren<Toolbar>();
        data.toolbar = new ItemStack[toolbar.slots.Length];
        for (int i = 0; i < toolbar.slots.Length; i++)
        {
            if(!toolbar.slots[i].HasItem)
            {
                data.toolbar[i] = null;
                continue;
            }
            Debug.Log(toolbar.slots[i].itemSlot.stack.amount + " of " + toolbar.slots[i].itemSlot.stack.id);
            Debug.Log(data.toolbar + " | " + toolbar.slots);
            data.toolbar[i] = toolbar.slots[i].itemSlot.stack;
        }

        return data;
    }
}

[System.Serializable]
public class CharacterData
{
    public bool playercontrolled;
    public Vector3 position;
    public ItemStack[] toolbar;
}
