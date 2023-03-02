using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhoIsInControl : MonoBehaviour
{
    public bool playerControlled;

    public GameObject[] onlyPlayer;
    public Player player;

    private void Start()
    {

        ChangeController(playerControlled);
    }

    public void ChangeController(bool _playerControlled)
    {
        playerControlled = _playerControlled;

        Debug.Log(gameObject.name + ": playercontrolled " + playerControlled);
        player.playerControlled = playerControlled;

        foreach (GameObject playerObject in onlyPlayer)
        {
            playerObject.SetActive(playerControlled);
        }
    }
}
