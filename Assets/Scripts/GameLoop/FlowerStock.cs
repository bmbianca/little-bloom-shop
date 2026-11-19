using System.Collections.Generic;
using UnityEngine;

// Keeps the vases full. At the start it remembers every flower standing in a vase.
// Restock() puts a new flower in each spot whose flower was taken.
// Flowers in a locked (hidden) vase are skipped until the vase is unlocked.
public class FlowerStock : MonoBehaviour
{
    class Slot
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public GameObject template;   // hidden copy used to make a new flower
        public GameObject current;    // the flower standing in this spot now
    }

    List<Slot> slots = new List<Slot>();
    Transform templateHolder;

    void Start()
    {
        templateHolder = new GameObject("FlowerTemplates (hidden)").transform;
        templateHolder.gameObject.SetActive(false);

        // every flower that waits in a vase has FlowerRest (also the hidden, locked ones)
        FlowerRest[] flowers = FindObjectsByType<FlowerRest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < flowers.Length; i++)
        {
            GameObject original = flowers[i].gameObject;

            // only real flowers: the bouquet on the wrapping table also has FlowerRest (to stay still),
            // and a copy of it would appear on the table as a gray cone
            if (original.GetComponent<FlowerItem>() == null) continue;

            Slot slot = new Slot();
            slot.parent = original.transform.parent;
            slot.localPosition = original.transform.localPosition;
            slot.localRotation = original.transform.localRotation;
            slot.current = original;

            // the copy lives under a disabled object, so it never shows and can't be grabbed
            slot.template = Instantiate(original, templateHolder);
            slot.template.name = original.name;

            slots.Add(slot);
        }
    }

    public void Restock()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];

            // vase still locked: nothing to do
            if (slot.parent != null && !slot.parent.gameObject.activeInHierarchy) continue;
            if (!FlowerWasTaken(slot)) continue;

            GameObject flower = Instantiate(slot.template, slot.parent);
            flower.name = slot.template.name;
            flower.transform.localPosition = slot.localPosition;
            flower.transform.localRotation = slot.localRotation;
            flower.SetActive(true);
            if (flower.GetComponent<GrowIn>() == null) flower.AddComponent<GrowIn>();

            slot.current = flower;
        }
    }

    bool FlowerWasTaken(Slot slot)
    {
        if (slot.current == null) return true;
        if (slot.current.transform.parent != slot.parent) return true;
        return Vector3.Distance(slot.current.transform.localPosition, slot.localPosition) > 0.05f;
    }

    // At the end of the day: removes flowers left lying around (not in a vase, not in the bouquet).
    public void ClearLooseFlowers(Bouquet bouquet)
    {
        FlowerItem[] all = FindObjectsByType<FlowerItem>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject flower = all[i].gameObject;
            if (IsInVase(flower)) continue;
            if (bouquet != null && flower.transform.IsChildOf(bouquet.transform)) continue;
            Destroy(flower);
        }
    }

    bool IsInVase(GameObject flower)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].current == flower && !FlowerWasTaken(slots[i])) return true;
        }
        return false;
    }
}
