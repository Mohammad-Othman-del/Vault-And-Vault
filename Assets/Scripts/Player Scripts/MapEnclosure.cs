using UnityEngine;

[ExecuteAlways]
public class MapEnclosure : MonoBehaviour
{
    [Header("Box Dimensions")]
    [Tooltip("Width (X), Height (Y), and Depth (Z) of the enclosure in world units.")]
    public Vector3 size = new Vector3(100f, 50f, 100f);

    [Header("Grid Appearance")]
    public Color baseColor     = new Color(0.02f, 0.01f, 0.05f, 1f);
    public Color gridColor     = new Color(1f,    0.05f, 0.05f, 1f);
    public float gridScale     = 15f;
    public float lineWidth     = 0.025f;
    public float glowWidth     = 0.18f;
    public float glowIntensity = 1.8f;
    public float pulseSpeed    = 1.5f;
    public float pulseAmount   = 0.18f;

    private GameObject box;
    private Material mat;

    void OnEnable()  => Build();
    void OnDisable() => Teardown();

    void OnValidate()
    {
        if (box != null)  box.transform.localScale = size;
        if (mat  != null) ApplyToMaterial(mat);
    }

    void Build()
    {
        Teardown();

        Shader shader = Shader.Find("Custom/GridWall");
        if (shader == null)
        {
            Debug.LogError("[MapEnclosure] Shader 'Custom/GridWall' not found. Make sure GridWall.shader is in Assets/Shaders/.");
            return;
        }

        mat = new Material(shader);
        ApplyToMaterial(mat);

        box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name      = "MapEnclosureBox";
        box.hideFlags = HideFlags.HideAndDontSave;
        box.transform.SetParent(transform, false);
        box.transform.localPosition = Vector3.zero;
        box.transform.localScale    = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var col = box.GetComponent<BoxCollider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    void Teardown()
    {
        if (box == null) return;
        if (Application.isPlaying) Destroy(box);
        else DestroyImmediate(box);
        box = null;
        mat = null;
    }

    void ApplyToMaterial(Material m)
    {
        m.SetColor("_WallBase",      baseColor);
        m.SetColor("_WallGrid",      gridColor);
        m.SetFloat("_GridScale",     gridScale);
        m.SetFloat("_LineWidth",     lineWidth);
        m.SetFloat("_GlowWidth",     glowWidth);
        m.SetFloat("_GlowIntensity", glowIntensity);
        m.SetFloat("_PulseSpeed",    pulseSpeed);
        m.SetFloat("_PulseAmount",   pulseAmount);
    }
}
