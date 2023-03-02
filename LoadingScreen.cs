using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Slider loadingSlider;
    public TMP_Text tmptext;
    public string[] loadingTexts;
    public float textInterval;

    private int firstValue;
    private bool firstValueGot = false;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating(nameof(ChangeText), 0f, textInterval);
    }

    public void UpdateSlider(int value)
    {
        if(!firstValueGot)
        {
            firstValue = value;
            firstValueGot = true;
        }

        int sliderValue = Mathf.FloorToInt(64f / firstValue * value);
        loadingSlider.value = 64 - sliderValue; 
    }

    void ChangeText()
    {
        tmptext.text = loadingTexts[Random.Range(0, loadingTexts.Length)];
    }
}
