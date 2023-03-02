using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class NewWorld : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField seedInput;
    public GameObject loadingScreen;

    public void Create()
    {
        if (seedInput.text == "")
        {
            seedInput.text = (System.DateTime.Now.Hour * 10000 + System.DateTime.Now.Day * 100 + System.DateTime.Now.Second).ToString();
        }

        int newSeed = Mathf.Abs(seedInput.text.GetHashCode()) / VoxelData.WorldSizeInVoxels;
        Debug.Log(newSeed);
        VoxelData.seed = newSeed;

        VoxelData.worldName = nameInput.text;

        Debug.Log("Creating " + VoxelData.worldName + " with seed " + VoxelData.seed);

        loadingScreen.SetActive(true);

        SceneManager.LoadScene("SinglePlayer", LoadSceneMode.Single);
    }

}
