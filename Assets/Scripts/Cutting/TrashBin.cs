using UnityEngine;

public class TrashBin : MonoBehaviour
{
    // A cut piece (Scrap) that falls in the bin is destroyed
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scrap"))
        {
            Destroy(other.gameObject);
        }
    }

    // From the team contract with Person A (section 6)
    // Called by DayCycle at the end of the day to clean the whole shop
    public void CleanUpScraps()
    {
        GameObject[] remainingScraps = GameObject.FindGameObjectsWithTag("Scrap");
        foreach (GameObject scrap in remainingScraps)
        {
            Destroy(scrap);
        }
    }
}