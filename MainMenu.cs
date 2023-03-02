using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    public GameObject[] menus;
    public TextMeshProUGUI seedInput;

    public InputActionAsset kontrollit;

    PlayerSettings settings;

    private void Awake()
    {
        if (!File.Exists(Application.dataPath + "/settings.cfg"))
        {
            Debug.Log("Settings file not found");
            settings = new PlayerSettings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else
        {
            Debug.Log("Loading settings");

            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<PlayerSettings>(jsonImport);

            LoadKeybinds();
        }

        ChangeMenu(0);
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            ChangeMenu(0);
        }
    }

    public void ChangeMenu(int index)
    {
        foreach(GameObject menu in menus)
        {
            if(menu == menus[index])
            {
                menu.SetActive(true);
            }
            else
            {
                menu.SetActive(false);
            }
        }
    }

    void LoadKeybinds()
    {
        LoadBinding(settings.inventoryKey, "Inventory", 0);
        LoadBinding(settings.debugScreenKey, "DebugScreen", 0);
        LoadBinding(settings.nextCharacterKey, "NextCharacter", 0);
        LoadBinding(settings.prevCharacterKey, "PrevCharacter", 0);
    }

    private void LoadBinding(string binding, string action, int index)
    {
        InputAction aktion = kontrollit.FindAction(action);
        aktion.ApplyBindingOverride(binding, path: aktion.bindings[index].path);
    }

    public void OpenSettings()
    {
        ChangeMenu(1);

        menus[1].GetComponent<SettingsMenu>().LoadSettings(settings);
    } 
    
    public void CloseSettings()
    {
        menus[1].GetComponent<SettingsMenu>().SaveSettings(settings);

        ChangeMenu(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
