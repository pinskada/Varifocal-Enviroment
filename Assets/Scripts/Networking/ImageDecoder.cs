using System;
using System.Collections.Generic;
using UnityEngine;

public class ImageDecoder
{
    const int MAX_IMAGE_SIZE = 5 * 1024 * 1024; // 5 MB

    public struct EyeImage
    {
        public int EyeId;     // 0 = left, 1 = right
        public int Width;
        public int Height;
        public byte[] Data;   // compressed image bytes
    }

    public static List<EyeImage> Decode(byte[] payload)
    {
        List<EyeImage> images = new List<EyeImage>();
        int offset = 0;

        int frameHeaderSize = 1; // 1 byte for number of images
        int eyeHeaderSize = 9; // 1 byte EyeId + 2 bytes Width + 2 bytes Height + 4 bytes Size
        int cummSize = frameHeaderSize; // cumulative size of headers and image data

        int payloadLength = payload.Length;

        if (!CheckPayloadLength(payloadLength, cummSize)) return new List<EyeImage>();

        // Frame header
        int count = payload[offset];
        offset += 1;

        if (count <= 0)
        {
            Debug.LogWarning("[ImageDecoder] No images in payload.");
            return new List<EyeImage>(); // return empty list
        }

        for (int i = 0; i < count; i++)
        {
            cummSize += eyeHeaderSize;
            if (!CheckPayloadLength(payloadLength, cummSize)) return new List<EyeImage>();

            int eyeId = payload[offset];
            offset += 1;

            int width = BitConverter.ToUInt16(payload, offset);
            offset += 2;

            int height = BitConverter.ToUInt16(payload, offset);
            offset += 2;

            int size = BitConverter.ToInt32(payload, offset);
            offset += 4;

            cummSize += size;

            if (size <= 0 || width <= 0 || height <= 0)
            {
                Debug.LogWarning($"[ImageDecoder] Invalid image metadata (EyeId: {eyeId}, Width: {width}, Height: {height}, Size: {size}).");
                return new List<EyeImage>();
            }

            if (size > MAX_IMAGE_SIZE)
            {
                Debug.LogWarning($"[ImageDecoder] Image size {size} exceeds limit.");
                return new List<EyeImage>();
            }

            if (!CheckPayloadLength(payloadLength, cummSize)) return new List<EyeImage>();

            byte[] data = new byte[size];
            Buffer.BlockCopy(payload, offset, data, 0, size);
            offset += size;

            images.Add(new EyeImage { EyeId = eyeId, Width = width, Height = height, Data = data });
        }

        return images;
    }

    public static bool CheckPayloadLength(int payloadLength, int minExpectedLength)
    {
        if (payloadLength < minExpectedLength)
        {
            Debug.LogWarning("[ImageDecoder] Payload too short.");
            return false;
        }
        return true;
    }
}
