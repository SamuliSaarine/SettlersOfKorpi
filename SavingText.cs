using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SavingText : MonoBehaviour
{
    private TMP_Text text;

    private void Awake()
    {
        text = gameObject.GetComponent<TMP_Text>();
    }

    private void Update()
    {
        UpdateText();
    }

    void UpdateText()
    {
        int percentage = 100 - Mathf.FloorToInt((1f * SaveSystem.unsavedChunksLeft / SaveSystem.chunksToSave * 100f));
        if(percentage < 100 && !text.text.Contains(percentage + "%"))
        {
            text.text = percentage + "% saved. Do NOT quit the world!";
        }

        if(SaveSystem.unsavedChunksLeft == 0)
        {
            gameObject.SetActive(false);
        }
    }
}
