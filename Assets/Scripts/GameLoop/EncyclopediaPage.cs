using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Greybox version of the Flower Encyclopedia: a page of text on the book.
// Lists every unlocked flower with its meaning; locked flowers show as "???".
public class EncyclopediaPage : MonoBehaviour
{
    public TextMeshPro pageText;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (pageText == null) return;

        List<string> known = new List<string>();      // "Rose|Red"
        List<string> unlockedLines = new List<string>();
        List<string> lockedKeys = new List<string>();

        FlowerItem[] flowers = FindObjectsByType<FlowerItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // first the unlocked flowers (at least one copy is visible in the shop)
        for (int i = 0; i < flowers.Length; i++)
        {
            if (!flowers[i].gameObject.activeInHierarchy) continue;
            string key = flowers[i].flowerName + "|" + flowers[i].colorName;
            if (known.Contains(key)) continue;
            known.Add(key);
            unlockedLines.Add("<b>" + flowers[i].colorName + " " + flowers[i].flowerName + "</b> - " + flowers[i].meaning);
        }

        // then the locked ones (only hidden copies exist)
        for (int i = 0; i < flowers.Length; i++)
        {
            string key = flowers[i].flowerName + "|" + flowers[i].colorName;
            if (known.Contains(key) || lockedKeys.Contains(key)) continue;
            lockedKeys.Add(key);
        }

        unlockedLines.Sort();

        string text = "<b>FLOWER ENCYCLOPEDIA</b>\n\n";
        for (int i = 0; i < unlockedLines.Count; i++)
        {
            text += unlockedLines[i] + "\n";
        }
        for (int i = 0; i < lockedKeys.Count; i++)
        {
            text += "<color=#888888>??? - not discovered yet</color>\n";
        }
        pageText.text = text;
    }
}
