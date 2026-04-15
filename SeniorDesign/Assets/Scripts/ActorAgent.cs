using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ActorAgent : Agent
{
    /// <summary>Count of floats in <see cref="CollectObservations"/>; must match Behavior Parameters &gt; Space Size.</summary>
    public const int ExpectedVectorObservationSize = 9;

    [Header("References")]
    public ActorBehavior actorBehavior;
    public Transform target;

    [Header("Agent Settings")]
    public float decisionMoveSpeed = 3f;
    public float decisionJumpForce = 5f;
    public float successDistance = 1.5f;
    public float maxEpisodeSeconds = 10;
    [Tooltip("Per-decision penalty (negative). Helps encourage faster solutions; keep small.")]
    public float stepPenalty = -0.0001f;
    [Tooltip("Reward scale for reducing distance to the target this step (delta-distance shaping).")]
    public float progressRewardScale = 0.05f;

    [Header("ML training")]
    [Tooltip("When the Python trainer is connected, episode ends after this many decisions (Max Step). Keeps episode length aligned with PPO summary_freq so you see completed episodes more often. 0 = use Behavior Parameters > Max Step only.")]
    public int maxTrainingDecisionSteps = 512;

    private Rigidbody rb;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float episodeClockStart;
    private float previousDistanceToTarget;
    private bool hasPreviousDistance;
    private bool hasLoggedActionSizeWarning;
    private bool hasLoggedStartupConfig;
    private int inspectorMaxStep = -1;
    private int trainingFlagDiagnosticFrames;
    private bool trainingFlagDiagnosticDone;
    private bool lastPlannedMovementEnabled;
    private int trainingProgressDiagFrames;
    private float trainingProgressDiagAbsDeltaSum;
    private int trainingProgressDiagSamples;
    private bool trainingProgressDiagLogged;

    void LateUpdate()
    {
        // Keep planned-movement mode synced even if the trainer connects after Initialize().
        if (actorBehavior != null)
        {
            bool shouldPlan = ShouldUsePlannedMovementFallback();
            if (shouldPlan != lastPlannedMovementEnabled)
            {
                actorBehavior.SetPlannedMovementEnabled(shouldPlan);
                lastPlannedMovementEnabled = shouldPlan;
            }
        }

        if (trainingFlagDiagnosticDone)
        {
            return;
        }

        if (Academy.Instance == null || !Academy.Instance.IsCommunicatorOn)
        {
            return;
        }

        trainingFlagDiagnosticFrames++;
        if (trainingFlagDiagnosticFrames < 30)
        {
            return;
        }

        if (FlagEffectRegistry.GetActiveProviderCount() == 0)
        {
            UnityEngine.Debug.LogWarning(
                "ActorAgent: ML training is active but FlagEffectRegistry has no FlagEffectProvider components. " +
                "Observations stay at neutral multipliers (1,1,1). Add MlTrainingFlagSeeder to the scene or place flags, then simulate with the trainer connected.");
        }

        trainingFlagDiagnosticDone = true;
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (actorBehavior == null)
        {
            actorBehavior = GetComponent<ActorBehavior>();
        }

        if (actorBehavior != null)
        {
            actorBehavior.SetPlannedMovementEnabled(ShouldUsePlannedMovementFallback());
        }

        LogStartupConfiguration();
        ApplyMaxStepForTrainingMode();
        EnsureDecisionRequesterForTraining();
    }

    private void ApplyMaxStepForTrainingMode()
    {
        if (inspectorMaxStep < 0)
        {
            inspectorMaxStep = MaxStep;
        }

        bool training = Academy.Instance != null && Academy.Instance.IsCommunicatorOn;
        if (training && maxTrainingDecisionSteps > 0)
        {
            MaxStep = maxTrainingDecisionSteps;
        }
        else
        {
            MaxStep = inspectorMaxStep;
        }
    }

    private void EnsureDecisionRequesterForTraining()
    {
        if (Academy.Instance == null || !Academy.Instance.IsCommunicatorOn)
        {
            return;
        }

        // If the scene/prefab forgot to include a DecisionRequester, training can look "stuck".
        var dr = GetComponent<DecisionRequester>();
        if (dr == null)
        {
            dr = gameObject.AddComponent<DecisionRequester>();
            dr.DecisionPeriod = 1;
            dr.TakeActionsBetweenDecisions = true;
        }
    }

    private void LogStartupConfiguration()
    {
        if (hasLoggedStartupConfig)
        {
            return;
        }

        hasLoggedStartupConfig = true;

        var behaviorParameters = GetComponent<BehaviorParameters>();
        if (behaviorParameters == null)
        {
            UnityEngine.Debug.LogWarning("ActorAgent: Missing BehaviorParameters component.");
            return;
        }

        int vectorObsSize = behaviorParameters.BrainParameters.VectorObservationSize;
        if (vectorObsSize != ExpectedVectorObservationSize)
        {
            UnityEngine.Debug.LogWarning(
                $"ActorAgent: Behavior Parameters vector observation size is {vectorObsSize} but CollectObservations writes {ExpectedVectorObservationSize}. Update Behavior Parameters > Observations.");
        }

        int configuredContinuousActions = -1;
        int configuredDiscreteActions = -1;

        try
        {
            object brainParameters = behaviorParameters.GetType().GetProperty("BrainParameters")?.GetValue(behaviorParameters);
            if (brainParameters != null)
            {
                object actionSpec = brainParameters.GetType().GetProperty("ActionSpec")?.GetValue(brainParameters);
                if (actionSpec != null)
                {
                    object numContinuous = actionSpec.GetType().GetProperty("NumContinuousActions")?.GetValue(actionSpec);
                    if (numContinuous is int continuous)
                    {
                        configuredContinuousActions = continuous;
                    }

                    object numDiscrete = actionSpec.GetType().GetProperty("NumDiscreteActions")?.GetValue(actionSpec);
                    if (numDiscrete is int discrete)
                    {
                        configuredDiscreteActions = discrete;
                    }
                }

                if (configuredContinuousActions < 0)
                {
                    object vectorActionSizeObj = brainParameters.GetType().GetProperty("VectorActionSize")?.GetValue(brainParameters);
                    if (vectorActionSizeObj is int[] vectorActionSize && vectorActionSize.Length > 0)
                    {
                        configuredContinuousActions = vectorActionSize[0];
                    }
                }
            }
        }
        catch
        {
        }

        string continuousLabel = configuredContinuousActions >= 0 ? configuredContinuousActions.ToString() : "unknown";
        string discreteLabel = configuredDiscreteActions >= 0 ? configuredDiscreteActions.ToString() : "unknown";

        UnityEngine.Debug.Log(
            $"ActorAgent Config: BehaviorName='{behaviorParameters.BehaviorName}', BehaviorType={behaviorParameters.BehaviorType}, VectorObs={vectorObsSize} (expected {ExpectedVectorObservationSize}), ContinuousActions={continuousLabel}, DiscreteActions={discreteLabel}, ExpectedContinuousActions=3");

        if (configuredContinuousActions >= 0 && configuredContinuousActions != 3)
        {
            UnityEngine.Debug.LogWarning($"ActorAgent: Expected 3 continuous actions but Behavior Parameters is configured for {configuredContinuousActions}. Update Behavior Parameters > Actions.");
        }
    }

    private bool ShouldUsePlannedMovementFallback()
    {
        bool trainerConnected = Academy.Instance != null && Academy.Instance.IsCommunicatorOn;
        if (trainerConnected)
        {
            return false;
        }

        var behaviorParameters = GetComponent<BehaviorParameters>();
        if (behaviorParameters != null)
        {
            if (behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
            {
                return false;
            }

            if (behaviorParameters.BehaviorType == BehaviorType.InferenceOnly && behaviorParameters.Model != null)
            {
                return false;
            }
        }

        return true;
    }

    public override void OnEpisodeBegin()
    {
        ForceResetActor();
        // During training, Time.timeScale may not be 1; unscaled time keeps maxEpisodeSeconds = wall seconds.
        episodeClockStart = UseTrainingEpisodeClock() ? Time.unscaledTime : Time.time;
        if (target != null)
        {
            previousDistanceToTarget = Vector3.Distance(transform.position, target.position);
            hasPreviousDistance = true;
        }
        else
        {
            previousDistanceToTarget = 0f;
            hasPreviousDistance = false;
        }
    }

    private bool UseTrainingEpisodeClock()
    {
        return Academy.Instance != null && Academy.Instance.IsCommunicatorOn;
    }

    private float EpisodeClockNow()
    {
        return UseTrainingEpisodeClock() ? Time.unscaledTime : Time.time;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        sensor.AddObservation(actorBehavior != null && actorBehavior.IsGrounded() ? 1f : 0f);

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            Vector3 localToTarget = transform.InverseTransformDirection(toTarget);

            sensor.AddObservation(localToTarget.x);
            sensor.AddObservation(localToTarget.z);
            sensor.AddObservation(toTarget.magnitude);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        var influence = FlagEffectRegistry.GetCombinedInfluence(transform);
        sensor.AddObservation(influence.MoveSpeedMultiplier);
        sensor.AddObservation(influence.JumpForceMultiplier);
        sensor.AddObservation(influence.RewardMultiplier);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var influence = FlagEffectRegistry.GetCombinedInfluence(transform);

        int continuousCount = actions.ContinuousActions.Length;
        if (!hasLoggedActionSizeWarning && continuousCount < 3)
        {
            UnityEngine.Debug.LogWarning($"ActorAgent: Expected 3 continuous actions but received {continuousCount}. Missing actions default to 0. Check Behavior Parameters > Actions.");
            hasLoggedActionSizeWarning = true;
        }

        float moveX = Mathf.Clamp(GetContinuousAction(actions, 0), -1f, 1f);
        float moveZ = Mathf.Clamp(GetContinuousAction(actions, 1), -1f, 1f);
        float jumpSignal = Mathf.Clamp(GetContinuousAction(actions, 2), -1f, 1f);

        // Actions are interpreted in the actor's local X/Z to match observations (local-to-target).
        Vector3 localMove = new Vector3(moveX, 0f, moveZ);
        Vector3 worldMove = transform.TransformDirection(localMove) * (decisionMoveSpeed * influence.MoveSpeedMultiplier);
        rb.linearVelocity = new Vector3(worldMove.x, rb.linearVelocity.y, worldMove.z);

        if (jumpSignal > 0.5f && actorBehavior != null && actorBehavior.IsGrounded())
        {
            rb.AddForce(Vector3.up * (decisionJumpForce * influence.JumpForceMultiplier), ForceMode.Impulse);
        }

        AddReward((stepPenalty + influence.ContinuousReward) * influence.RewardMultiplier);

        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            float delta = 0f;
            if (hasPreviousDistance)
            {
                delta = previousDistanceToTarget - distance; // positive when moving closer
            }

            previousDistanceToTarget = distance;
            hasPreviousDistance = true;

            // Primary shaping: reward progress toward target.
            // Clamp delta for stability (meters per decision).
            float clampedDelta = Mathf.Clamp(delta, -1f, 1f);
            AddReward(clampedDelta * progressRewardScale * influence.RewardMultiplier);

            // Training-only: log once if progress signal is effectively ~0 (common when decision frequency is too low or movement is overridden).
            if (!trainingProgressDiagLogged && Academy.Instance != null && Academy.Instance.IsCommunicatorOn)
            {
                trainingProgressDiagFrames++;
                if (trainingProgressDiagFrames > 10)
                {
                    trainingProgressDiagAbsDeltaSum += Mathf.Abs(delta);
                    trainingProgressDiagSamples++;
                }

                if (trainingProgressDiagSamples >= 60)
                {
                    float meanAbsDelta = trainingProgressDiagAbsDeltaSum / Mathf.Max(1, trainingProgressDiagSamples);
                    if (meanAbsDelta < 0.0005f)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"ActorAgent: progress shaping signal is near-zero (mean |Δdistance| ≈ {meanAbsDelta:0.000000}). " +
                            "This usually means decisions are too infrequent or another script is overriding movement. " +
                            "Confirm DecisionRequester is active and ActorBehavior planned movement is disabled during training.");
                    }

                    trainingProgressDiagLogged = true;
                }
            }

            if (distance <= successDistance)
            {
                AddReward(1f * influence.RewardMultiplier);
                EndEpisode();
            }
        }

        if (EpisodeClockNow() - episodeClockStart >= maxEpisodeSeconds)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        if (continuousActions.Length > 0)
        {
            continuousActions[0] = Input.GetAxis("Horizontal");
        }

        if (continuousActions.Length > 1)
        {
            continuousActions[1] = Input.GetAxis("Vertical");
        }

        if (continuousActions.Length > 2)
        {
            continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : -1f;
        }
    }

    private float GetContinuousAction(ActionBuffers actions, int index)
    {
        if (index < 0 || index >= actions.ContinuousActions.Length)
        {
            return 0f;
        }

        return actions.ContinuousActions[index];
    }

    public void ForceResetActor()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
