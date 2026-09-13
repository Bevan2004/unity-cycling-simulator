using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers; 
using System.Linq; 
using Unity.Netcode; 

namespace SBPScripts
{
    [System.Serializable]
    public class CycleGeometry
    {
        public GameObject handles, lowerFork, fWheelVisual, RWheel, crank, lPedal, rPedal, fGear, rGear;
    }
    [System.Serializable]
    public class PedalAdjustments
    {
        public float crankRadius;
        public Vector3 lPedalOffset, rPedalOffset;
        public float pedalingSpeed;
    }
    [System.Serializable]
    public class WheelFrictionSettings
    {
        public PhysicsMaterial fPhysicMaterial, rPhysicMaterial;
        public Vector2 fFriction, rFriction;
    }
    [System.Serializable]
    public class WayPointSystem
    {
        public enum RecordingState { DoNothing, Record, Playback };
        public RecordingState recordingState = RecordingState.DoNothing;
        [Range(1, 10)]
        public int frameIncrement;
        [HideInInspector]
        public List<Vector3> bicyclePositionTransform;
        [HideInInspector]
        public List<Quaternion> bicycleRotationTransform;
        [HideInInspector]
        public List<Vector2Int> movementInstructionSet;
        [HideInInspector]
        public List<bool> sprintInstructionSet;
        [HideInInspector]
        public List<int> bHopInstructionSet;
    }
    [System.Serializable]
    public class AirTimeSettings
    {
        public bool freestyle;
        public float airTimeRotationSensitivity;
        [Range(0.5f, 10)]
        public float heightThreshold;
        public float groundSnapSensitivity;
    }

    public class BicycleController : NetworkBehaviour 
    {
        [Header("Zwift Physics Engine")]
        public bool useZwiftMode = true;
        public SplineController splineController; 
        
        [Header("Rider Specs")]
        public float riderMassKg = 75f;
        public float bikeMassKg = 10f;
        public float CdA = 0.42f; 
        public float Crr = 0.004f;
        
        [Header("Visuals")]
        public float leanAggressiveness = 1.5f; 
        public float leanSmoothing = 5.0f;
        public float zwiftSwayDamper = 0.1f;
        
        [Header("Ground Alignment")]
        public float raycastOffset = 1.0f;
        public LayerMask groundLayers = ~0; 

        [Header("Live Readout")]
        public float inputWatts = 0f; 
        public float currentSpeedKph; 
        public float currentGradePercent;

        [Header("Ghost Mode Settings")]
        public bool isGhost = false;       
        public float ghostInputSpeed = 0f; 

        // --- REMOTE ANIMATION VARIABLES ---
        private Vector3 _lastPosition;
        private float _calculatedGhostSpeed; 
        // ---------------------------------

        private float _currentVelocityMps; 
        private float _currentWattsSmooth;
        private float _currentLeanAngle;
        private float _currentSteerAngle;
        private float _lastHeading;
        
        private const float AirDensity = 1.225f;
        private const float Gravity = 9.81f;

        public CycleGeometry cycleGeometry;
        public GameObject fPhysicsWheel, rPhysicsWheel;
        public WheelFrictionSettings wheelFrictionSettings;
        public AnimationCurve accelerationCurve;
        public AnimationCurve steerAngle;
        public float axisAngle;
        public AnimationCurve leanCurve;
        public float torque, topSpeed;
        [Range(0.1f, 0.9f)]
        public float relaxedSpeed;
        public float reversingSpeed;
        public Vector3 centerOfMassOffset;
        [HideInInspector]
        public bool isReversing, isAirborne, stuntMode;

        [Range(0, 8)]
        public float oscillationAmount;

        [Range(0, 1)]
        public float oscillationAffectSteerRatio;
        float oscillationSteerEffect;
        [HideInInspector]
        public float cycleOscillation;
        [HideInInspector]
        public Rigidbody rb, fWheelRb, rWheelRb;
        float xQuat, zQuat;
        [HideInInspector]
        public float crankSpeed, crankCurrentQuat, crankLastQuat, restingCrank;
        public PedalAdjustments pedalAdjustments;
        [HideInInspector]
        public float turnLeanAmount;
        RaycastHit hit;
        [HideInInspector]
        public float customSteerAxis, customLeanAxis, customAccelerationAxis, rawCustomAccelerationAxis;
        bool isRaw, sprint;
        [HideInInspector]
        public bool wheelieInput;
        [HideInInspector]
        public float wheeliePower;
        public bool wheelieToggle;
        [HideInInspector]
        public int bunnyHopInputState;
        [HideInInspector]
        public float currentTopSpeed, pickUpSpeed;
        Quaternion initialLowerForkLocalRotaion, initialHandlesRotation;
        ConfigurableJoint fPhysicsWheelConfigJoint, rPhysicsWheelConfigJoint;
        public bool groundConformity;
        RaycastHit hitGround;
        float groundZ;
        public bool inelasticCollision;
        [HideInInspector]
        public Vector3 lastVelocity, deceleration, lastDeceleration;
        int impactFrames;
        bool isBunnyHopping;
        [HideInInspector]
        public float bunnyHopAmount;
        public float bunnyHopStrength;
        public WayPointSystem wayPointSystem;
        public AirTimeSettings airTimeSettings;

        // HELPER: Returns the correct speed for the animation script
        public float GetCurrentSpeed()
        {
            if (isGhost) return ghostInputSpeed;
            if (IsOwner) 
            {
                if (useZwiftMode) return _currentVelocityMps;
                if (rb != null) return rb.linearVelocity.magnitude; 
                return 0f;
            }
            // For other players, return the calculated "Ghost Speed"
            return _calculatedGhostSpeed;
        }

        void Awake()
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.maxAngularVelocity = Mathf.Infinity;

            fWheelRb = fPhysicsWheel.GetComponent<Rigidbody>();
            fWheelRb.maxAngularVelocity = Mathf.Infinity;

            rWheelRb = rPhysicsWheel.GetComponent<Rigidbody>();
            rWheelRb.maxAngularVelocity = Mathf.Infinity;

            currentTopSpeed = topSpeed;

            initialHandlesRotation = cycleGeometry.handles.transform.localRotation;
            initialLowerForkLocalRotaion = cycleGeometry.lowerFork.transform.localRotation;

            fPhysicsWheelConfigJoint = fPhysicsWheel.GetComponent<ConfigurableJoint>();
            rPhysicsWheelConfigJoint = rPhysicsWheel.GetComponent<ConfigurableJoint>();

            if (useZwiftMode)
            {
                rb.isKinematic = true;
                fWheelRb.isKinematic = true;
                rWheelRb.isKinematic = true;
                
                if(splineController != null)
                {
                    splineController.UpdateIn = CurvyUpdateMethod.Update;
                    _currentVelocityMps = splineController.Speed;
                }
            }
            _lastPosition = transform.position;
        }

        void Update()
        {
            // --- 1. REMOTE PLAYER LOGIC ---
            if (!IsOwner)
            {
                // Calculate speed based on how far we moved since last frame
                float dist = Vector3.Distance(transform.position, _lastPosition);
                if (Time.deltaTime > 0) _calculatedGhostSpeed = dist / Time.deltaTime;
                else _calculatedGhostSpeed = 0f;

                _lastPosition = transform.position;
                
                // Manually spin visuals for the remote bike
                AnimateRemoteBike(_calculatedGhostSpeed);
                
                // IMPORTANT: RETURN here so we don't process keyboard input for other players
                return; 
            }

            // --- 2. LOCAL PLAYER LOGIC ---
            ApplyCustomInput();

            if (useZwiftMode)
            {
                HandleZwiftInput();
            }
            else
            {
                if (bunnyHopInputState == 1)
                {
                    isBunnyHopping = true;
                    bunnyHopAmount += Time.deltaTime * 8f;
                }
                if (bunnyHopInputState == -1)
                    StartCoroutine(DelayBunnyHop());

                if (bunnyHopInputState == -1 && !isAirborne)
                    rb.AddForce(transform.up * bunnyHopAmount * bunnyHopStrength, ForceMode.VelocityChange);
                else
                    bunnyHopAmount = Mathf.Lerp(bunnyHopAmount, 0, Time.deltaTime * 8f);

                bunnyHopAmount = Mathf.Clamp01(bunnyHopAmount);
            }
        }

        void HandleZwiftInput()
        {
            float targetWatts = 0f;

            // --- BLUETOOTH INJECTION (FREE WINDOWS VERSION) ---
            if (FreeWindowsBike.Instance != null && FreeWindowsBike.Instance.isConnected)
            {
                targetWatts = FreeWindowsBike.Instance.LiveWatts;
            }
            else
            {
                // Fallback: If no bike is connected, test on PC with the W/S keys
                if(Input.GetKey(KeyCode.W)) targetWatts = 250f;
                if(Input.GetKey(KeyCode.S)) targetWatts = 400f;
            }
            // --------------------------------------------------

            _currentWattsSmooth = Mathf.Lerp(_currentWattsSmooth, targetWatts, Time.deltaTime * 2.0f);
            inputWatts = _currentWattsSmooth;
        }

        void FixedUpdate()
        {
            if (!IsOwner) return;

            if (isGhost)
            {
                _currentVelocityMps = ghostInputSpeed;
                currentSpeedKph = _currentVelocityMps * 3.6f;
                if (splineController != null) splineController.Speed = _currentVelocityMps;
                return; 
            }

            if (useZwiftMode) 
            {
                ZwiftPhysicsCalculation();
            }
            else 
            {
                StandardPhysicsLoop();
            }
        }

        void LateUpdate()
        {
            // Only update Local Player visuals here. 
            // Remote players are updated in Update() using AnimateRemoteBike.
            if (IsOwner && useZwiftMode)
            {
                ZwiftVisualOverride();
            }
        }

        // --- ANIMATION FOR REMOTE PLAYERS ---
        void AnimateRemoteBike(float speed)
        {
            // Spin Wheels
            float wheelRotation = (speed / 0.35f) * Mathf.Rad2Deg * Time.deltaTime;
            cycleGeometry.fWheelVisual.transform.Rotate(wheelRotation, 0, 0);
            cycleGeometry.RWheel.transform.Rotate(wheelRotation, 0, 0);

            // Spin Pedals
            float gearRatio = 2.5f; 
            if (speed < 0.1f) 
            {
                crankSpeed = Mathf.Lerp(crankSpeed, restingCrank, Time.deltaTime * 5);
            }
            else 
            { 
                crankSpeed += (wheelRotation / gearRatio); 
                crankSpeed %= 360; 
            }

            cycleGeometry.crank.transform.localRotation = Quaternion.Euler(crankSpeed, 0, 0);
            
            // Move Pedals (IK Legs will follow this!)
            cycleGeometry.lPedal.transform.localPosition = pedalAdjustments.lPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius);
            cycleGeometry.rPedal.transform.localPosition = pedalAdjustments.rPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius);

            if (cycleGeometry.fGear != null) cycleGeometry.fGear.transform.rotation = cycleGeometry.crank.transform.rotation;
            if (cycleGeometry.rGear != null) cycleGeometry.rGear.transform.rotation = cycleGeometry.RWheel.transform.rotation;
        }

        void ZwiftPhysicsCalculation()
        {
            if (splineController == null || splineController.Spline == null) return;

            float currentGrade = 0f;
            CurvySplineSegment seg = splineController.Spline.TFToSegment(splineController.RelativePosition);
            if (seg != null)
            {
                var zwiftData = seg.GetComponent<ZwiftSegment>();
                if (zwiftData != null) currentGrade = zwiftData.gradePercentage;
            }
            currentGradePercent = currentGrade;

            float totalMass = riderMassKg + bikeMassKg;
            float gradeAngle = Mathf.Atan(currentGrade / 100f);
            float fGravity = totalMass * Gravity * Mathf.Sin(gradeAngle);
            float vSquared = _currentVelocityMps * _currentVelocityMps;
            float fDrag = 0.5f * AirDensity * CdA * vSquared;
            float fRoll = Crr * totalMass * Gravity;
            float calcVelocity = Mathf.Max(_currentVelocityMps, 1.0f); 
            float fPedal = inputWatts / calcVelocity;
            if(_currentVelocityMps < 0.1f && inputWatts > 10f) fPedal = 50f;

            float netForce = fPedal - fDrag - fRoll - fGravity;
            float acceleration = netForce / totalMass;

            _currentVelocityMps += acceleration * Time.fixedDeltaTime;
            if (_currentVelocityMps < 0f) _currentVelocityMps = 0f;

            splineController.Speed = _currentVelocityMps;
            currentSpeedKph = _currentVelocityMps * 3.6f;
        }

        void ZwiftVisualOverride()
        {
            if (splineController == null || splineController.Spline == null) return;

            Vector3 targetForward = splineController.Spline.GetTangentFast(splineController.RelativePosition);
            
            float currentHeading = Quaternion.LookRotation(targetForward, Vector3.up).eulerAngles.y;
            float deltaRot = Mathf.DeltaAngle(_lastHeading, currentHeading);
            _lastHeading = currentHeading;
            float turnRate = deltaRot / Time.deltaTime;

            Vector3 targetUp = Vector3.up;
            Vector3 rayStart = transform.position + (Vector3.up * raycastOffset);
            
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f, groundLayers);
            var validHits = hits.Where(h => !h.transform.IsChildOf(transform) && !h.collider.CompareTag("Player")).OrderBy(h => h.distance).ToArray();

            if (validHits.Length > 0)
            {
                targetUp = validHits[0].normal;
                Debug.DrawLine(rayStart, validHits[0].point, Color.green); 
            }
            else
            {
                 targetUp = splineController.Spline.GetOrientationFast(splineController.RelativePosition) * Vector3.up;
            }

            Quaternion baseRotation = Quaternion.LookRotation(targetForward, targetUp);

            float targetLean = Mathf.Clamp(turnRate * leanAggressiveness, -45f, 45f);
            _currentLeanAngle = Mathf.Lerp(_currentLeanAngle, targetLean, Time.deltaTime * leanSmoothing);
            
            Quaternion leanQuat = Quaternion.Euler(0, 0, -_currentLeanAngle + (cycleOscillation * zwiftSwayDamper));
            transform.rotation = baseRotation * leanQuat;

            float wheelRotation = (_currentVelocityMps / 0.35f) * Mathf.Rad2Deg * Time.deltaTime;
            cycleGeometry.fWheelVisual.transform.Rotate(wheelRotation, 0, 0);
            cycleGeometry.RWheel.transform.Rotate(wheelRotation, 0, 0);

            if (_currentVelocityMps > 0.5f)
            {
                pickUpSpeed = Mathf.Lerp(pickUpSpeed, 1f, Time.deltaTime);
                cycleOscillation = -Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 90)) * (oscillationAmount * 1.0f) * pickUpSpeed;
            }
            else
            {
                pickUpSpeed = 0;
                cycleOscillation = Mathf.Lerp(cycleOscillation, 0, Time.deltaTime * 5);
            }

            float targetSteer = Mathf.Clamp(_currentLeanAngle * 1.5f, -30f, 30f);
            _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetSteer, Time.deltaTime * leanSmoothing);

            cycleGeometry.handles.transform.localRotation = Quaternion.Euler(0, _currentSteerAngle, 0) * initialHandlesRotation;
            cycleGeometry.lowerFork.transform.localRotation = Quaternion.Euler(0, _currentSteerAngle, -_currentSteerAngle * axisAngle) * initialLowerForkLocalRotaion;

            float gearRatio = 2.5f; 
            if (_currentVelocityMps < 0.1f) crankSpeed = Mathf.Lerp(crankSpeed, restingCrank, Time.deltaTime * 5);
            else { crankSpeed += (wheelRotation / gearRatio); crankSpeed %= 360; }
            
            // Also run animator for local player to sync IK
            AnimateRemoteBike(_currentVelocityMps);

            customAccelerationAxis = Mathf.Clamp01(inputWatts / 500f); 
        }

        void StandardPhysicsLoop()
        {
            fPhysicsWheel.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + customSteerAxis * steerAngle.Evaluate(rb.linearVelocity.magnitude) + oscillationSteerEffect, 0);
            fPhysicsWheelConfigJoint.axis = new Vector3(1, 0, 0);
            
            float currentSpeed = rb.linearVelocity.magnitude;
            if (!sprint) currentTopSpeed = Mathf.Lerp(currentTopSpeed, topSpeed * relaxedSpeed, Time.deltaTime);
            else currentTopSpeed = Mathf.Lerp(currentTopSpeed, topSpeed, Time.deltaTime);

            if (currentSpeed < currentTopSpeed && rawCustomAccelerationAxis > 0)
                rWheelRb.AddTorque(transform.right * torque * customAccelerationAxis);

            if (currentSpeed < currentTopSpeed && rawCustomAccelerationAxis > 0 && !isAirborne && !isBunnyHopping)
                rb.AddForce(transform.forward * accelerationCurve.Evaluate(customAccelerationAxis));

            cycleGeometry.handles.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle.Evaluate(currentSpeed) + oscillationSteerEffect * 5, 0) * initialHandlesRotation;
            cycleGeometry.lowerFork.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle.Evaluate(currentSpeed) + oscillationSteerEffect * 5, customSteerAxis * -axisAngle) * initialLowerForkLocalRotaion;
        }

        void ApplyCustomInput()
        {
            CustomInput("Horizontal", ref customSteerAxis, 5, 5, false);
            CustomInput("Vertical", ref customAccelerationAxis, 1, 1, false);
            CustomInput("Horizontal", ref customLeanAxis, 1, 1, false);
            CustomInput("Vertical", ref rawCustomAccelerationAxis, 1, 1, true);
        }

        float CustomInput(string name, ref float axis, float sensitivity, float gravity, bool isRaw)
        {
            var r = Input.GetAxisRaw(name);
            var s = sensitivity;
            var g = gravity;
            var t = Time.unscaledDeltaTime;

            if (isRaw) axis = r;
            else
            {
                if (r != 0) axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
                else axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
            }
            return axis;
        }

        IEnumerator DelayBunnyHop()
        {
            yield return new WaitForSeconds(0.5f);
            isBunnyHopping = false;
            yield return null;
        }
    }
}