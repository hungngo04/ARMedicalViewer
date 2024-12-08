using UnityEngine;
using System.Collections.Generic;

public enum Axis { X, Y, Z }

public class TextureManager
{
    public Texture2D ResizeTexture(Texture2D originalTexture, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture.active = rt;
        Graphics.Blit(originalTexture, rt);

        Texture2D resizedTexture = new Texture2D(width, height);
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();

        RenderTexture.active = null;
        rt.Release();

        return resizedTexture;
    }

    public Texture3D Create3DTexture(List<Texture2D> textures, int width, int height)
    {
        Texture3D texture3D = new Texture3D(width, height, textures.Count, TextureFormat.RGBA32, false);
        Color[] colorArray = new Color[width * height * textures.Count];

        for (int z = 0; z < textures.Count; z++)
        {
            Color[] sliceColors = textures[z].GetPixels();
            sliceColors.CopyTo(colorArray, z * width * height);
        }

        texture3D.SetPixels(colorArray);
        texture3D.Apply();

        return texture3D;
    }

    public Texture2D GetSliceFrom3DTexture(Texture3D texture3D, Axis axis, int sliceIndex)
    {
        int width = texture3D.width;
        int height = texture3D.height;
        int depth = texture3D.depth;

        Texture2D sliceTexture;
        Color[] sliceColors;

        switch (axis)
        {
            case Axis.X:
                sliceTexture = new Texture2D(height, depth);
                sliceColors = new Color[height * depth];
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        sliceColors[y + z * height] = texture3D.GetPixel(sliceIndex, y, z);
                    }
                }
                break;

            case Axis.Y:
                sliceTexture = new Texture2D(width, depth);
                sliceColors = new Color[width * depth];
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        sliceColors[x + z * width] = texture3D.GetPixel(x, sliceIndex, z);
                    }
                }
                break;

            case Axis.Z:
                sliceTexture = new Texture2D(width, height);
                sliceColors = new Color[width * height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        sliceColors[x + y * width] = texture3D.GetPixel(x, y, sliceIndex);
                    }
                }
                break;

            default:
                throw new System.ArgumentException("Invalid axis specified.");
        }

        sliceTexture.SetPixels(sliceColors);
        sliceTexture.Apply();

        return sliceTexture;
    }
}