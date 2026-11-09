using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Wrapping station on the assembling counter.
// 1. Pull a sheet from a roll and put it on the marked spot.
// 2. Put the flowers on the paper (BouquetZone).
// 3. Fold the 2 flaps over the flowers (left, right).
// 4. Bring a ribbon to the bouquet base -> a bow appears.
// 5. The bouquet is done and can be picked up.
// While you hold paper, a flower or a ribbon, a white outline shows where it goes.
public class WrappingStation : MonoBehaviour
{
    [Header("Bouquet")]
    public Bouquet bouquet;
    public XRGrabInteractable bouquetGrab;
    public bool lockBouquetUntilDone = true;   // the bouquet can't be picked up until it is tied

    [Header("Paper")]
    public WrapSocket paperSocket;             // spot on the table for the paper
    public GameObject paperSpotMarker;         // outline of the paper spot (only while holding paper)
    public GameObject laidPaper;               // paper laid on the table: middle + 2 flaps
    public Renderer[] laidPaperPieces;         // paper pieces, to apply the color
    public PaperFlap[] flaps;                  // the folding flaps

    [Header("Cone and ribbon (from the bouquet)")]
    public Renderer wrapCone;
    public GrowIn wrapConeGrow;
    public GameObject ribbonBow;
    public WrapSocket ribbonSocket;            // spot at the bouquet base for the ribbon

    [Header("Outlines (shown only while holding the matching item)")]
    public GameObject flowerHint;              // where the flowers go on the paper
    public GameObject ribbonHint;              // ring at the bouquet base

    [Header("Effects (optional)")]
    public ParticleSystem puffEffect;
    public ParticleSystem doneEffect;

    [Header("Player choices")]
    public string paperColor = "";
    public string ribbonColor = "";

    Material paperMaterial;
    Collider wrapConeCollider;
    bool paperPlaced;
    int foldedFlaps;

    // what the player holds right now
    bool holdingPaper;
    bool holdingRibbon;
    bool holdingFlower;
    List<IXRInteractor> interactors = new List<IXRInteractor>();

    void Start()
    {
        laidPaper.SetActive(false);

        // the cone is hidden and can't be touched until the bouquet is wrapped
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
        // flaps can fold only when the paper is down and has flowers on it
        bool ready = laidPaper.activeSelf && bouquet.flowers.Count > 0 && !bouquet.isWrapped;
        for (int i = 0; i < flaps.Length; i++)
        {
            flaps[i].canFold = ready;
        }

        // an outline shows only while holding the item that goes there
        CheckHands();
        if (paperSpotMarker != null) paperSpotMarker.SetActive(holdingPaper && !paperPlaced);
        if (flowerHint != null) flowerHint.SetActive(holdingFlower && !bouquet.isWrapped);
        if (ribbonHint != null) ribbonHint.SetActive(holdingRibbon && bouquet.isWrapped && !bouquet.isTied);
    }

    // Checks what each hand holds (sockets are skipped, they are not hands).
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

    // ---------- 1. paper placed on the table ----------
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
        // let the sheet settle on the table
        yield return new WaitForSeconds(0.25f);

        // then swap it for the laid paper (same size, place and color)
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

    // ---------- 2. flaps folded ----------
    // Called by each PaperFlap when it stays folded.
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

        // the folded paper becomes the cone around the bouquet
        laidPaper.SetActive(false);

        if (paperMaterial != null) wrapCone.sharedMaterial = paperMaterial;
        wrapCone.enabled = true;
        if (wrapConeCollider != null) wrapConeCollider.enabled = true;
        if (wrapConeGrow != null) wrapConeGrow.Play();
        if (puffEffect != null) puffEffect.Play();

        bouquet.isWrapped = true;
        bouquet.wrapColor = paperColor;

        // now the ribbon can be tied
        ribbonSocket.gameObject.SetActive(true);
    }

    // ---------- 3. ribbon tied ----------
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

        ribbonBow.SetActive(true);   // GrowIn on the bow makes it grow
        bouquet.isTied = true;

        if (doneEffect != null) doneEffect.Play();

        // the bouquet is done and can be picked up
        if (bouquetGrab != null) bouquetGrab.enabled = true;
    }
}
