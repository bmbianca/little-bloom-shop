using UnityEngine;

// One customer order, filled in the Inspector (GameLoop > Orders).
// Good flowers are written as "Name/Color", for example "Rose/Red".
// Write only the name ("Tulip") if any color is fine.
[System.Serializable]
public class OrderData
{
    public string clientName = "Ana";
    [TextArea(2, 5)] public string request = "It's my parents' anniversary tomorrow...";
    public string[] goodFlowers = new string[] { "Rose/Red" };
    [Tooltip("How many flowers must be unlocked before this order can appear (0 = from the start)")]
    public int requiredUnlocks = 0;
    [TextArea(1, 3)] public string happyReply = "It's perfect, thank you!";
    [TextArea(1, 3)] public string sadReply = "Hmm... not quite what I meant.";
}
