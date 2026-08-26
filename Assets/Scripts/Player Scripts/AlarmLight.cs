using UnityEngine;

public class AlarmLight : MonoBehaviour
{
    [Header("Spin")]
    [Tooltip("Degrees per second. 180 = one full rotation every 2 seconds.")]
    public float spinSpeed = 180f;

    [Header("Beam")]
    public Color beamColor = Color.red;
    public float beamIntensity = 5f;
    public float beamRange = 30f;
    public float beamAngle = 40f;
    [Tooltip("Degrees below horizontal the beam points. 0 = perfectly horizontal.")]
    public float beamTiltDown = 10f;

    [Header("Ambient Glow")]
    [Tooltip("Constant omnidirectional red glow from the fixture itself.")]
    public float ambientIntensity = 0.8f;
    public float ambientRange = 8f;

    private Transform rotatingHead;

    void Awake()
    {
        // Fixed ambient point light — constant red glow on nearby surfaces
        Light ambient = gameObject.AddComponent<Light>();
        ambient.type      = LightType.Point;
        ambient.color     = beamColor;
        ambient.intensity = ambientIntensity;
        ambient.range     = ambientRange;
        ambient.shadows   = LightShadows.None;

        // Rotating head that holds the sweeping spotlight
        GameObject head = new GameObject("RotatingHead");
        head.transform.SetParent(transform, false);
        head.transform.localRotation = Quaternion.Euler(beamTiltDown, 0f, 0f);
        rotatingHead = head.transform;

        Light spot = head.AddComponent<Light>();
        spot.type       = LightType.Spot;
        spot.color      = beamColor;
        spot.intensity  = beamIntensity;
        spot.range      = beamRange;
        spot.spotAngle  = beamAngle;
        spot.shadows    = LightShadows.Soft;

        // Physical housing — small flat cylinder so the fixture is visible in the scene
        GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        housing.name = "LightHousing";
        housing.transform.SetParent(transform, false);
        housing.transform.localScale    = new Vector3(0.25f, 0.12f, 0.25f);
        housing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Destroy(housing.GetComponent<Collider>());

        Material housingMat = CreateSafeMaterial(new Color(0.12f, 0.12f, 0.12f), 0.5f, 0.5f, Color.clear);
        housing.GetComponent<Renderer>().material = housingMat;

        // Red lens cap on the front of the housing
        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.SetParent(housing.transform, false);
        lens.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        lens.transform.localScale    = new Vector3(0.7f, 0.4f, 0.7f);
        Destroy(lens.GetComponent<Collider>());

        Material lensMat = CreateSafeMaterial(new Color(1f, 0.05f, 0.05f), 0f, 0.9f, new Color(1.5f, 0f, 0f));
        lens.GetComponent<Renderer>().material = lensMat;
    }

    private Material CreateSafeMaterial(Color color, float metallic, float smoothness, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = (shader != null) ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (emissionColor != Color.clear)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            if (mat.HasProperty("_Emission")) mat.SetColor("_Emission", emissionColor);
        }

        return mat;
    }

    void Update()
    {
        if (rotatingHead != null)
        {
            rotatingHead.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}
