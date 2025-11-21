using UnityEngine;
using Contracts;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class ImageRenderer : MonoBehaviour, ImageDestroyer
{
    [SerializeField] private RawImage leftEyeImage;
    [SerializeField] private RawImage rightEyeImage;

    [SerializeField] private AspectRatioFitter leftEyeAspectFitter;
    [SerializeField] private AspectRatioFitter rightEyeAspectFitter;
    // [SerializeField] private RectTransform leftOverlayRoot;
    // [SerializeField] private RectTransform rightOverlayRoot;
    [SerializeField] private RectTransform leftCross;
    // [SerializeField] private RectTransform leftCircle;
    [SerializeField] private RectTransform rightCross;
    // [SerializeField] private RectTransform rightCircle;

    private Texture2D _leftTex, _rightTex;
    private int printCounter = 0;
    private bool displayTextures = false;

    private void Start()
    {
        if (leftEyeImage == null || rightEyeImage == null)
        {
            Debug.LogError("[ImageRenderer] RawImage components not assigned.");
        }

        // var crossTex = GenerateCrossTexture(1000, 30);
        // var circleTex = GenerateCircleTexture(1000, 30);

        // var crossSprite = MakeSprite(crossTex);
        // var circleSprite = MakeSprite(circleTex);

        // leftCross.sprite = crossSprite;
        // rightCross.sprite = crossSprite;

        // leftCircle.sprite = circleSprite;
        // rightCircle.sprite = circleSprite;

        // // Optional: keep aspect, tint, etc.
        // leftCross.preserveAspect = true;
        // rightCross.preserveAspect = true;
        // leftCircle.preserveAspect = true;
        // rightCircle.preserveAspect = true;
    }

    void OnDisable()
    {
        // Clean up explicitly to avoid leaked native textures in Editor
        if (_leftTex) Destroy(_leftTex);
        if (_rightTex) Destroy(_rightTex);
        if (leftEyeImage) leftEyeImage.texture = null;
        if (rightEyeImage) rightEyeImage.texture = null;
    }

    void Update()
    {
        var hasImages = GUIQueueContainer.images.TryDequeue(out var images);
        var hasTrackerData = GUIQueueContainer.trackerData.TryDequeue(out var trackerData);

        if (!displayTextures)
        {
            if (_leftTex != null || _rightTex != null)
                ClearTextures();
            return;
        }
        if (hasImages && images != null)
            RenderEyeImage(images);

        if (hasTrackerData)
        {
            RenderOverlay(trackerData.left_eye, isLeft: true);
            RenderOverlay(trackerData.right_eye, isLeft: false);
        }
    }

    private void RenderEyeImage(List<EyeImage> images)
    {
        foreach (var eye in images)
        {
            try
            {
                printCounter++;
                //if (printCounter % 30 == 0)
                //Debug.Log($"[ImageRenderer] Decoded image for eye {eye.EyeId}: {eye.Width}x{eye.Height}");

                var isLeft = eye.EyeId == 0;
                ref Texture2D dstTex = ref (isLeft ? ref _leftTex : ref _rightTex);

                var targetImg = isLeft ? leftEyeImage : rightEyeImage;
                var targetFitter = isLeft ? leftEyeAspectFitter : rightEyeAspectFitter;

                if (targetImg == null || targetFitter == null) continue;

                var bytes = eye.Data;

                // Reject clearly bad frames before touching native decoder
                if (!IsLikelyImage(bytes))
                {
                    Debug.LogWarning($"[ImageRenderer] Dropped non-image frame for eye {eye.EyeId} (len={bytes?.Length}).");
                    continue;
                }

                // Lazily create once; LoadImage will resize as needed.
                if (dstTex == null)
                    dstTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                // IMPORTANT: markNonReadable = true to drop CPU copy after upload
                // Also validates the JPEG; returns false on corrupt input.
                if (!dstTex.LoadImage(eye.Data, markNonReadable: true))
                {
                    Debug.LogWarning($"[ImageRenderer] JPEG decode failed for eye {eye.EyeId} (bytes={eye.Data?.Length}).");
                    continue;
                }

                // Apply to UI
                targetFitter.aspectRatio = (float)dstTex.width / dstTex.height;
                targetImg.texture = dstTex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommRouter]HandlePreviewImage: Failed to load image for eye {eye.EyeId}: {ex.Message}");
            }
        }
    }

    private void RenderOverlay(EyeData tracker_data, bool isLeft)
    {
        var cross = isLeft ? leftCross : rightCross;
        var tex = isLeft ? _leftTex : _rightTex;
        var img = isLeft ? leftEyeImage : rightEyeImage;

        if (cross == null || tex == null || img == null)
            return;

        if (!tracker_data.is_valid)
        {
            cross.gameObject.SetActive(false);
            return;
        }

        cross.gameObject.SetActive(true);

        // Rect of the preview on screen
        RectTransform imgRectT = img.rectTransform;
        Rect imgRect = imgRectT.rect;

        // pixel -> normalized [0..1]
        float nx = tracker_data.center_x / tex.width;
        float ny = tracker_data.center_y / tex.height;

        // normalized -> UI units in the preview rect
        float uiX = nx * imgRect.width;
        float uiY = (1f - ny) * imgRect.height; // flip Y

        // our cross anchor is bottom-left of RightImage/LeftImage,
        // so shift from preview bottom-left to parent bottom-left:
        // (assumes RightImage/LeftImage is the direct parent of the preview)
        Vector2 offsetInParent = imgRectT.anchoredPosition;

        Vector2 pos = offsetInParent + new Vector2(uiX, uiY);

        cross.anchoredPosition = pos;

        // Debug.Log("Rendererd overlay: " +
        //     $"eye={(isLeft ? "left" : "right")}, " +
        //     $"pos=({pos.x:F1}, {pos.y:F1}), " +
        //     $"radius=({tracker_data.radius_x:F1}, {tracker_data.radius_y:F1})");
    }


    private static bool IsLikelyImage(byte[] data)
    {
        if (data == null || data.Length < 8)
            return false;

        // --- JPEG ---
        if (data[0] == 0xFF && data[1] == 0xD8 &&
            data[^2] == 0xFF && data[^1] == 0xD9)
            return true;

        // --- PNG ---
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 &&
            data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A &&
            data[6] == 0x1A && data[7] == 0x0A)
            return true;

        return false;
    }


    public void ControlTextures(bool displayTextures)
    {
        //Debug.Log($"[ImageRenderer] Display textures set to {displayTextures}");
        this.displayTextures = displayTextures;
    }


    private void ClearTextures()
    {
        //Debug.Log("[ImageRenderer] Clearing textures.");
        if (_leftTex)
        {
            Destroy(_leftTex);
            _leftTex = null;
        }
        if (_rightTex)
        {
            Destroy(_rightTex);
            _rightTex = null;
        }
        if (leftEyeImage) leftEyeImage.texture = null;
        if (rightEyeImage) rightEyeImage.texture = null;
    }

    private Texture2D Rotate(Texture2D src, bool clockwise)
    {
        // Helper method for rotating texture 90 degrees clockwise or counter-clockwise

        int width = src.width;
        int height = src.height;

        // Rotated texture has swapped width/height
        Texture2D rotated = new Texture2D(height, width, src.format, false);
        Color32[] srcPixels = src.GetPixels32();
        Color32[] rotatedPixels = new Color32[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcIndex = y * width + x;
                int destX = clockwise ? height - y - 1 : y;
                int destY = clockwise ? x : width - x - 1;
                int destIndex = destY * height + destX;

                rotatedPixels[destIndex] = srcPixels[srcIndex];
            }
        }

        rotated.SetPixels32(rotatedPixels);
        rotated.Apply();
        return rotated;
    }


    private Texture2D Flip(Texture2D src, bool vertical)
    {
        // Helper method for flipping texture vertically or horizontally

        int width = src.width;
        int height = src.height;

        Texture2D flipped = new Texture2D(width, height, src.format, false);
        Color32[] srcPixels = src.GetPixels32();
        Color32[] flippedPixels = new Color32[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            int flippedY = vertical ? (height - 1 - y) : y;

            for (int x = 0; x < width; x++)
            {
                int flippedX = vertical ? x : (width - 1 - x);

                int srcIndex = y * width + x;
                int destIndex = flippedY * width + flippedX;

                flippedPixels[destIndex] = srcPixels[srcIndex];
            }
        }

        flipped.SetPixels32(flippedPixels);
        flipped.Apply();
        return flipped;
    }

    private Texture2D GenerateCircleTexture(int size, int thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(255, 0, 0, 255);

        int radius = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - radius;
                int dy = y - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Draw a ring with thickness
                if (dist > radius - thickness && dist < radius)
                    tex.SetPixel(x, y, red);
                else
                    tex.SetPixel(x, y, clear);
            }
        }

        tex.Apply();
        return tex;
    }

    private Texture2D GenerateCrossTexture(int size, int thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(255, 0, 0, 255);

        tex.SetPixels32(new Color32[size * size]); // clear

        for (int i = 0; i < size; i++)
        {
            // Vertical bar
            for (int t = -thickness; t <= thickness; t++)
                tex.SetPixel(size / 2 + t, i, red);

            // Horizontal bar
            for (int t = -thickness; t <= thickness; t++)
                tex.SetPixel(i, size / 2 + t, red);
        }

        tex.Apply();
        return tex;
    }

    private Sprite MakeSprite(Texture2D tex)
    {
        return Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f); // pivot center, 100 px/unit
    }
}
