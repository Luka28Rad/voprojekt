using System.Collections;
using UnityEngine;

public class NewspaperAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform newspaperRoot;
    [SerializeField] public float duration=1.0f;
    [SerializeField] private float startYoffset = -900f;

    private Vector2 endPos;
    private Coroutine routine;

    private void Awake()
    {
        if (newspaperRoot == null) newspaperRoot = (RectTransform)transform;
        endPos = newspaperRoot.anchoredPosition;
    }

    public void PlayAnimation()
    {
        if (routine != null) StopCoroutine(SlideIn());
        routine = StartCoroutine(SlideIn());
    }
    
    private IEnumerator SlideIn()
    {
        var startPos = new Vector2(endPos.x, endPos.y + startYoffset);
        newspaperRoot.anchoredPosition = startPos;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            newspaperRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, a);
            yield return null;
        }

        newspaperRoot.anchoredPosition = endPos;
        routine = null;
    }
}
