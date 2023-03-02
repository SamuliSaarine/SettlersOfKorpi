using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour, Kontrollit.Kontrollit.IPlayerActions
{
    /* PUBLIC */
    [HideInInspector]
    public bool playerControlled;

    public Stats stats;

    [Header("Movement")]
    public Transform cam;
    public Movement movement;

    [Header("Interacting")]
    public int orientation;
    public Transform highlightBlock;
    public Transform placeBlock;
    public int amount = 4;
    public float checkIncrement = 0.1f;
    public float reach = 16f;

    public Toolbar toolbar;

    public string HiglightedVoxel { get; private set; }

    [Header("InventorySystem")]
    public GameObject backpack;
    public GameObject cursorSlot;

    [Header("Debug")]
    public GameObject debugScreen;
    public int rested=8*64;
    public int full=4*64;
    private bool _inUI = false;

    [Header("UI")]
    public GameObject menu;
    public GameObject loadingPanel;
    public GameObject savingText;

    /* PRIVATE */
    ControllerManager controllerManager;
    World world;

    //Movement


    public PlayerSettings settings;

    private float energyClamp = 1f;

    private void Awake()
    {
        controllerManager = GameObject.Find("PlayerControllerManager").GetComponent<ControllerManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<PlayerSettings>(jsonImport);

        InUI = false;

        highlightBlock.localScale *= amount;
        placeBlock.localScale *= amount;

        world = World.Instance;
    }

    public void TickUpdate()
    {
       if(rested>0)
       {
            rested -= 1;
            if (stats.tired.activeSelf)
            {
                stats.tired.SetActive(false);
            }
        }
       else if(stats.energySlider.value>0)
       {
            stats.ChangeEnergy(-1f/64);
            energyClamp -= 1f / (64*16);
            if(!stats.tired.activeSelf)
            {
                stats.tired.SetActive(true);
            }
       }

       if(full>0)
       {
            full -=1;
            if(stats.energySlider.value<energyClamp) stats.ChangeEnergy(0.5f/64);
            if (stats.hungry.activeSelf)
            {
                stats.hungry.SetActive(false);
            }
        }
       else if(stats.energySlider.value>0)
       {
            stats.ChangeEnergy(-1f / (64*16));
            if (!stats.hungry.activeSelf)
            {
                stats.hungry.SetActive(true);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(transform.forward + " | " + BackSlope);

        if (loadingPanel.activeSelf) return;

        if(playerControlled)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                backpack.SetActive(false);
                menu.SetActive(!menu.activeSelf);
                InUI = menu.activeSelf;
            }

            if (!InUI)
            {
                GetPlayerInputs();
                PlaceBlocks();
                //movement.GetMovementInput();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                SaveGame();
            }

            Vector3 XZDirection = transform.forward;
            XZDirection.y = 0;
            if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
            {
                orientation = 0;
            }
            else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
            {
                orientation = 5;
            }
            else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
            {
                orientation = 1;
            }
            else
            {
                orientation = 4;
            }
        }
        else
        {
            //AI stuff upcoming
        }

    }

    void GetPlayerInputs()
    {
        if(highlightBlock.gameObject.activeSelf)
        {
            if(amount > 1 && Input.GetKeyDown(KeyCode.DownArrow))
            {
                amount /= 2;
                highlightBlock.localScale /= 2;
                placeBlock.localScale /= 2;
            }

            if (amount < 4 && Input.GetKeyDown(KeyCode.UpArrow))
            {
                amount *= 2;
                highlightBlock.localScale *= 2;
                placeBlock.localScale *= 2;
            }

            //Destroy
            if (Input.GetMouseButtonDown(0))
            {
                //Debug.Log(highlightBlock.position);
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0, amount);
                //int3 i3pos = new int3(Mathf.FloorToInt(highlightBlock.position.x / 32f), Mathf.FloorToInt(highlightBlock.position.y / 32f), Mathf.FloorToInt(highlightBlock.position.z / 32f));
                //Debug.Log(world.worldData.RequestChunck(i3pos, false).position + " -> " + world.worldData.RequestChunck(i3pos + BlockData.nativeFaceChecks[orientation], false).position);
            }

            //Place
            if (Input.GetMouseButtonDown(1))
            {
                if(toolbar.slots[toolbar.slotIndex].HasItem && toolbar.slots[toolbar.slotIndex].itemSlot.stack.amount >= Mathf.Pow(amount, 3))
                {
                    world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id, amount);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(Mathf.FloorToInt(Mathf.Pow(amount, 3)));
                }
            }

            if(Input.GetKeyDown(KeyCode.E))
            {
                if(world.worldData.GetVoxel(highlightBlock.position).id==10)
                {
                    world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 11, 1);
                    if(full<64*16)
                    {
                        full += 16;
                    }
                }
            }
        }
    }

    void PlaceBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new();
         
        if(Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 16f))
        {   
            if(Input.GetMouseButtonDown(0))
            {
                hit.collider.gameObject.GetComponent<Player>()?.Damage();
            }
        }
        else
        {
            while (step < reach)
            {
                Vector3 pos = cam.position + (cam.forward * step);
                pos = new Vector3(Mathf.FloorToInt(pos.x / amount) * amount, Mathf.FloorToInt(pos.y / amount) * amount, Mathf.FloorToInt(pos.z / amount) * amount);

                if (CheckForVoxel(pos,false, movement.isCrouching ? 2 : 1))
                {
                    highlightBlock.position = pos;
                    highlightBlock.gameObject.SetActive(true);
                    if(debugScreen.activeSelf) HiglightedVoxel = VoxelName(highlightBlock.position).ToString();

                    if (CheckForVoxel(pos, false, 2))
                    {
                        placeBlock.position = lastPos;
                        placeBlock.gameObject.SetActive(true);
                    }

                    return;
                }

                lastPos = pos;

                step += checkIncrement;
            }
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }


    public string VoxelName(Vector3 pos)
    {
        if (CheckForVoxel(pos))
        {
            return world.blockTypes[world.worldData.GetVoxel(pos).id].blockName;
        }
        else
        {
            return string.Empty;
        }

    }


    public void ExitGame()
    {
        InUI = true;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }



    public void OnInventory(InputAction.CallbackContext context)
    {
        menu.SetActive(false);
        backpack.SetActive(!backpack.activeSelf);
        InUI = backpack.activeSelf;
    }

    public void OnDebugScreen(InputAction.CallbackContext context)
    {
        debugScreen.SetActive(!debugScreen.activeSelf);
    }

    public void OnNextCharacter(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            Debug.Log("Next player");
            controllerManager.ChangeControlled(1);
        }
    }

    public void OnPrevCharacter(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            Debug.Log("Previous player");
            controllerManager.ChangeControlled(-1);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            transform.localScale = new Vector3(3.2f, 1.6f, 3.2f);
            movement.isCrouching = true;
        }
        else if (context.canceled && movement.CheckUpSpeed(-1.5f) != 0)
        {
            transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
            movement.isCrouching = false;
        }
    }

    public void SaveGame()
    {
        savingText.SetActive(true);
        world.worldData.time = world.time;
        SaveSystem.SaveWorld(World.Instance.worldData);
    }

    public void Damage()
    {
        stats.ChangeHealth(-0.1f);
    }

    public void Death()
    {
        controllerManager.PlayerDied(transform.parent);
    }

    public bool CheckForVoxel(Vector3 pos, bool trueIfNull = false, int minSolidity = 2)
    {
        VoxelState voxel = world.worldData.GetVoxel(pos);

        if (!voxel.created)
        {
            return trueIfNull;
        }

        if (BlockData.voxelTypes[voxel.id].solidity >= minSolidity)
            return true;
        else
            return false;
    }

    public AudioClip CheckForAudio(Vector3 pos)
    {
        VoxelState voxel = world.worldData.GetVoxel(pos);

        if (!voxel.created)
        {
            return null;
        }
        else if (BlockData.voxelTypes[voxel.id].solidity > 1)
        {
            return world.blockTypes[voxel.id].stepSound;
        }
        else
        {
            return null;
        }
    }

    public bool InUI
    {
        get { return _inUI; }

        set
        {
            _inUI = value;
            Debug.Log("UI " + value);

            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorSlot.SetActive(false);
            }
        }
    }
}

[System.Serializable]
public class PlayerSettings
{
    [Header("Perfomance")]
    public int viewDistance = 2;
    public int loadDistance = 4; 
    public bool enableThreading = true;
    public CloudStyle clouds = CloudStyle.TwoD;

    [Header("Control")]
    [Range(1, 16)]
    public float mouseSensitivity = 4;
    public string inventoryKey = "<Keyboard>/tab";
    public string debugScreenKey = "<Keyboard>/f3";
    public string nextCharacterKey = "<Keyboard>/p";
    public string prevCharacterKey = "<Keyboard>/o";
}

[System.Serializable]
public class Stats
{
    public Slider healthSlider;
    public Slider energySlider;
    public Slider staminaSlider;
    public GameObject hungry;
    public GameObject tired;
    public Player parent;
    
    public void ChangeHealth(float value)
    {
        healthSlider.value += value;

        if(healthSlider.value < 0.01f)
        {
            Death();
        }
        else if(healthSlider.value<0.1f)
        {
            Knockout();
        }
    }

    public void ChangeEnergy(float value)
    {
        energySlider.value += value;

        if (energySlider.value < 0.01f)
        {
            Knockout();
        }
    }

    public void ChangeStamina(float value)
    {
        staminaSlider.value += value / energySlider.value;

        if (energySlider.value < 0.01f)
        {
            OutOfBreath();
        }
    }

    void Death()
    {
        parent.Death();
    }

    void Knockout()
    {

    }

    void OutOfBreath()
    {

    }


}

