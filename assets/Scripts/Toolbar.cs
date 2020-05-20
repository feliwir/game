using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public int slotIndex;
    public Player player;

    private void Start()
    {
        byte index = 1;
        foreach (var s in slots)
        {
            var stack = new ItemStack(index, Random.Range(2, 65));
            var slot = new ItemSlot(s, stack);
            index++;
        }
    }

    private void Update()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0 && slotIndex > 0) slotIndex--;
            else if (slotIndex < slots.Length - 1) slotIndex++;

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
    }
}
