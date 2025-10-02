using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Contracts;

public class ImageSelector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private EyeSide eyeSide;
    [SerializeField] private RectTransform selectionBoxUI;
    private RectTransform imageRectTransform;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private bool isSelecting = false;
    private bool isCroped = false; // Flag to check if the image is cropped

    // Normalized coordinates (0-1) of the selected area (min/max)


    void Start()
    {
        imageRectTransform = GetComponent<RectTransform>();

        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (isCroped) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRectTransform, eventData.position, eventData.pressEventCamera, out startMousePos);
        isSelecting = true;

        if (selectionBoxUI != null)
        {
            selectionBoxUI.gameObject.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isCroped) return;

        if (!isSelecting) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRectTransform, eventData.position, eventData.pressEventCamera, out endMousePos);
        UpdateSelectionBox();
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (isCroped) return;

        if (!isSelecting) return;

        isSelecting = false;

        if (selectionBoxUI != null)
        {
            selectionBoxUI.gameObject.SetActive(false);
        }

        CalculateNormalizedCoordinates();
    }


    void UpdateSelectionBox()
    {
        if (selectionBoxUI == null) return;

        Vector2 size = endMousePos - startMousePos;
        selectionBoxUI.anchoredPosition = startMousePos + size / 2;
        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }


    void CalculateNormalizedCoordinates()
    {
        Vector2 imgSize = imageRectTransform.rect.size;

        Vector2 min = new Vector2(Mathf.Min(startMousePos.x, endMousePos.x), Mathf.Min(startMousePos.y, endMousePos.y));
        Vector2 max = new Vector2(Mathf.Max(startMousePos.x, endMousePos.x), Mathf.Max(startMousePos.y, endMousePos.y));

        Vector2 tempMin = min;
        Vector2 tempMax = max;

        min.y = 1 - tempMax.y;
        max.y = 1 - tempMin.y;

        List<List<float>> normalizedCoordinates = new List<List<float>>();

        if (eyeSide == EyeSide.Left)
        {
            normalizedCoordinates = new List<List<float>>
            {
                new List<float>() { RoundToThreeDecimals((min.y + imgSize.y / 2) / imgSize.y), RoundToThreeDecimals((max.y + imgSize.y / 2) / imgSize.y)},
                new List<float>() { RoundToThreeDecimals((min.x + imgSize.x / 2) / imgSize.x / 2), RoundToThreeDecimals((max.x + imgSize.x / 2) / imgSize.x) / 2}
            };
        }
        else if (eyeSide == EyeSide.Right)
        {
            normalizedCoordinates = new List<List<float>>
            {
                new List<float>() { RoundToThreeDecimals((min.y + imgSize.y / 2) / imgSize.y), RoundToThreeDecimals((max.y + imgSize.y / 2) / imgSize.y)},
                new List<float>() { RoundToThreeDecimals((min.x + imgSize.x / 2) / imgSize.x / 2 + 0.5f), RoundToThreeDecimals((max.x + imgSize.x / 2) / imgSize.x) / 2 + 0.5f}
            };
        }
        else
            Debug.LogError($"Wrong side assigned to ImageSelector: {eyeSide}");

        sendCrop(normalizedCoordinates);
        isCroped = true;
    }


    // Optional: Helper to get crop area in relative format
    private void sendCrop(List<List<float>> normalizedCoordinates)
    {
        if (eyeSide == EyeSide.Left)
            Debug.Log("Sending crop for left eye");
        //guiHub.SendConfig("tracker_config crop_left", normalizedCoordinates);
        else if (eyeSide == EyeSide.Right)
            Debug.Log("Sending crop for right eye");
        //guiHub.SendConfig("tracker_config crop_right", normalizedCoordinates);
        else
            Debug.LogError($"Wrong side assigned to ImageSelector: {eyeSide}");
    }


    public void resetScale()
    {
        List<List<float>> normalizedCoordinates;
        if (eyeSide == EyeSide.Left)
        {
            normalizedCoordinates = new List<List<float>>
            {
                new List<float>() {0f, 1f},
                new List<float>() {0f, 0.5f}
            };
        }
        else if (eyeSide == EyeSide.Right)
        {
            normalizedCoordinates = new List<List<float>>
            {
                new List<float>() {0f, 1f},
                new List<float>() {0.5f, 1f}
            };
        }
        else
        {
            Debug.LogError($"Wrong side assigned to ImageSelector: {eyeSide}");
            return;
        }
        isCroped = false;
        sendCrop(normalizedCoordinates);
    }


    private float RoundToThreeDecimals(float value) { return Mathf.Round(value * 1000f) / 1000f; }
}
