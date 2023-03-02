using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_raycaster = null;
    private PointerEventData m_pointerEventData;
    [SerializeField] private EventSystem m_eventSystem = null;

    private World world;
    public Player player;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if(!player.InUI)
        {
            return;
        }

        cursorSlot.transform.position = Input.mousePosition;
    
        if(Input.GetMouseButtonDown(0))
        {
            HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
        {
            if(cursorSlot.HasItem)
            {
                cursorItemSlot.EmptySlot();
            }
            return;
        }

        if(!cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            return;
        }

        if(clickedSlot.itemSlot == null)
        {
            Debug.Log(clickedSlot + " itemslot null");
        }

        if(clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if(!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if(cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }
            else
            {
                if (cursorSlot.itemSlot.stack.amount + clickedSlot.itemSlot.stack.amount <= world.blockTypes[clickedSlot.itemSlot.stack.id].stackSize)
                {
                    ItemStack newStack = cursorSlot.itemSlot.TakeAll();
                    newStack.amount += clickedSlot.itemSlot.stack.amount;
                    clickedSlot.itemSlot.InsertStack(newStack);
                }
                else
                {
                    cursorSlot.itemSlot.stack.amount -= world.blockTypes[clickedSlot.itemSlot.stack.id].stackSize - clickedSlot.itemSlot.stack.amount;
                    clickedSlot.itemSlot.stack.amount = world.blockTypes[clickedSlot.itemSlot.stack.id].stackSize;
                    cursorSlot.UpdateSlot();
                    clickedSlot.UpdateSlot();
                }
            }
        }

    }

    private UIItemSlot CheckForSlot()
    {
        m_pointerEventData = new(m_eventSystem);
        m_pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new();
        m_raycaster.Raycast(m_pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("UIItemSlot"))
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
