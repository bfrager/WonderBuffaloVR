using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RectSizeMatcher : MonoBehaviour
{
    public RectTransform sourceRect;
    public RectTransform targetRect;

    public Vector2 offset;

    void Update()
    {
        if (sourceRect == null || targetRect == null)
            return;

        targetRect.anchorMin = sourceRect.anchorMin;
        targetRect.anchorMax = sourceRect.anchorMax;

        Vector2 position = sourceRect.anchoredPosition;
        position.y += offset.y;

        targetRect.anchoredPosition = position;

        Vector2 size = sourceRect.sizeDelta;
        size.x += offset.x * 2;
        size.y += offset.y * 2;

        targetRect.sizeDelta = size;

        targetRect.pivot = sourceRect.pivot;
    }
}
