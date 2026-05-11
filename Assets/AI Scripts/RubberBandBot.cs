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
    public float startDelay = 3f;

    [Header("Breadcrumb Settings")]
    [Tooltip("How often (in seconds) to record the player's position.")]
    public float recordInterval = 0.05f;
    [Tooltip("How close the bot needs to be to a breadcrumb to consume it.")]
    public float waypointThreshold = 0.2f;

    [Header("Rubber Banding Speeds")]
    public float baseSpeed = 12f;
    public float catchUpSpeed = 18f;
    public float slowDownSpeed = 8f;

    [Header("Rubber Banding Distances")]
    public float catchUpDistance = 15f;
    public float slowDownDistance = 5f;

    private Queue<Breadcrumb> breadcrumbs = new Queue<Breadcrumb>();
    private float lastRecordTime;
    private bool canMove = false;
    private float gameStartTime;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        gameStartTime = Time.time;
        lastRecordTime = Time.time;

        // Initialize bot at player's start position so it comes out of the same spawn
        if (player != null)
        {
            transform.position = player.position;
            transform.rotation = player.rotation;
        }
    }

    void Update()
    {
        if (player == null) return;

        RecordBreadcrumb();

        // Handle Start Delay
        if (!canMove)
        {
            if (Time.time - gameStartTime >= startDelay)
            {
                canMove = true;
            }
            return;
        }

        MoveAlongPath();
        CheckCatch();
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

        // Peak at the next breadcrumb without removing it yet
        Breadcrumb targetWaypoint = breadcrumbs.Peek();

        // Calculate dynamic Rubber Band speed
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float currentSpeed = baseSpeed;

        if (distanceToPlayer > catchUpDistance)
        {
            currentSpeed = catchUpSpeed; // Player is far, speed up!
        }
        else if (distanceToPlayer < slowDownDistance)
        {
            currentSpeed = slowDownSpeed; // Player is close, give them a chance!
        }

        // Move towards the breadcrumb
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, currentSpeed * Time.deltaTime);
        
        // Rotate smoothly
        transform.rotation = Quaternion.Slerp(transform.rotation, targetWaypoint.rotation, currentSpeed * 2f * Time.deltaTime);

        // If we reached the breadcrumb, consume it so we target the next one on the next frame
        if (Vector3.Distance(transform.position, targetWaypoint.position) <= waypointThreshold)
        {
            breadcrumbs.Dequeue();
        }
    }

    private void CheckCatch()
    {
        if (Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            Debug.Log("Bot Caught Player!");
            // Add your Game Over or Restart Logic here
        }
    }
}
