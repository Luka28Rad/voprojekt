using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct UILocation
{
    public float left;
    public float right;
    public float top;
    public float bottom;
}
public class TrainAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform trainRoot;
    [SerializeField] private UILocation left;
    [SerializeField] private UILocation right;
    [SerializeField] private Image trainImage;
    [SerializeField] private float frameRate = 8f;
    [SerializeField] private Sprite frameA;
    [SerializeField] private Sprite frameB;
    [SerializeField] public float duration = 1.0f;
    [SerializeField] int travelDirection = 0; // 0 = left, 1 = right

    private Vector2 startPos;
    private Vector2 endPos;
    private Coroutine routine;
    private Coroutine animRoutine;
    
    private void Awake()
    {
        if (trainRoot == null) trainRoot = (RectTransform)transform;
        //trainImage = trainRoot.GetComponentInChildren<Sprite>();
        Vector2 leftPos = ToAnchoredPosition(left);
        Vector2 rightPos = ToAnchoredPosition(right);
        if (travelDirection == 0)
        {
            startPos = rightPos;
            endPos = leftPos;
        }
        else if(travelDirection == 1)
        {
            trainRoot.localEulerAngles = new Vector3(0f, 180f, 0f);
            startPos = leftPos;
            endPos = rightPos;
        }
    }
    public void PlayAnimation()
    {
        if (routine != null) StopCoroutine(TrainMoving());
        if (animRoutine != null) StopCoroutine(SpriteLoop());
        routine = StartCoroutine(TrainMoving());
        animRoutine = StartCoroutine(SpriteLoop());
    }
    private Vector2 ToAnchoredPosition(UILocation location)
    {
        return new Vector2(
                (location.left - location.right) * 0.5f,
                (location.bottom - location.top) * 0.5f
            );
    }
    private IEnumerator TrainMoving()
    {
        trainRoot.anchoredPosition = startPos;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            trainRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, a);
            yield return null;
        }
        trainRoot.anchoredPosition = endPos;
        routine = null;
    }
    private IEnumerator SpriteLoop()
    {
        float delay = 1f / Mathf.Max(1f, frameRate);
        bool toggle = false;
        while(routine != null)
        {
            trainImage.sprite = toggle ? frameA : frameB;
            toggle = !toggle;
            yield return new WaitForSeconds(delay);
        }
        trainImage.sprite = frameA;
        animRoutine = null;
    }
}
