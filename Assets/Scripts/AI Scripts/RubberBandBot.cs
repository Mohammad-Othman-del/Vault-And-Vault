using UnityEngine;
using System.Collections.Generic;

public class RubberBandBot : MonoBehaviour
{
    private struct Breadcrumb
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;

        public Breadcrumb(Vector3 pos, Quaternion rot, float time)
        {
            position = pos;
            rotation = rot;
            timestamp = time;
        }
    }

    [Header("Target")]
    public Transform player;
    public float catchDistance = 1.5f;
    public float startDelay = 2.5f;

    [Header("Breadcrumb Settings")]
    [Tooltip("How often (in seconds) to record the player's position.")]
    public float recordInterval = 0.02f;
    [Tooltip("How close the bot needs to be to a breadcrumb to consume it.")]
    public float waypointThreshold = 0.15f;

    [Header("Rubber Banding Speeds")]
    public float baseSpeed = 10.0f;
    public float catchUpSpeed = 15.2f;
    public float superCatchUpSpeed = 28.0f;
    public float slowDownSpeed = 7.5f;

    [Header("Rubber Banding Distances")]
    public float catchUpDistance = 15f;
    public float extremeCatchUpDistance = 25f;
    public float slowDownDistance = 5f;

    [Header("Level State")]
    public bool isActivated = false;
    public static bool isCinematicPlaying = false;
    public static bool isLevelFinished = false;

    private Queue<Breadcrumb> breadcrumbs = new Queue<Breadcrumb>();
    private float lastRecordTime;
    private bool canMove = false;
    private float gameStartTime;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private Transform meshHolder;
    private Light lightComp;
    private CharacterController controller;

    void Awake()
    {
        isLevelFinished = false;

        // Rule 1: Level 1 has NO chasing bot at all
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Level 1", System.StringComparison.OrdinalIgnoreCase))
        {
            gameObject.SetActive(false);
            return;
        }

        // Rule 2 & 3: Level 2 and Level 3 start deactivated until player steals data fragment
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Level 2", System.StringComparison.OrdinalIgnoreCase) ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            isActivated = false;
        }
        else
        {
            isActivated = true;
        }

        // Slightly slower speed tuning for Level 3
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            baseSpeed = 10.8f;     // Tuned base speed
            catchUpSpeed = 15.2f;  // Tuned catchup speed
            slowDownSpeed = 7.5f;   // Tuned minimum speed
        }

        InitPhysicsController();
        BuildEveVisualModel();
    }

    private void InitPhysicsController()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        controller.radius = 0.35f;
        controller.height = 1.2f;
        controller.center = new Vector3(0f, 0.6f, 0f);
        controller.detectCollisions = false; // Ghost mode: allow passing through all buildings like Boo from Super Mario World!
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;

        gameStartTime = Time.time;
        lastRecordTime = Time.time;

        if (player != null && initialSpawnPosition == Vector3.zero)
        {
            initialSpawnPosition = player.position - player.forward * 4f + Vector3.up * 1f;
            initialSpawnRotation = player.rotation;
            
            if (controller != null) controller.enabled = false;
            transform.position = initialSpawnPosition;
            transform.rotation = initialSpawnRotation;
            if (controller != null) controller.enabled = true;
        }

        if (meshHolder != null)
        {
            meshHolder.gameObject.SetActive(isActivated);
        }
    }

    private bool wasAIDisabledLastFrame = false;

    void Update()
    {
        if (player == null) return;

        // If bot is not activated yet (e.g. Level 2 before fragment collection), do nothing
        if (!isActivated) return;

        // Cheat Check: Disable AI chasing
        if (PauseMenuManager.disableAI)
        {
            wasAIDisabledLastFrame = true;
            canMove = false;

            // Continue recording player breadcrumbs so the bot maintains the pursuit path
            if (!isCinematicPlaying)
            {
                RecordBreadcrumb();
            }
            return;
        }
        else if (wasAIDisabledLastFrame)
        {
            // Player re-enabled AI chasing in Pause Menu! Restore movement and resume pursuit
            wasAIDisabledLastFrame = false;
            if (!isCinematicPlaying)
            {
                canMove = true;
            }

            if (breadcrumbs.Count == 0 && player != null)
            {
                breadcrumbs.Enqueue(new Breadcrumb(player.position, player.rotation, Time.time));
            }
        }

        // Procedural EVE Floating Hover Animation
        if (meshHolder != null)
        {
            meshHolder.localPosition = Vector3.up * (Mathf.Sin(Time.time * 3.5f) * 0.12f);
        }

        RecordBreadcrumb();

        // Bot will NOT move or catch while cinematic is playing or after level is completed
        if (!isActivated || isCinematicPlaying || !canMove || isLevelFinished || Time.timeScale == 0f) return;

        MoveAlongPath();
        CheckCatch();
    }

    public void ActivateBot()
    {
        isActivated = true;
        canMove = false;
        gameStartTime = Time.time;
        lastRecordTime = Time.time;
        breadcrumbs.Clear();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (meshHolder == null)
        {
            BuildEveVisualModel();
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.Equals("Level 2", System.StringComparison.OrdinalIgnoreCase))
        {
            // Level 2 fixed spawn point: (-22.18, 8.0, 60.114), regardless of player position or facing direction
            Vector3 level2SpawnPos = new Vector3(-22.18f, 8.0f, 60.114f);
            if (controller != null) controller.enabled = false;
            transform.position = level2SpawnPos;
            if (player != null)
            {
                Vector3 lookDir = player.position - level2SpawnPos;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
            if (controller != null) controller.enabled = true;
        }
        else if (sceneName.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            // Level 3 fixed spawn point: (-17.5, 13.74, 48.457), regardless of player position or facing direction
            Vector3 level3SpawnPos = new Vector3(-17.5f, 13.74f, 48.457f);
            if (controller != null) controller.enabled = false;
            transform.position = level3SpawnPos;
            if (player != null)
            {
                Vector3 lookDir = player.position - level3SpawnPos;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
            if (controller != null) controller.enabled = true;
        }
        else if (player != null)
        {
            // Spawn the bot 7.5 meters behind the player when activated
            if (controller != null) controller.enabled = false;
            transform.position = player.position - player.forward * 7.5f + Vector3.up * 1.5f;
            transform.rotation = player.rotation;
            if (controller != null) controller.enabled = true;
        }

        if (meshHolder != null)
        {
            meshHolder.gameObject.SetActive(true);
        }

        // Trigger dramatic over-the-shoulder cinematic camera reveal!
        StopAllCoroutines();
        StartCoroutine(PlayCinematicRevealSequence());

        Debug.Log("Chasing AI Bot Activated with Cinematic Reveal!");
    }

    private System.Collections.IEnumerator PlayCinematicRevealSequence()
    {
        isCinematicPlaying = true;
        canMove = false;

        MouseLook mouseLook = FindAnyObjectByType<MouseLook>();
        PlayerMovement movement = player != null ? player.GetComponent<PlayerMovement>() : FindAnyObjectByType<PlayerMovement>();
        Camera cam = Camera.main;

        // Instantly freeze player physics, velocity, parkour states, and safely ground if mid-air
        if (movement != null)
        {
            movement.FreezeForCinematic();
        }

        if (cam != null && player != null)
        {
            Transform camTransform = cam.transform;
            Transform originalParent = camTransform.parent;
            Vector3 originalLocalPos = camTransform.localPosition;
            Quaternion originalLocalRot = camTransform.localRotation;
            float originalFOV = cam.fieldOfView;

            // Unparent camera during cutscene for free 3D camera pan & dolly
            camTransform.SetParent(null);

            // Cutscene Camera Anchors
            Vector3 eyePos = player.position + Vector3.up * 1.6f;
            Quaternion startRot = camTransform.rotation;

            // Over-the-shoulder wide crane shot framing EVE hovering 7.5m behind player
            Vector3 cutsceneCamPos = player.position + player.right * 1.8f + player.up * 1.8f - player.forward * 2.8f;
            Vector3 botHeadPos = transform.position + Vector3.up * 1.2f;
            Quaternion cutsceneCamRot = Quaternion.LookRotation(botHeadPos - cutsceneCamPos);

            // Phase 1: Smooth 3D Arcing Pull-Back Sweep Shot (0.9 seconds)
            float elapsed = 0f;
            float sweepDuration = 0.9f;
            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / sweepDuration);
                camTransform.position = Vector3.Lerp(eyePos, cutsceneCamPos, t);
                camTransform.rotation = Quaternion.Slerp(startRot, cutsceneCamRot, t);
                cam.fieldOfView = Mathf.Lerp(originalFOV, 54f, t);

                // Orient bot face (+Z, black visor & cyan eyes) directly toward cutscene camera
                Vector3 faceCamDir = camTransform.position - transform.position;
                faceCamDir.y = 0f;
                if (faceCamDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(faceCamDir);

                yield return null;
            }

            // Phase 2: Slow Dolly Zoom & Eye Power-Up Ignition (1.5 seconds)
            if (lightComp != null) lightComp.intensity = 9f;
            
            float dollyElapsed = 0f;
            float dollyDuration = 1.5f;
            Vector3 dollyTargetPos = cutsceneCamPos + (botHeadPos - cutsceneCamPos).normalized * 0.8f;
            
            while (dollyElapsed < dollyDuration)
            {
                dollyElapsed += Time.deltaTime;
                float dt = dollyElapsed / dollyDuration;
                
                // Slow subtle dolly zoom on EVE's face
                camTransform.position = Vector3.Lerp(cutsceneCamPos, dollyTargetPos, Mathf.SmoothStep(0f, 1f, dt));
                camTransform.rotation = Quaternion.LookRotation(botHeadPos - camTransform.position);
                
                // Keep bot face pointing directly into the cutscene camera lens
                Vector3 faceCamDir = camTransform.position - transform.position;
                faceCamDir.y = 0f;
                if (faceCamDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(faceCamDir);

                // Cinematic FOV lens punch when eyes flare (54 -> 46 -> 54)
                cam.fieldOfView = 54f - Mathf.Sin(dt * Mathf.PI) * 8f;
                yield return null;
            }
            if (lightComp != null) lightComp.intensity = 3.5f;

            // Phase 3: Seamless Fly-In Back into First-Person Eyes (0.8 seconds)
            elapsed = 0f;
            float flyInDuration = 0.8f;
            Vector3 lastCutscenePos = camTransform.position;
            Quaternion lastCutsceneRot = camTransform.rotation;

            // In Level 3: Smoothly align final eye rotation to point directly at the open exit door (-31.29905, 10.69943, 53.51)
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Vector3 level3ExitDoorPos = new Vector3(-31.29905f, 10.69943f, 53.51f);
            Quaternion targetEndRot = startRot;

            if (activeScene.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase) && player != null)
            {
                Vector3 dirToDoor = level3ExitDoorPos - (player.position + Vector3.up * 1.6f);
                if (dirToDoor != Vector3.zero)
                {
                    targetEndRot = Quaternion.LookRotation(dirToDoor);
                }
            }

            while (elapsed < flyInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / flyInDuration);
                
                Vector3 targetEyePos = player.position + Vector3.up * 1.6f;
                camTransform.position = Vector3.Lerp(lastCutscenePos, targetEyePos, t);
                camTransform.rotation = Quaternion.Slerp(lastCutsceneRot, targetEndRot, t);
                cam.fieldOfView = Mathf.Lerp(54f, originalFOV, t);
                yield return null;
            }

            // In Level 3: Orient player body and MouseLook pitch straight at the open exit door
            if (activeScene.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase) && mouseLook != null)
            {
                mouseLook.SetFacingDirection(level3ExitDoorPos);
            }

            // Re-parent camera back to player head slot cleanly
            camTransform.SetParent(originalParent);
            camTransform.localPosition = originalLocalPos;
            if (!activeScene.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
            {
                camTransform.localRotation = originalLocalRot;
            }
            cam.fieldOfView = originalFOV;
        }

        // Restore player movement and control FIRST!
        isCinematicPlaying = false;

        // Bot waits for exactly 0.8 seconds AFTER camera returns and control is restored before beginning pursuit
        canMove = false;
        yield return new WaitForSeconds(0.8f);
        
        canMove = true;
        gameStartTime = Time.time;
    }

    public void ResetBotToSpawn()
    {
        breadcrumbs.Clear();
        canMove = false;
        gameStartTime = Time.time;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if ((sceneName == "Level 2" || sceneName == "Level 3") && !CollectibleItem.IsCollected)
        {
            isActivated = false;
            if (meshHolder != null) meshHolder.gameObject.SetActive(false);
            return;
        }

        if (controller != null) controller.enabled = false;
        if (sceneName.Equals("Level 2", System.StringComparison.OrdinalIgnoreCase))
        {
            Vector3 level2SpawnPos = new Vector3(-22.18f, 8.0f, 60.114f);
            transform.position = level2SpawnPos;
            if (player != null)
            {
                Vector3 lookDir = player.position - level2SpawnPos;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else if (sceneName.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            Vector3 level3SpawnPos = new Vector3(-17.5f, 13.74f, 48.457f);
            transform.position = level3SpawnPos;
            if (player != null)
            {
                Vector3 lookDir = player.position - level3SpawnPos;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else if (player != null)
        {
            transform.position = player.position - player.forward * 5f + Vector3.up * 1.2f;
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }
        else
        {
            transform.position = initialSpawnPosition;
            transform.rotation = initialSpawnRotation;
        }
        if (controller != null) controller.enabled = true;

        if (meshHolder != null)
        {
            meshHolder.gameObject.SetActive(isActivated);
        }
    }

    private void RecordBreadcrumb()
    {
        if (Time.time - lastRecordTime >= recordInterval)
        {
            breadcrumbs.Enqueue(new Breadcrumb(player.position, player.rotation, Time.time));
            lastRecordTime = Time.time;
        }
    }

    private void MoveAlongPath()
    {
        if (breadcrumbs.Count == 0) return;

        Breadcrumb targetWaypoint = breadcrumbs.Peek();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float currentSpeed = baseSpeed;

        if (distanceToPlayer > extremeCatchUpDistance)
        {
            // Extreme distance gap: dynamically scale speed up to 45 m/s to rapidly close distance!
            float distanceRatio = distanceToPlayer / extremeCatchUpDistance;
            currentSpeed = Mathf.Lerp(catchUpSpeed, superCatchUpSpeed * distanceRatio, 0.5f);
            currentSpeed = Mathf.Clamp(currentSpeed, catchUpSpeed, 45f);
        }
        else if (distanceToPlayer > catchUpDistance)
        {
            // Moderate distance gap (15m - 25m): 15.2 m/s
            currentSpeed = catchUpSpeed;
        }
        else if (distanceToPlayer < slowDownDistance)
        {
            // Close range (< 5m): 7.5 m/s
            currentSpeed = slowDownSpeed;
        }

        // Ghost Movement: Float directly through world geometry like Boo from Super Mario World!
        Vector3 moveDelta = targetWaypoint.position - transform.position;
        float maxDistanceThisFrame = currentSpeed * Time.deltaTime;
        
        if (moveDelta.magnitude > 0.001f)
        {
            Vector3 stepVelocity = Vector3.ClampMagnitude(moveDelta, maxDistanceThisFrame);
            transform.position += stepVelocity;
        }

        // Smoothly rotate the bot to face directly toward the player so the player sees the bot's face (eyes & visor)
        if (player != null)
        {
            Vector3 dirToPlayer = player.position - transform.position;
            dirToPlayer.y = 0f;
            if (dirToPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= waypointThreshold)
        {
            breadcrumbs.Dequeue();
        }
    }

    private void CheckCatch()
    {
        if (isLevelFinished || Time.timeScale == 0f || !isActivated || !canMove) return;

        if (Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            Debug.Log("Bot Caught Player -> Show Game Over Modal!");
            
            // Immediately disable bot movement to prevent multi-frame catch loops!
            isActivated = false;
            canMove = false;

            GameOverManager gom = FindAnyObjectByType<GameOverManager>();
            if (gom == null)
            {
                GameObject gomGo = new GameObject("GameOverManager");
                gom = gomGo.AddComponent<GameOverManager>();
            }
            gom.ShowGameOver("CAUGHT BY AI BOT");
        }
    }

    public void DisableBotOnVictory()
    {
        isActivated = false;
        canMove = false;
        isLevelFinished = true;
        if (meshHolder != null)
        {
            meshHolder.gameObject.SetActive(false);
        }
        breadcrumbs.Clear();
    }

    // Procedural EVE (WALL-E) High-Level 3D Model Generation
    private void BuildEveVisualModel()
    {
        // Don't build duplicate meshes if already present
        Transform existingHolder = transform.Find("EveMeshHolder");
        if (existingHolder != null)
        {
            meshHolder = existingHolder;
            return;
        }

        GameObject holderGo = new GameObject("EveMeshHolder");
        holderGo.transform.SetParent(transform, false);
        meshHolder = holderGo.transform;
        meshHolder.localScale = new Vector3(1.05f, 1.05f, 1.05f); // Imposing larger bot size

        // URP & Built-in Compatible Ghost Materials (Slightly transparent sheen, highly visible)
        Color pearlWhite = new Color(0.95f, 0.96f, 0.98f, 0.82f); // 82% opacity body
        Color jetBlack = new Color(0.01f, 0.01f, 0.01f, 0.88f);   // 88% opacity visor
        Color darkMetal = new Color(0.08f, 0.09f, 0.12f, 0.82f);  // 82% opacity collar
        Color cyanEyeColor = new Color(0f, 0.88f, 1f, 0.95f);     // 95% opacity cyan eyes

        Material whiteGlossMat = CreateEveMaterial(pearlWhite, 0.95f, 0.15f, Color.clear);
        Material blackVisorMat = CreateEveMaterial(jetBlack, 0.98f, 0.1f, Color.clear);
        Material darkMetalMat = CreateEveMaterial(darkMetal, 0.8f, 0.5f, Color.clear);
        Material cyanGlowMat = CreateEveMaterial(cyanEyeColor, 1.0f, 0f, cyanEyeColor * 3.5f);

        // 1. Body (Sleek Pearl White Egg Capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "EveBody";
        body.transform.SetParent(meshHolder, false);
        body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        body.transform.localScale = new Vector3(0.68f, 0.82f, 0.68f);
        Destroy(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial = whiteGlossMat;

        // 2. Inner Collar Cavity (Dark Ring where head floats above)
        GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        collar.name = "EveCollar";
        collar.transform.SetParent(meshHolder, false);
        collar.transform.localPosition = new Vector3(0f, 1.22f, 0f);
        collar.transform.localScale = new Vector3(0.52f, 0.06f, 0.52f);
        Destroy(collar.GetComponent<Collider>());
        collar.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        // 3. Head (Floating Glossy Pearl Dome)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "EveHead";
        head.transform.SetParent(meshHolder, false);
        head.transform.localPosition = new Vector3(0f, 1.48f, 0.02f);
        head.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
        head.transform.localScale = new Vector3(0.66f, 0.50f, 0.62f);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().sharedMaterial = whiteGlossMat;

        // 4. Black Visor Screen (Embedded Glossy Black Glass)
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visor.name = "EveVisor";
        visor.transform.SetParent(head.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0.03f, 0.26f);
        visor.transform.localScale = new Vector3(0.74f, 0.68f, 0.55f);
        Destroy(visor.GetComponent<Collider>());
        visor.GetComponent<Renderer>().sharedMaterial = blackVisorMat;

        // 5. Glowing Cyan Digital Eyes
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "LeftEye";
        leftEye.transform.SetParent(head.transform, false);
        leftEye.transform.localPosition = new Vector3(-0.16f, 0.06f, 0.51f);
        leftEye.transform.localScale = new Vector3(0.2f, 0.09f, 0.1f);
        leftEye.transform.localRotation = Quaternion.Euler(0f, -12f, 6f);
        Destroy(leftEye.GetComponent<Collider>());
        leftEye.GetComponent<Renderer>().sharedMaterial = cyanGlowMat;

        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "RightEye";
        rightEye.transform.SetParent(head.transform, false);
        rightEye.transform.localPosition = new Vector3(0.16f, 0.06f, 0.51f);
        rightEye.transform.localScale = new Vector3(0.2f, 0.09f, 0.1f);
        rightEye.transform.localRotation = Quaternion.Euler(0f, 12f, -6f);
        Destroy(rightEye.GetComponent<Collider>());
        rightEye.GetComponent<Renderer>().sharedMaterial = cyanGlowMat;

        // 6. Floating Side Arm Pods
        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftArm.name = "LeftArm";
        leftArm.transform.SetParent(meshHolder, false);
        leftArm.transform.localPosition = new Vector3(-0.46f, 0.78f, 0.02f);
        leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        leftArm.transform.localScale = new Vector3(0.13f, 0.36f, 0.13f);
        Destroy(leftArm.GetComponent<Collider>());
        leftArm.GetComponent<Renderer>().sharedMaterial = whiteGlossMat;

        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightArm.name = "RightArm";
        rightArm.transform.SetParent(meshHolder, false);
        rightArm.transform.localPosition = new Vector3(0.46f, 0.78f, 0.02f);
        rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
        rightArm.transform.localScale = new Vector3(0.13f, 0.36f, 0.13f);
        Destroy(rightArm.GetComponent<Collider>());
        rightArm.GetComponent<Renderer>().sharedMaterial = whiteGlossMat;

        // 7. Bottom Hover Thruster Ring
        GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        thruster.name = "EveThrusterRing";
        thruster.transform.SetParent(meshHolder, false);
        thruster.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        thruster.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
        Destroy(thruster.GetComponent<Collider>());
        thruster.GetComponent<Renderer>().sharedMaterial = cyanGlowMat;

        // 8. Cyan Underglow Point Light
        GameObject lightGo = new GameObject("EveUnderglow");
        lightGo.transform.SetParent(meshHolder, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        lightComp = lightGo.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = cyanEyeColor;
        lightComp.intensity = 3.5f;
        lightComp.range = 6f;
    }

    private Material CreateEveMaterial(Color color, float smoothness, float metallic, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        
        Material mat = new Material(shader);

        // Configure Transparent Surface Mode for Ghost Effect
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);     // 0 = Alpha Blend
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3);       // 3 = Transparent in Standard

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

        if (emissionColor != Color.clear)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            if (mat.HasProperty("_Emission")) mat.SetColor("_Emission", emissionColor);
        }

        return mat;
    }
}
