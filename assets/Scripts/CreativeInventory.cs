using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{
    public GameObject slotPrefab;
    World world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        for (var i = 0; i < world.blocktypes.Length; i++)
        {
            var newSlot = Instantiate(slotPrefab, transform);
            var stack = new ItemStack((byte)i, 64);
            var slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = true;
        }
    }
}
