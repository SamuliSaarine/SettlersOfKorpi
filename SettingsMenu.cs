using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.InputSystem;

public class SettingsMenu : MonoBehaviour
{
    public GameObject[] settingPages;
    public Toggle[] toggles;

    public SliderNumberConfigs[] sliderNumberConfigs;
    public SliderImageConfigs[] sliderImageConfigs;

    public InputActionAsset kontrollit;

    public GameObject controlsUIPanel;

    [Header("Settings")]

    [Header("Gameplay")]
    public Slider viewDistance;
    public Slider threading;
    public Slider clouds;

    [Header("PCControls")]
    public Slider mouseSensitivity;

    public void LoadSettings(PlayerSettings settings)
    {
        viewDistance.value = settings.viewDistance / 4;
        mouseSensitivity.value = settings.mouseSensitivity * 4;
        clouds.value = (int)settings.clouds;

        if(settings.enableThreading)
        {
            threading.value = 1;
        }
        else
        {
            threading.value = 0;
        }

        UpdateSliderNumber();
        UpdateSliderImage();
    }    
    
    public void SaveSettings(PlayerSettings settings)
    {
        settings.viewDistance = (int)viewDistance.value * 4;
        settings.mouseSensitivity = mouseSensitivity.value / 4;
        settings.clouds = (CloudStyle)clouds.value;

        //Keybinds
        settings.inventoryKey = GetBinding("Inventory", 0);
        settings.debugScreenKey = GetBinding("DebugScreen", 0);
        settings.nextCharacterKey = GetBinding("NextCharacter", 0);
        settings.prevCharacterKey = GetBinding("PrevCharacter", 0);

        if (threading.value == 0)
        {
            settings.enableThreading = false;
        }
        else
        {
            settings.enableThreading = true;
        }

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
    }

    private string GetBinding(string action, int index)
    {
        if (kontrollit.FindAction(action).bindings[index].overridePath != null)
        {
           return kontrollit.FindAction(action).bindings[index].overridePath;
        }
        else
        {
            return kontrollit.FindAction(action).bindings[index].path;
        }
    }

    public void ChangePage(int page)
    {
        if (!toggles[page].isOn) return;

        for (int i = 0; i < settingPages.Length; i++)
        {
            settingPages[i].SetActive(toggles[i].isOn);
        }
    }

    public void UpdateSliderNumber()
    {
        foreach (SliderNumberConfigs slider in sliderNumberConfigs)
        {
            slider.text.text = (slider.slider.value * slider.multiplier).ToString();
        }
    }

    public void UpdateSliderImage()
    {
        foreach (SliderImageConfigs configs in sliderImageConfigs)
        {
            configs.image.sprite = configs.icons[(int)configs.slider.value];
        }
    }
}

[System.Serializable]
public class SliderNumberConfigs
{
    public Slider slider;
    public TextMeshProUGUI text;
    public float multiplier = 1;
}

[System.Serializable]
public class SliderImageConfigs
{
    public Slider slider;
    public Image image;
    public Sprite[] icons;
}

