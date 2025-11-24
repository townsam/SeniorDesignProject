using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

using System.Collections.Generic;

public class ActorBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public float jumpForce = 5f;
    public int plannedSteps = 50;
    public float minStepDuration = 1.0f;
    public float maxStepDuration = 3.0f;
    public bool loopPath = true;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    private Rigidbody rb;
    private List<MovementCommand> movementPlan = new List<MovementCommand>();
    private int currentStep = 0;
    private float stepEndTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        GenerateMovementPlan();
        StartStep(0);
    }

    void FixedUpdate()
    {
        if (currentStep >= movementPlan.Count) return;

        // Check if it's time to move to the next step
        if (Time.time >= stepEndTime)
        {
            currentStep++;

            if (currentStep >= movementPlan.Count)
            {
                if (loopPath)
                    StartStep(0);
                return;
            }

            StartStep(currentStep);
        }

        // Continue moving in the current direction
        var cmd = movementPlan[currentStep];
        Vector3 move = cmd.direction * speed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    void StartStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= movementPlan.Count) return;

        var cmd = movementPlan[stepIndex];
        stepEndTime = Time.time + cmd.duration;

        // Execute jump if planned
        if (cmd.shouldJump && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void GenerateMovementPlan()
    {
        movementPlan.Clear();

        for (int i = 0; i < plannedSteps; i++)
        {
            float x = UnityEngine.Random.Range(-1f, 1f);
            float z = UnityEngine.Random.Range(-1f, 1f);
            float duration = UnityEngine.Random.Range(minStepDuration, maxStepDuration);
            bool jump = UnityEngine.Random.value < 0.2f; // 20% chance to include a jump

            movementPlan.Add(new MovementCommand(new Vector3(x, 0, z), duration, jump));
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public void InitializeWithSeed(int seed)
    {
        UnityEngine.Random.InitState(seed);
        GenerateMovementPlan();
    }

    public void ResetSteps()
    {
        currentStep = 0;
    }

    // Useful for ML integration or debugging
    public List<MovementCommand> GetMovementPlan() => movementPlan;
}

[System.Serializable]
public struct MovementCommand
{
    public Vector3 direction;
    public float duration;
    public bool shouldJump;

    public MovementCommand(Vector3 dir, float dur, bool jump)
    {
        direction = dir.normalized;
        duration = dur;
        shouldJump = jump;
    }
}

