using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public Player player;
    public int slotIndex = 0;

    private void Start()
    {
        foreach (UIItemSlot s in slots)
        {
            new ItemSlot(s);
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0)
            {
                slotIndex--;
            }
            else
            {
                slotIndex++;
            }

            if (slotIndex > slots.Length - 1)
            {
                slotIndex = 0;
            }

            if (slotIndex < 0)
            {
                slotIndex = slots.Length - 1;
            }

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
    }
}

