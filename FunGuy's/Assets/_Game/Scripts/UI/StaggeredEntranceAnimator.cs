using System.Collections;
using UnityEngine;

public class StaggeredEntranceAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] groups;
    [SerializeField] private RectTransform[] cards;
    [SerializeField] private float delayBetween = 0.06f;
    [SerializeField] private float fadeDuration = 0.20f;
    [SerializeField] private float slideDistance = 18f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool unscaledTime = true;

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (groups == null || groups.Length == 0) yield break;

        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            if (group == null) continue;

            Vector2 startPos = Vector2.zero;
            Vector2 endPos = Vector2.zero;
            RectTransform card = (cards != null && i < cards.Length) ? cards[i] : group.GetComponent<RectTransform>();
            if (card != null)
            {
                endPos = card.anchoredPosition;
                startPos = endPos + new Vector2(0f, -slideDistance);
                card.anchoredPosition = startPos;
            }

            group.alpha = 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += DeltaTime();
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeDuration));
                group.alpha = p;
                if (card != null) card.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
                yield return null;
            }

            group.alpha = 1f;
            if (card != null) card.anchoredPosition = endPos;

            float delay = 0f;
            while (delay < delayBetween)
            {
                delay += DeltaTime();
                yield return null;
            }
        }
    }

    private float DeltaTime()
    {
        return unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
