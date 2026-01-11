using UnityEngine;
using System.Collections;

public class DragAnimation : MonoBehaviour
{
    [SerializeField] public Sprite[] angryEyebrowsSelect;
    [SerializeField] public Sprite[] angryEyesSelect;
    [SerializeField] public Sprite[] angrySkinSelect1;
    [SerializeField] public Sprite[] angrySkinSelect2;
    [SerializeField] public Sprite[] angryOutfitSelect1;
    [SerializeField] public Sprite[] angryOutfitSelect2;
    public bool playAnimation = false;
    public EditPlayerLook editPlayerLook;
    private Sprite originalEyebrows;
    private Sprite originalEyes;
    private Sprite originalSkin;
    private Sprite originalOutfit;

    public void Awake()
    {
        editPlayerLook = GetComponentInChildren<EditPlayerLook>(true);
    }
    public void StartDragAnimation() {
        Debug.Log("Start drag animation");
        originalEyebrows = editPlayerLook.eyebrowsRenderer.sprite;
        originalEyes = editPlayerLook.eyesRenderer.sprite;
        originalSkin = editPlayerLook.skinRenderer.sprite;
        originalOutfit = editPlayerLook.outfitRenderer.sprite;
        playAnimation = true;
        editPlayerLook.eyebrowsRenderer.sprite = angryEyebrowsSelect[editPlayerLook.networkData.eyebrowsIndex.Value];
        editPlayerLook.eyebrowsRenderer.SetNativeSize();
        editPlayerLook.eyesRenderer.sprite = angryEyesSelect[editPlayerLook.networkData.eyesIndex.Value];
        editPlayerLook.eyesRenderer.SetNativeSize();
        editPlayerLook.skinRenderer.sprite = angrySkinSelect1[editPlayerLook.networkData.skinIndex.Value];
        editPlayerLook.skinRenderer.SetNativeSize();
        editPlayerLook.outfitRenderer.sprite = angryOutfitSelect1[editPlayerLook.networkData.outfitIndex.Value];
        editPlayerLook.outfitRenderer.SetNativeSize();
        StartCoroutine(DragAnimationCoroutine());
    }

    public void EndDragAnimation() {
        Debug.Log("End drag animation");
        playAnimation = false;
        editPlayerLook.eyebrowsRenderer.sprite = originalEyebrows;
        editPlayerLook.eyebrowsRenderer.SetNativeSize();
        editPlayerLook.eyesRenderer.sprite = originalEyes;
        editPlayerLook.eyesRenderer.SetNativeSize();
        editPlayerLook.skinRenderer.sprite = originalSkin;
        editPlayerLook.skinRenderer.SetNativeSize();
        editPlayerLook.outfitRenderer.sprite = originalOutfit;
        editPlayerLook.outfitRenderer.SetNativeSize();
    }

    private IEnumerator DragAnimationCoroutine()
    {
        int skinIndex = editPlayerLook.networkData.skinIndex.Value;
        int outfitIndex = editPlayerLook.networkData.outfitIndex.Value;
        bool toggle = false;

        while (playAnimation)
        {
            if (toggle){
                editPlayerLook.skinRenderer.sprite = angrySkinSelect1[skinIndex];
                editPlayerLook.skinRenderer.SetNativeSize();
                editPlayerLook.outfitRenderer.sprite = angryOutfitSelect1[outfitIndex];
                editPlayerLook.outfitRenderer.SetNativeSize();
            }
            else{
                editPlayerLook.skinRenderer.sprite = angrySkinSelect2[skinIndex];
                editPlayerLook.skinRenderer.SetNativeSize();
                editPlayerLook.outfitRenderer.sprite = angryOutfitSelect2[outfitIndex];
                editPlayerLook.outfitRenderer.SetNativeSize();
            }

            toggle = !toggle;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
