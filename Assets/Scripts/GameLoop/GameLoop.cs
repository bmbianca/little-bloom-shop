using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The game loop of Little Bloom Shop.
//
// DAY:   sign -> OPEN, a few orders, sunset, sign -> CLOSED, night, next day
// ORDER: bell rings, order appears in the notebook, touch the notebook to take it,
//        make the bouquet, put it on the main desk, press the desk bell,
//        the bouquet is scored, the board and the sun update, the next order comes.
//
// All the text the player reads is in the shop (notebook, board, book): no pop-ups.
public class GameLoop : MonoBehaviour
{
    public enum State
    {
        Closed,           // waiting for the player to flip the sign to OPEN
        WaitingForOrder,  // the next customer is on the way
        OrderWaiting,     // the order is in the notebook, not taken yet
        Working,          // order taken, the player makes the bouquet
        DayOver           // all orders done, waiting for the sign to be flipped to CLOSED
    }

    [Header("Orders")]
    public List<OrderData> orders = new List<OrderData>();
    public int ordersPerDay = 3;
    public float delayBeforeOrder = 3f;    // seconds before the next customer
    public float resultTime = 6f;          // seconds the result stays in the notebook

    [Header("Shop parts")]
    public WrappingStation station;
    public TextMeshPro notebookText;
    public float notebookFontSize = 0.09f; // fixed size, so the text doesn't shrink when a hint shows up
    public Transform notebook;             // where the new-order sound plays
    public Bounds deliveryArea = new Bounds(Vector3.zero, Vector3.zero);   // box on the main desk where the bouquet is handed over (size 0 = anywhere)
    public ShopSign sign;
    public DayCycle dayCycle;
    public ScoreBoard scoreBoard;
    public FlowerStock flowerStock;
    public EncyclopediaPage encyclopedia;
    public TrashBin trashBin;

    [Header("Sounds (optional)")]
    public AudioClip newOrderSound;
    public AudioClip goodResultSound;
    public AudioClip badResultSound;
    public AudioClip unlockSound;

    [Header("State (read only)")]
    public State state = State.Closed;
    public int day = 0;
    public int ordersDoneToday = 0;

    OrderData currentOrder;
    bool[] usedOrders;
    List<string> todayResults = new List<string>();
    string notebookPage = "";

    void Start()
    {
        usedOrders = new bool[orders.Count];
        state = State.Closed;

        if (notebookText != null)
        {
            notebookText.enableAutoSizing = false;
            notebookText.fontSize = notebookFontSize;
        }

        if (sign != null) sign.SetOpen(false, true);
        if (dayCycle != null) dayCycle.SetNight(true);
        ShowPage("<b>Little Bloom Shop</b>\n\nFlip the sign on the door to <b>OPEN</b> to start day 1.");
    }

    // ================= called by the TouchButtons =================

    public void OnSignPressed()
    {
        if (state == State.Closed)
        {
            OpenShop();
        }
        else if (state == State.DayOver)
        {
            CloseShop();
        }
        else
        {
            ShowHint("Finish today's orders before closing the shop.");
        }
    }

    public void OnNotebookPressed()
    {
        if (state == State.OrderWaiting)
        {
            state = State.Working;
            ShowPage(OrderPage(true));
        }
    }

    public void OnBellPressed()
    {
        if (state != State.Working)
        {
            if (state == State.OrderWaiting) ShowHint("Take the order in the notebook first.");
            return;
        }

        Bouquet bouquet = station.bouquet;
        if (!bouquet.isTied)
        {
            ShowHint("Wrap the bouquet and tie the ribbon first.");
            return;
        }
        if (station.bouquetGrab != null && station.bouquetGrab.isSelected)
        {
            ShowHint("Put the bouquet down on the desk, then ring the bell.");
            return;
        }
        if (deliveryArea.size != Vector3.zero && !deliveryArea.Contains(bouquet.transform.position))
        {
            ShowHint("Bring the bouquet to this desk, next to the bell.");
            return;
        }

        Deliver(bouquet);
    }

    // ================= day =================

    void OpenShop()
    {
        day++;
        ordersDoneToday = 0;
        todayResults.Clear();

        if (sign != null) sign.SetOpen(true, false);
        if (dayCycle != null) dayCycle.SetDayProgress(0f);
        if (flowerStock != null) flowerStock.Restock();

        state = State.WaitingForOrder;
        ShowPage("<b>Day " + day + "</b>\n\nThe shop is open!\nWaiting for the first customer...");
        StartCoroutine(NextOrderAfter(delayBeforeOrder));
    }

    void EndDay()
    {
        state = State.DayOver;

        string page = "<b>Day " + day + " is over!</b>\n\n";
        for (int i = 0; i < todayResults.Count; i++)
        {
            page += todayResults[i] + "\n";
        }
        if (scoreBoard != null) page += "\nShop score: " + scoreBoard.shopScore;
        page += "\n\nFlip the sign on the door to <b>CLOSED</b>.";
        ShowPage(page);
    }

    void CloseShop()
    {
        state = State.Closed;

        if (sign != null) sign.SetOpen(false, false);
        if (dayCycle != null) dayCycle.SetNight(false);

        // tidy the shop for tomorrow
        if (trashBin != null) trashBin.CleanUpScraps();
        RemoveLoosePaperAndRibbons();
        if (station != null) station.ResetStation();
        if (flowerStock != null)
        {
            flowerStock.ClearLooseFlowers(station != null ? station.bouquet : null);
            flowerStock.Restock();
        }

        ShowPage("<b>Good night!</b>\n\nFlip the sign to <b>OPEN</b> to start day " + (day + 1) + ".");
    }

    void RemoveLoosePaperAndRibbons()
    {
        WrapItem[] items = FindObjectsByType<WrapItem>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            Destroy(items[i].gameObject);
        }
    }

    // ================= orders =================

    IEnumerator NextOrderAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        currentOrder = PickOrder();
        if (currentOrder == null)
        {
            ShowPage("No orders found.\nAdd some in GameLoop > Orders.");
            yield break;
        }

        if (newOrderSound != null)
        {
            Vector3 where = notebook != null ? notebook.position : transform.position;
            AudioSource.PlayClipAtPoint(newOrderSound, where);
        }

        state = State.OrderWaiting;
        ShowPage(OrderPage(false));
    }

    // The next order not used yet whose flowers are already unlocked.
    // When every order was used, they can come again.
    OrderData PickOrder()
    {
        int unlocked = scoreBoard != null ? scoreBoard.unlockedCount : 0;

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                if (!usedOrders[i] && orders[i].requiredUnlocks <= unlocked)
                {
                    usedOrders[i] = true;
                    return orders[i];
                }
            }
            // nothing left: allow repeats
            for (int i = 0; i < usedOrders.Length; i++) usedOrders[i] = false;
        }
        return null;
    }

    string OrderPage(bool taken)
    {
        string page = "Order " + (ordersDoneToday + 1) + " of " + ordersPerDay + "\n\n";
        page += "<b>" + currentOrder.clientName + "</b>\n";
        page += "<i>\"" + currentOrder.request + "\"</i>\n\n";

        if (taken)
        {
            page += "<color=#B03060><b>[ TAKEN ]</b></color>\n";
            page += "Bring the finished bouquet to this desk and ring the bell.";
        }
        else
        {
            page += "Touch the notebook to take this order.";
        }
        return page;
    }

    void Deliver(Bouquet bouquet)
    {
        int total = BouquetEvaluator.CountFlowers(bouquet);
        int matches = BouquetEvaluator.CountMatches(bouquet, currentOrder);
        int stars = BouquetEvaluator.Stars(bouquet, currentOrder);

        string unlocked = "";
        if (scoreBoard != null) unlocked = scoreBoard.AddResult(stars);

        ordersDoneToday++;
        todayResults.Add(currentOrder.clientName + "  " + BouquetEvaluator.StarText(stars));

        // the result stamp in the notebook
        string page = "<b>" + currentOrder.clientName + "</b>  " + BouquetEvaluator.StarText(stars) + "\n\n";
        if (stars >= 2) page += "<i>\"" + currentOrder.happyReply + "\"</i>\n\n";
        else page += "<i>\"" + currentOrder.sadReply + "\"</i>\n\n";
        page += matches + " of " + total + " flowers matched the request.";
        if (unlocked != "") page += "\n\n<color=#2E7D32><b>New flower unlocked: " + unlocked + "!</b></color>";
        ShowPage(page);

        AudioClip sound = stars >= 2 ? goodResultSound : badResultSound;
        if (sound != null) AudioSource.PlayClipAtPoint(sound, bouquet.transform.position);
        if (unlocked != "" && unlockSound != null) AudioSource.PlayClipAtPoint(unlockSound, bouquet.transform.position);

        // the customer takes the bouquet, the table is ready for the next one
        station.ResetStation();
        if (flowerStock != null) flowerStock.Restock();
        if (encyclopedia != null && unlocked != "") encyclopedia.Refresh();

        // the sun moves forward
        if (dayCycle != null) dayCycle.SetDayProgress((float)ordersDoneToday / ordersPerDay);

        // while the result is shown, nothing else can happen
        state = State.WaitingForOrder;
        if (ordersDoneToday >= ordersPerDay) StartCoroutine(EndDayAfter(resultTime));
        else StartCoroutine(NextOrderAfter(resultTime));
    }

    IEnumerator EndDayAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        EndDay();
    }

    // the delivery box is drawn in the Scene view when GameLoop is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.7f, 0.8f);
        Gizmos.DrawWireCube(deliveryArea.center, deliveryArea.size);
    }

    // ================= notebook text =================

    void ShowPage(string page)
    {
        notebookPage = page;
        if (notebookText != null) notebookText.text = page;
    }

    // A small note in place of the last line of the page (it goes away with the next page).
    // Replacing the line instead of adding one keeps the page the same length.
    void ShowHint(string hint)
    {
        if (notebookText == null) return;
        int lastLine = notebookPage.LastIndexOf('\n');
        string top = lastLine >= 0 ? notebookPage.Substring(0, lastLine + 1) : "";
        notebookText.text = top + "<color=#777777><i>" + hint + "</i></color>";
    }
}
