using UnityEngine;

public class TrashBin : MonoBehaviour
{
    // Când o bucată tăiată (Scrap) cade în coș, o ștergem
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scrap"))
        {
            Destroy(other.gameObject);
        }
    }

    // Funcția din Contractul cu Persoana A (Secțiunea 6 din PDF)
    // Apelată de DayCycle la finalul zilei pentru curățenie în tot magazinul
    public void CleanUpScraps()
    {
        GameObject[] remainingScraps = GameObject.FindGameObjectsWithTag("Scrap");
        foreach (GameObject scrap in remainingScraps)
        {
            Destroy(scrap);
        }
    }
}