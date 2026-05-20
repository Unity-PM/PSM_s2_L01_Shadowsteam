using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace D
{
    /// <summary>
    /// Activates destructible objects based on triggers or collisions.
    /// Can animate its position and scale.
    /// </summary>
    [AddComponentMenu("D/D Activator")]
    public class DActivator : MonoBehaviour
    {
        /// <summary>
        /// Defines how the activator is triggered.
        /// </summary>
        public enum ActivationType
        {
            /// <summary>
            /// Activates when a collider enters the trigger.
            /// </summary>
            OnTriggerEnter = 0,

            /// <summary>
            /// Activates when a collider exits the trigger.
            /// </summary>
            OnTriggerExit = 1,

            /// <summary>
            /// Activates on collision.
            /// </summary>
            OnCollision = 2
        }

        /// <summary>
        /// Defines the type of animation for the activator.
        /// </summary>
        public enum AnimationType
        {
            /// <summary>
            /// Animates through a list of global positions.
            /// </summary>
            ByGlobalPositionList = 0,

            /// <summary>
            /// Animates along a static line.
            /// </summary>
            ByStaticLine = 1,

            /// <summary>
            /// Animates along a dynamic line.
            /// </summary>
            ByDynamicLine = 2,

            /// <summary>
            /// Animates through a list of local positions.
            /// </summary>
            ByLocalPositionList = 5
        }

        /// <summary>
        /// Defines the shape of the activator gizmo/collider.
        /// </summary>
        public enum GizmoType
        {
            /// <summary>
            /// Box shape.
            /// </summary>
            Box = 1,

            /// <summary>
            /// Sphere shape.
            /// </summary>
            Sphere = 0,

            /// <summary>
            /// Uses existing collider.
            /// </summary>
            Collider = 2,

            /// <summary>
            /// Uses particle system collisions.
            /// </summary>
            ParticleSystem = 5
        }

        /// <summary>
        /// The shape type of the activator.
        /// </summary>
        public GizmoType gizmoType;

        /// <summary>
        /// Radius for spherical activator.
        /// </summary>
        public float sphereRadius = 5f;

        /// <summary>
        /// Size for box activator.
        /// </summary>
        public Vector3 boxSize = new Vector3(5f, 2f, 5f);

        /// <summary>
        /// Check for Rigid components on activation.
        /// </summary>
        public bool checkRigid = true;

        /// <summary>
        /// Check for RigidRoot components on activation.
        /// </summary>
        public bool checkRigidRoot = true;

        /// <summary>
        /// The type of activation trigger.
        /// </summary>
        public ActivationType type;

        /// <summary>
        /// Delay before activation in seconds.
        /// </summary>
        public float delay;

        /// <summary>
        /// Demolish connected clusters upon activation.
        /// </summary>
        public bool demolishCluster;

        /// <summary>
        /// Apply force to activated objects.
        /// </summary>
        public bool apply;

        /// <summary>
        /// Velocity vector to apply to activated objects.
        /// </summary>
        public Vector3 velocity;

        /// <summary>
        /// Angular velocity (spin) to apply to activated objects.
        /// </summary>
        public Vector3 spin;

        /// <summary>
        /// Force mode for applying velocity and spin.
        /// </summary>
        public ForceMode mode;

        /// <summary>
        /// Use local coordinates for velocity application.
        /// </summary>
        public bool coord;

        /// <summary>
        /// Show animation in editor.
        /// </summary>
        public bool showAnimation;

        /// <summary>
        /// Duration of the animation sequence.
        /// </summary>
        public float duration = 3f;

        /// <summary>
        /// Target scale for animation.
        /// </summary>
        public float scaleAnimation = 1f;

        /// <summary>
        /// Type of position animation.
        /// </summary>
        public AnimationType positionAnimation;

        /// <summary>
        /// LineRenderer used for path animation.
        /// </summary>
        public LineRenderer line;

        /// <summary>
        /// List of positions for animation path.
        /// </summary>
        public List<Vector3> positionList;

        /// <summary>
        /// Toggle gizmo visibility.
        /// </summary>
        public bool showGizmo = true;

        /// <summary>
        /// The collider used for activation.
        /// </summary>
        public Collider activatorCollider;

        /// <summary>
        /// Particle system for particle-based activation.
        /// </summary>
        public ParticleSystem ps;

        /// <summary>
        /// List to store particle collision events.
        /// </summary>
        public List<ParticleCollisionEvent> collisionEvents;

        [NonSerialized] bool animating;
        [NonSerialized] float pathRatio;
        [NonSerialized] float lineLength;
        [NonSerialized] float[] checkpoints;
        [NonSerialized] float delta;
        [NonSerialized] float deltaRatioStep;
        [NonSerialized] float distDeltaStep;
        [NonSerialized] float distRatio;
        [NonSerialized] float timePassed;
        [NonSerialized] int activeSegment;
        [NonSerialized] Vector3 positionStart;
        [NonSerialized] Vector3 scaleStart;

        /// <summary>
        /// Initializes the activator, sets up colliders or particle systems.
        /// </summary>
        void Awake()
        {
            if (gizmoType != GizmoType.ParticleSystem)
                SetCollider();
            else
                SetParticleSystem();
            
            positionStart = transform.position;
            scaleStart = transform.localScale;
        }

        /// <summary>
        /// Handles collision entry events.
        /// </summary>
        /// <param name="collision">Collision data.</param>
        void OnCollisionEnter(Collision collision)
        {
            if (type == ActivationType.OnCollision && gizmoType != GizmoType.ParticleSystem)
                ActivationCheck(collision.collider);
        }

        /// <summary>
        /// Handles particle collision events.
        /// </summary>
        /// <param name="other">The GameObject struck by particles.</param>
        void OnParticleCollision(GameObject other)
        {
            if (type == ActivationType.OnCollision && gizmoType == GizmoType.ParticleSystem)
            {
                int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);
                for (int i = 0; i < numCollisionEvents; i++)
                    ActivationCheck(collisionEvents[i].colliderComponent as Collider);
            }
        }

        /// <summary>
        /// Handles trigger entry events.
        /// </summary>
        /// <param name="coll">The collider entering the trigger.</param>
        void OnTriggerEnter(Collider coll)
        {
            if (type == ActivationType.OnTriggerEnter)
                ActivationCheck(coll);
        }

        /// <summary>
        /// Handles trigger exit events.
        /// </summary>
        /// <param name="coll">The collider exiting the trigger.</param>
        void OnTriggerExit(Collider coll)
        {
            if (type == ActivationType.OnTriggerExit)
                ActivationCheck(coll);
        }

        /// <summary>
        /// Sets up the collider based on the gizmo type.
        /// </summary>
        void SetCollider()
        {
            if (gizmoType == GizmoType.Sphere)
            {
                SphereCollider col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = type != ActivationType.OnCollision;
                col.radius = sphereRadius;
                activatorCollider = col;
            }

            if (gizmoType == GizmoType.Box)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = type != ActivationType.OnCollision;
                col.size = boxSize;
                activatorCollider = col;
            }

            if (gizmoType == GizmoType.Collider)
            {
                Collider[] colliders = GetComponents<Collider>();
                if (colliders.Length == 0)
                    Debug.Log("D Activator: " + name + " has no activation collider", gameObject);
                if (type != ActivationType.OnCollision)
                    for (int i = 0; i < colliders.Length; i++)
                        colliders[i].isTrigger = type != ActivationType.OnCollision;
            }
        }

        /// <summary>
        /// Configures the particle system for collision detection.
        /// </summary>
        void SetParticleSystem()
        {
            collisionEvents = new List<ParticleCollisionEvent>();
            ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.CollisionModule cm = ps.collision;
                cm.enabled = true;
                cm.enableDynamicColliders = true;
                cm.sendCollisionMessages = true;
            }

            if (type != ActivationType.OnCollision)
            {
                type = ActivationType.OnCollision;
                Debug.Log("D Activator: " + name + " Particle System Gizmo Type supports only On Collision Activation type. Set Activation Type to On Collision.", gameObject);
            }
        }

        /// <summary>
        /// Checks if the collider triggers activation for Rigid or RigidRoot objects.
        /// </summary>
        /// <param name="coll">The collider to check.</param>
        void ActivationCheck(Collider coll)
        {
            if (checkRigid == true)
                RigidListActivationCheck(coll);
            if (checkRigidRoot == true)
                RigidRootActivationCheck(coll);
        }

        /// <summary>
        /// Checks and activates DRigid components.
        /// </summary>
        /// <param name="coll">The collider to check.</param>
        void RigidListActivationCheck(Collider coll)
        {
            DRigid rigid = coll.attachedRigidbody == null 
                ? coll.GetComponent<DRigid>() 
                : coll.attachedRigidbody.GetComponent<DRigid>();
            
            if (rigid == null)
                return;
           
            if (rigid.objectType == ObjectType.MeshRoot)
                return;

            if (rigid.activation.act == true)
                if (rigid.simulationType == SimType.Inactive || rigid.simulationType == SimType.Kinematic)
                {
                    if (delay <= 0)
                        Activate(rigid);
                    else
                        StartCoroutine(DelayedActivationCor(rigid));
                }
            
            if (rigid.objectType == ObjectType.ConnectedCluster)
                if (demolishCluster == true)
                {
                    if (delay <= 0)
                    {
                        RFDemolitionCluster.DemolishConnectedCluster(rigid, new[] { coll });
                    }
                    else
                        StartCoroutine(DelayedClusterCor(rigid, coll));
                }
        }

        /// <summary>
        /// Coroutine for delayed activation of a DRigid object.
        /// </summary>
        /// <param name="rigid">The DRigid object.</param>
        /// <returns>IEnumerator.</returns>
        IEnumerator DelayedActivationCor(DRigid rigid)
        {
            yield return new WaitForSeconds(delay);
            
            if (rigid != null)
                Activate(rigid);;
        }

        /// <summary>
        /// Coroutine for delayed cluster demolition.
        /// </summary>
        /// <param name="rigid">The DRigid object.</param>
        /// <param name="coll">The specific collider hit.</param>
        /// <returns>IEnumerator.</returns>
        IEnumerator DelayedClusterCor(DRigid rigid, Collider coll)
        {
            yield return new WaitForSeconds(delay);

            if (rigid != null && coll != null)
                RFDemolitionCluster.DemolishConnectedCluster(rigid, new[] {coll});
        }

        /// <summary>
        /// Activates a DRigid object and applies force.
        /// </summary>
        /// <param name="rigid">The DRigid object.</param>
        void Activate(DRigid rigid)
        {
            rigid.Activate();
            AddForce(rigid.physics.rigidBody);
        }

        /// <summary>
        /// Checks and activates DRigidRoot components (shards).
        /// </summary>
        /// <param name="coll">The collider to check.</param>
        void RigidRootActivationCheck(Collider coll)
        {
            if (coll.transform.parent == null)
                return;
            
            DRigidRoot rigidRoot = null;
            if (coll.transform.parent != null)
                rigidRoot = coll.transform.parent.GetComponentInParent<DRigidRoot>();
            
            if (rigidRoot == null)
                return;
                
            if (rigidRoot.activation.act == true)
                if (rigidRoot.simulationType == SimType.Inactive || rigidRoot.simulationType == SimType.Kinematic)
                {
                    if (delay <= 0)
                        ActivateCollider(rigidRoot, coll);
                    else
                        StartCoroutine(DelayedActivationCor(rigidRoot, coll));
                }
        }

        /// <summary>
        /// Coroutine for delayed activation of a shard in a DRigidRoot.
        /// </summary>
        /// <param name="rigidRoot">The DRigidRoot object.</param>
        /// <param name="coll">The collider representing the shard.</param>
        /// <returns>IEnumerator.</returns>
        IEnumerator DelayedActivationCor(DRigidRoot rigidRoot, Collider coll)
        {
            yield return new WaitForSeconds(delay);

            if (rigidRoot != null)
                ActivateCollider(rigidRoot, coll);
        }

        /// <summary>
        /// Activates a specific shard collider within a DRigidRoot.
        /// </summary>
        /// <param name="rigidRoot">The DRigidRoot object.</param>
        /// <param name="coll">The collider to activate.</param>
        void ActivateCollider(DRigidRoot rigidRoot, Collider coll)
        {
            for (int i = rigidRoot.inactiveShards.Count - 1; i >= 0; i--)
            {
                if (rigidRoot.inactiveShards[i].col == coll)
                {
                    if (RFActivation.ActivateShard(rigidRoot.inactiveShards[i], rigidRoot) == true)
                    {
                        AddForce(rigidRoot.inactiveShards[i].rb);
                        rigidRoot.inactiveShards.RemoveAt(i);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Applies configured force and torque to a Rigidbody.
        /// </summary>
        /// <param name="rb">The target Rigidbody.</param>
        void AddForce(Rigidbody rb)
        {
            if (apply == true)
            {
                if (velocity != Vector3.zero)
                {
                    if (coord == false)
                        rb.AddForce(velocity, mode);
                    else
                        rb.AddForce(transform.TransformDirection(velocity), mode);
                }

                if (spin != Vector3.zero)
                {
                    rb.AddTorque(spin, mode);
                }
            }
        }

        /// <summary>
        /// Starts the animation sequence.
        /// </summary>
        public void TriggerAnimation()
        {
            if (animating == true)
                return;

            SetAnimation();

            if (positionList.Count < 2 && scaleAnimation == 1f)
            {
                Debug.Log("Position list is empty and scale is not animated");
                return;
            }

            StartCoroutine(AnimationCor());
        }

        /// <summary>
        /// Prepares animation data based on settings.
        /// </summary>
        void SetAnimation()
        {
            if (ByLine == true)
                SetWorldPointsByLine();
            
            if (positionAnimation == AnimationType.ByLocalPositionList)
                SetWorldPointsByLocal();

            SetCheckPoints();
        }

        /// <summary>
        /// Sets world points from the LineRenderer.
        /// </summary>
        void SetWorldPointsByLine()
        {
            if (line == null)
            {
                Debug.Log("Path line is not defined");
                return;
            }

            positionList = new List<Vector3>();
            for (int i = 0; i < line.positionCount; i++)
                positionList.Add(line.transform.TransformPoint(line.GetPosition(i)));

            if (line.loop == true)
                positionList.Add(positionList[0]);
        }
        
        /// <summary>
        /// Converts local position list to world positions relative to the current transform.
        /// </summary>
        void SetWorldPointsByLocal()
        {
            if (positionList.Count < 2)
                return;

            List<Vector3> worldPoints = new List<Vector3>(){transform.position};
            for (int i = 1; i < positionList.Count; i++)
                worldPoints.Add(transform.position + positionList[i]);
            
            positionList.Clear();
            positionList = worldPoints;
        }
        
        /// <summary>
        /// Calculates progress checkpoints based on segment lengths.
        /// </summary>
        void SetCheckPoints()
        {
            if (positionList.Count < 2)
                return;

            lineLength = 0f;
            List<float> segmentsLength = new List<float>();
            if (positionList.Count >= 2)
            {
                for (int i = 0; i < positionList.Count - 1; i++)
                {
                    float length = Vector3.Distance(positionList[i], positionList[i + 1]);
                    segmentsLength.Add(length);
                    lineLength += length;
                }
            }

            float sum = 0f;
            checkpoints = new float[segmentsLength.Count + 1];
            for (int i = 0; i < segmentsLength.Count; i++)
            {
                float localRation = segmentsLength[i] / lineLength * 100f;
                checkpoints[i] = sum;
                sum += localRation;
            }

            checkpoints[segmentsLength.Count] = 100f;
        }

        /// <summary>
        /// Coroutine that handles the animation logic.
        /// </summary>
        /// <returns>IEnumerator.</returns>
        IEnumerator AnimationCor()
        {
            if (animating == true)
                yield break;

            animating = true;

            if (positionList.Count >= 2)
                transform.position = positionList[0];

            while (timePassed < duration)
            {
                if (animating == false)
                    yield break;

                if (positionAnimation == AnimationType.ByDynamicLine)
                    SetAnimation();

                delta = Time.deltaTime;
                timePassed += delta;

                if (positionList.Count >= 2)
                {
                    deltaRatioStep = delta / duration;
                    distDeltaStep = lineLength * deltaRatioStep;
                    distRatio = distDeltaStep / lineLength * 100f;
                    pathRatio += distRatio;

                    activeSegment = GetSegment(pathRatio);
                    float segmentRate = (checkpoints[activeSegment + 1] - pathRatio) / (checkpoints[activeSegment + 1] - checkpoints[activeSegment]);
                    Vector3 stepPos = Vector3.Lerp(positionList[activeSegment + 1], positionList[activeSegment], segmentRate);
                    transform.position = stepPos;
                }

                if (scaleAnimation > 1f)
                {
                    float scaleRate = timePassed / duration;
                    Vector3 maxScale = new Vector3(scaleAnimation, scaleAnimation, scaleAnimation);
                    Vector3 newScale = Vector3.Lerp(scaleStart, maxScale, scaleRate);
                    transform.localScale = newScale;
                }

                yield return null;
            }

            ResetData();
        }

        /// <summary>
        /// Gets the index of the current segment based on progress ratio.
        /// </summary>
        /// <param name="ration">Progress ratio (0-100).</param>
        /// <returns>Index of the segment.</returns>
        int GetSegment(float ration)
        {
            if (checkpoints.Length > 2)
            {
                for (int i = 0; i < checkpoints.Length - 1; i++)
                    if (ration > checkpoints[i] && ration < checkpoints[i + 1])
                        return i;
                return checkpoints.Length - 2;
            }

            return 0;
        }

        /// <summary>
        /// Resets animation data variables.
        /// </summary>
        void ResetData()
        {
            animating = false;
            pathRatio = 0f;
            lineLength = 0f;
            checkpoints = null;
            delta = 0f;
            deltaRatioStep = 0f;
            distDeltaStep = 0f;
            distRatio = 0f;
            timePassed = 0f;
            activeSegment = 0;
        }

        /// <summary>
        /// Stops the current animation.
        /// </summary>
        public void StopAnimation()
        {
            animating = false;
        }

        /// <summary>
        /// Resets the animation state and restores start position.
        /// </summary>
        public void ResetAnimation()
        {
            ResetData();
            transform.position = positionStart;
        }

        /// <summary>
        /// Adds a new position to the animation list.
        /// </summary>
        /// <param name="newPos">The position to add.</param>
        public void AddPosition(Vector3 newPos)
        {
            if (ByLine == true)
            {
                Debug.Log("Position can be saved only for Global and Local Position animation type.");
                return;
            }
            
            if (positionList == null)
                positionList = new List<Vector3>();

            if (positionList.Count > 0 && newPos == positionList[positionList.Count - 1])
            {
                Debug.Log("Activator at the same position.");
                return;
            }

            if (positionAnimation == AnimationType.ByGlobalPositionList)
            {
                if (positionList.Count == 0 || newPos != positionList[positionList.Count - 1])
                    positionList.Add(newPos);
            }
            
            if (positionAnimation == AnimationType.ByLocalPositionList)
            {
                if (positionList.Count == 0)
                    positionList.Add(newPos);
    
                else
                    positionList.Add(newPos - positionList[0]);
            }
        }

        /// <summary>
        /// Sets the gizmo type and updates the collider.
        /// </summary>
        /// <param name="gizmo">The new gizmo type.</param>
        public void SetGizmoType(GizmoType gizmo)
        {
            gizmoType = gizmo;
            
            if (Application.isPlaying == true)
            {
                if (activatorCollider != null)
                    Destroy(activatorCollider);
                
                SetCollider();
            }
        }
        
        /// <summary>
        /// Checks if animation uses a list of positions.
        /// </summary>
        public bool ByPositions
        {
            get
            {
                return positionAnimation == AnimationType.ByLocalPositionList ||
                       positionAnimation == AnimationType.ByGlobalPositionList;

            }
        }
        
        /// <summary>
        /// Checks if animation uses a line (static or dynamic).
        /// </summary>
        public bool ByLine
        {
            get
            {
                return positionAnimation == AnimationType.ByStaticLine ||
                       positionAnimation == AnimationType.ByDynamicLine;

            }
        }
    }
}
