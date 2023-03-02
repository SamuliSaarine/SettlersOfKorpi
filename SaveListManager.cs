using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveListManager : MonoBehaviour
{
    public GameObject saveListItem;
    public Transform list;

    public GameObject newWorldMenu;
    public GameObject loadScreen;

    private Toggle[] saveItems;

    void Start()
    {
        LoadSaveList();
    }

    void LoadSaveList()
    {
        foreach  (Transform listItem in list)
        {
            Destroy(listItem.gameObject);
        }

        string path = Application.persistentDataPath + "/saves/";

        Debug.Log("Loading saves from " + path);

        saveItems = new Toggle[Directory.GetDirectories(path).Length];

        if (saveItems.Length == 0) return;

        for (int i = 0; i < Directory.GetDirectories(path).Length; i++)
        {
            GameObject newItem = Instantiate(saveListItem, list);
            saveItems[i] = newItem.GetComponent<Toggle>();
            saveItems[i].group = list.GetComponent<ToggleGroup>();
            DirectoryInfo dir = new(path);
            DirectoryInfo dirInfo = dir.GetDirectories()[i];
            newItem.transform.GetComponentInChildren<TMP_Text>().text = dirInfo.Name;
        }

        saveItems[0].isOn = true;
    }

    public void OpenSave()
    {
        string worldName = WorldName();

        if (worldName == null) return;

        Debug.Log("Loading " + worldName);
        VoxelData.worldName = worldName;
        loadScreen.SetActive(true);
        SceneManager.LoadScene("SinglePlayer", LoadSceneMode.Single);
    }

    string WorldName()
    {
        for (int i = 0; i < saveItems.Length; i++)
        {
            if(saveItems[i].isOn)
            {
                return saveItems[i].transform.GetComponentInChildren<TMP_Text>().text;
            }
        }

        return null;
    }

    public void DeleteSave()
    {
        string path = Application.persistentDataPath + "/saves/" + WorldName();

        if(Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        LoadSaveList();
    }
}
