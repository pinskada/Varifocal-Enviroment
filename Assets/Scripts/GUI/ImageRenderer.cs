using UnityEngine;
using Contracts;

public class ImageRenderer : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.RawImage leftEyeImage;
    [SerializeField] private UnityEngine.UI.RawImage rightEyeImage;

    [SerializeField] private UnityEngine.UI.AspectRatioFitter leftEyeAspectFitter;
    [SerializeField] private UnityEngine.UI.AspectRatioFitter rightEyeAspectFitter;


    void Update()
    {
        // Dequeue the image if present
        if (GUIQueueContainer.eyePreviewQueue.TryDequeue(out var image))
        {
            if (leftEyeImage == null || rightEyeImage == null)
            {
                Debug.LogError("[ImageRenderer] Left or Right eye RawImage is not assigned.");
                return;
            }

            if (image.rawData == null)
            {
                Debug.LogError("[ImageRenderer] Received null image data.");
                return;
            }

            // Create Texture2D from raw data
            Texture2D tex = image.rawData;

            // Calculate aspect ratio
            float aspect = (float)tex.width / tex.height;

            // Apply image to the correct RawImage and change aspect ratio
            if (image.eyeSide == EyeSide.Left && leftEyeImage != null)
            {
                leftEyeImage.texture = tex;
                leftEyeAspectFitter.aspectRatio = aspect;
            }
            else if (image.eyeSide == EyeSide.Right && rightEyeImage != null)
            {
                rightEyeImage.texture = tex;
                rightEyeAspectFitter.aspectRatio = aspect;
            }
        }
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
}
