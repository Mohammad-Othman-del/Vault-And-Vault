#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CreateWindowCookie : MonoBehaviour
{
    [MenuItem("Tools/Create Window Cookie")]
    static void CreateCookie()
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // Fill black
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.black);

        // Window is roughly octagonal/rounded rectangle
        // Center and dimensions
        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float halfW = size * 0.36f;   // horizontal half-width of opening
        float halfH = size * 0.30f;   // vertical half-height of opening
        float cornerCut = size * 0.10f; // size of the diagonal corner cut

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float dy = Mathf.Abs(y - cy);

                // Inside the rectangular bounds
                if (dx <= halfW && dy <= halfH)
                {
                    // Cut the corners diagonally (octagon shape)
                    float cornerX = dx - (halfW - cornerCut);
                    float cornerY = dy - (halfH - cornerCut);

                    bool inCorner = cornerX > 0 && cornerY > 0 &&
                    (cornerX + cornerY) > cornerCut;

                    if (!inCorner)
                        tex.SetPixel(x, y, Color.white);
                }
            }
        }

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = "Assets/Sun Rays and Lights/WindowCookie.png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Cookie;
        importer.borderMipmap = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        Debug.Log("Octagonal window cookie created!");
    }
}
#endif
