using TMPro;
using UnityEngine;

// The board on the wall: shop score, finished orders and the unlock progress bar.
// The progress bar never goes down. When it is full, the next locked vase fills with flowers.
public class ScoreBoard : MonoBehaviour
{
    [Header("Display")]
    public TextMeshPro boardText;
    public Transform barFill;          // thin box that grows from left to right

    [Header("Unlocks")]
    public GameObject[] lockedFlowerGroups;   // flowers in empty vases, in unlock order (start disabled)
    public int pointsPerUnlock = 100;
    public ParticleSystem unlockEffect;       // optional

    [Header("State (read only)")]
    public int shopScore;
    public int unlockPoints;
    public int unlockedCount;
    public int ordersCompleted;
    public int perfectOrders;

    float barFullWidth;
    float barStartX;

    void Awake()
    {
        if (barFill != null)
        {
            barFullWidth = barFill.localScale.x;
            barStartX = barFill.localPosition.x - barFullWidth / 2f;   // left edge of the bar
        }

        // locked flowers are hidden at the start
        for (int i = 0; i < lockedFlowerGroups.Length; i++)
        {
            if (lockedFlowerGroups[i] != null) lockedFlowerGroups[i].SetActive(false);
        }
    }

    void Start()
    {
        Refresh();
    }

    // Adds the result of one order. Returns the name of a new flower if one was unlocked, or "".
    public string AddResult(int stars)
    {
        ordersCompleted++;
        if (stars == 3)
        {
            perfectOrders++;
            shopScore += 10;
            unlockPoints += 100;
        }
        else if (stars == 2)
        {
            shopScore += 3;
            unlockPoints += 50;
        }
        else
        {
            shopScore -= 5;          // the shop score can go down...
            if (shopScore < 0) shopScore = 0;
            unlockPoints += 10;      // ...but the unlock bar only goes up
        }

        string unlocked = "";
        while (unlockPoints >= pointsPerUnlock && unlockedCount < lockedFlowerGroups.Length)
        {
            unlockPoints -= pointsPerUnlock;
            GameObject group = lockedFlowerGroups[unlockedCount];
            unlockedCount++;

            if (group == null) continue;
            group.SetActive(true);
            if (unlockEffect != null)
            {
                unlockEffect.transform.position = group.transform.position;
                unlockEffect.Play();
            }

            FlowerItem flower = group.GetComponentInChildren<FlowerItem>();
            if (flower != null)
            {
                if (unlocked != "") unlocked += ", ";
                unlocked += flower.colorName + " " + flower.flowerName;
            }
        }

        // everything unlocked: keep the bar full
        if (unlockedCount >= lockedFlowerGroups.Length) unlockPoints = Mathf.Min(unlockPoints, pointsPerUnlock);

        Refresh();
        return unlocked;
    }

    public void Refresh()
    {
        if (boardText != null)
        {
            string text = "<b>LITTLE BLOOM SHOP</b>\n\n";
            text += "Shop score: <b>" + shopScore + "</b>\n";
            text += "Orders completed: " + ordersCompleted + "\n";
            text += "Perfect bouquets: " + perfectOrders + "\n\n";
            if (unlockedCount < lockedFlowerGroups.Length)
                text += "Next flower: " + unlockPoints + " / " + pointsPerUnlock;
            else
                text += "All flowers unlocked!";
            boardText.text = text;
        }

        if (barFill != null)
        {
            float progress = (float)unlockPoints / pointsPerUnlock;
            if (unlockedCount >= lockedFlowerGroups.Length) progress = 1f;
            progress = Mathf.Clamp01(progress);

            float width = Mathf.Max(barFullWidth * progress, 0.0001f);
            Vector3 scale = barFill.localScale;
            scale.x = width;
            barFill.localScale = scale;

            Vector3 position = barFill.localPosition;
            position.x = barStartX + width / 2f;   // keep the left edge in place
            barFill.localPosition = position;
        }
    }
}
