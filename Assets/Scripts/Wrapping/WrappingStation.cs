using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Statia de ambalare de pe masa de asamblare.
// 1. Tragi o foaie de pe suportul cu role si o pui pe locul marcat de pe masa.
// 2. Pui florile pe hartie (BouquetZone, partea de flori).
// 3. Indoi cele 2 clape ale foii peste flori (stanga, dreapta).
// 4. Iei o panglica si o duci la baza buchetului -> apare fundita.
// 5. Buchetul e gata si poate fi ridicat.
// Cand tii in mana hartie, o floare sau panglica, apare un contur alb acolo unde trebuie pusa.
public class WrappingStation : MonoBehaviour
{
    [Header("Buchetul")]
    public Bouquet bouquet;
    public XRGrabInteractable bouquetGrab;
    public bool lockBouquetUntilDone = true;   // buchetul nu poate fi luat de pe masa pana nu e legat

    [Header("Hartia")]
    public WrapSocket paperSocket;             // locul de pe masa unde pui foaia
    public GameObject paperSpotMarker;         // conturul care arata unde se pune foaia (doar cat tii o foaie)
    public GameObject laidPaper;               // foaia intinsa pe masa: mijloc + 2 clape
    public Renderer[] laidPaperPieces;         // bucatile foii, ca sa le dam culoarea
    public PaperFlap[] flaps;                  // clapele care se indoaie

    [Header("Conul si panglica (din buchet)")]
    public Renderer wrapCone;
    public GrowIn wrapConeGrow;
    public GameObject ribbonBow;
    public WrapSocket ribbonSocket;            // locul de la baza buchetului unde duci panglica

    [Header("Contururi (apar doar cat tii obiectul potrivit)")]
    public GameObject flowerHint;              // unde pui florile pe hartie
    public GameObject ribbonHint;              // inelul de la baza buchetului

    [Header("Efecte (optional)")]
    public ParticleSystem puffEffect;
    public ParticleSystem doneEffect;

    [Header("Ce a ales jucatorul")]
    public string paperColor = "";
    public string ribbonColor = "";

    Material paperMaterial;
    Collider wrapConeCollider;
    bool paperPlaced;
    int foldedFlaps;

    // ce tine jucatorul in maini acum
    bool holdingPaper;
    bool holdingRibbon;
    bool holdingFlower;
    List<IXRInteractor> interactors = new List<IXRInteractor>();

    void Start()
    {
        laidPaper.SetActive(false);

        // conul nu se vede si nu se poate atinge pana nu e impachetat buchetul
        wrapCone.enabled = false;
        wrapConeCollider = wrapCone.GetComponent<Collider>();
        if (wrapConeCollider != null) wrapConeCollider.enabled = false;

        ribbonBow.SetActive(false);
        ribbonSocket.gameObject.SetActive(false);

        if (lockBouquetUntilDone && bouquetGrab != null)
        {
            bouquetGrab.enabled = false;
        }

        paperSocket.selectEntered.AddListener(OnPaperPlaced);
        ribbonSocket.selectEntered.AddListener(OnRibbonPlaced);
    }

    void OnDestroy()
    {
        if (paperSocket != null) paperSocket.selectEntered.RemoveListener(OnPaperPlaced);
        if (ribbonSocket != null) ribbonSocket.selectEntered.RemoveListener(OnRibbonPlaced);
    }

    void Update()
    {
        // clapele se pot indoi doar cand foaia e pe masa si are flori pe ea
        bool ready = laidPaper.activeSelf && bouquet.flowers.Count > 0 && !bouquet.isWrapped;
        for (int i = 0; i < flaps.Length; i++)
        {
            flaps[i].canFold = ready;
        }

        // conturul apare doar cat tii in mana obiectul care merge acolo
        CheckHands();
        if (paperSpotMarker != null) paperSpotMarker.SetActive(holdingPaper && !paperPlaced);
        if (flowerHint != null) flowerHint.SetActive(holdingFlower && !bouquet.isWrapped);
        if (ribbonHint != null) ribbonHint.SetActive(holdingRibbon && bouquet.isWrapped && !bouquet.isTied);
    }

    // Se uita ce tine fiecare mana (socket-urile nu conteaza, ele nu sunt maini).
    void CheckHands()
    {
        holdingPaper = false;
        holdingRibbon = false;
        holdingFlower = false;

        XRInteractionManager manager = paperSocket.interactionManager;
        if (manager == null) return;
        manager.GetRegisteredInteractors(interactors);

        for (int i = 0; i < interactors.Count; i++)
        {
            IXRSelectInteractor hand = interactors[i] as IXRSelectInteractor;
            if (hand == null || hand is XRSocketInteractor || !hand.hasSelection) continue;

            Transform held = hand.interactablesSelected[0].transform;
            WrapItem item = held.GetComponent<WrapItem>();
            if (item != null)
            {
                if (item.isRibbon) holdingRibbon = true;
                else holdingPaper = true;
            }
            if (held.GetComponent<FlowerItem>() != null) holdingFlower = true;
        }
    }

    // ---------- 1. foaia pusa pe masa ----------
    void OnPaperPlaced(SelectEnterEventArgs args)
    {
        if (paperPlaced) return;
        paperPlaced = true;

        GameObject sheet = args.interactableObject.transform.gameObject;
        WrapItem item = sheet.GetComponent<WrapItem>();
        if (item != null) paperColor = item.colorName;

        Renderer sheetRenderer = sheet.GetComponentInChildren<Renderer>();
        if (sheetRenderer != null) paperMaterial = sheetRenderer.sharedMaterial;

        StartCoroutine(LayPaper(sheet));
    }

    IEnumerator LayPaper(GameObject sheet)
    {
        // lasam foaia sa alunece lin pana se asaza pe masa
        yield return new WaitForSeconds(0.25f);

        // apoi o inlocuim cu foaia de pe masa (aceeasi marime, acelasi loc, aceeasi culoare)
        paperSocket.socketActive = false;
        Destroy(sheet);
        if (paperSpotMarker != null) paperSpotMarker.SetActive(false);

        if (paperMaterial != null)
        {
            for (int i = 0; i < laidPaperPieces.Length; i++)
            {
                laidPaperPieces[i].sharedMaterial = paperMaterial;
            }
        }
        laidPaper.SetActive(true);
    }

    // ---------- 2. clapele indoite ----------
    // O apeleaza fiecare clapa (PaperFlap) cand s-a fixat.
    public void OnFlapFolded()
    {
        foldedFlaps++;
        if (foldedFlaps >= flaps.Length)
        {
            StartCoroutine(FinishWrapping());
        }
    }

    IEnumerator FinishWrapping()
    {
        yield return new WaitForSeconds(0.3f);

        // foaia indoita devine conul din jurul buchetului
        laidPaper.SetActive(false);

        if (paperMaterial != null) wrapCone.sharedMaterial = paperMaterial;
        wrapCone.enabled = true;
        if (wrapConeCollider != null) wrapConeCollider.enabled = true;
        if (wrapConeGrow != null) wrapConeGrow.Play();
        if (puffEffect != null) puffEffect.Play();

        bouquet.isWrapped = true;
        bouquet.wrapColor = paperColor;

        // acum se poate lega panglica
        ribbonSocket.gameObject.SetActive(true);
    }

    // ---------- 3. panglica legata ----------
    void OnRibbonPlaced(SelectEnterEventArgs args)
    {
        if (!bouquet.isWrapped || bouquet.isTied) return;

        GameObject piece = args.interactableObject.transform.gameObject;
        WrapItem item = piece.GetComponent<WrapItem>();
        if (item != null) ribbonColor = item.colorName;

        Renderer pieceRenderer = piece.GetComponentInChildren<Renderer>();
        if (pieceRenderer != null)
        {
            Renderer bowRenderer = ribbonBow.GetComponent<Renderer>();
            if (bowRenderer != null) bowRenderer.sharedMaterial = pieceRenderer.sharedMaterial;
        }

        StartCoroutine(TieRibbon(piece));
    }

    IEnumerator TieRibbon(GameObject piece)
    {
        yield return null;

        ribbonSocket.socketActive = false;
        Destroy(piece);
        ribbonSocket.gameObject.SetActive(false);

        ribbonBow.SetActive(true);   // GrowIn de pe fundita o face sa creasca
        bouquet.isTied = true;

        if (doneEffect != null) doneEffect.Play();

        // buchetul e gata, acum poate fi luat de pe masa
        if (bouquetGrab != null) bouquetGrab.enabled = true;
    }
}
