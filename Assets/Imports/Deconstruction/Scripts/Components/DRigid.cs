using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace D
{
    /// <summary>
    /// Core component for the Deconstruction system.
    /// Manages physical properties, demolition logic, activation, and connectivity for destructible objects.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu ("D/D Rigid")]
    public class DRigid : MonoBehaviour
    {
        /// <summary>
        /// initialization mode for the component.
        /// </summary>
        public enum InitType
        {
            /// <summary>
            /// Initialize manually via method call.
            /// </summary>
            ByMethod = 0,
            /// <summary>
            /// Initialize automatically in Awake/Start.
            /// </summary>
            AtStart  = 1
        }

        /// <summary>
        /// Initialization mode.
        /// </summary>
        public InitType initialization = InitType.ByMethod;

        /// <summary>
        /// Simulation type (Dynamic, Static, Kinematic, Sleeping).
        /// </summary>
        public SimType simulationType = SimType.Dynamic;

        /// <summary>
        /// Object type (Mesh, SkinnedMesh, Cluster, etc.).
        /// </summary>
        public ObjectType objectType = ObjectType.Mesh;

        /// <summary>
        /// Type of demolition allowed (None, Runtime, Reference).
        /// </summary>
        public DemolitionType demolitionType = DemolitionType.None;

        /// <summary>
        /// Physics properties.
        /// </summary>
        public RFPhysic physics = new RFPhysic();

        /// <summary>
        /// Activation properties.
        /// </summary>
        public RFActivation activation = new RFActivation();

        /// <summary>
        /// Limitations for demolition (depth, size, time, etc.).
        /// </summary>
        public RFLimitations limitations = new RFLimitations();

        /// <summary>
        /// Mesh demolition settings.
        /// </summary>
        public RFDemolitionMesh meshDemolition = new RFDemolitionMesh();

        /// <summary>
        /// Cluster demolition settings.
        /// </summary>
        public RFDemolitionCluster clusterDemolition = new RFDemolitionCluster();

        /// <summary>
        /// Reference demolition settings.
        /// </summary>
        public RFReferenceDemolition referenceDemolition = new RFReferenceDemolition();

        /// <summary>
        /// Material properties.
        /// </summary>
        public RFSurface materials = new RFSurface();

        /// <summary>
        /// Damage properties.
        /// </summary>
        public RFDamage damage = new RFDamage();

        /// <summary>
        /// Fading properties.
        /// </summary>
        public RFFade fading = new RFFade();

        /// <summary>
        /// Reset properties.
        /// </summary>
        public RFReset reset = new RFReset();
        
        /// <summary>
        /// Is the component initialized.
        /// </summary>
        public bool initialized;

        /// <summary>
        /// Cached RayFire meshes.
        /// </summary>
        public RFMesh[] rfMeshes;

        /// <summary>
        /// List of fragments generated from this object.
        /// </summary>
        public List<DRigid> fragments;

        /// <summary>
        /// Cached rotation. Must be public to avoid rotation errors during demolition.
        /// </summary>
        public Quaternion cacheRotation; 

        /// <summary>
        /// Cached transform.
        /// </summary>
        public Transform transForm;

        /// <summary>
        /// Root child transform.
        /// </summary>
        public Transform rootChild;

        /// <summary>
        /// Root parent transform.
        /// </summary>
        public Transform rootParent;

        /// <summary>
        /// Cached MeshFilter.
        /// </summary>
        public MeshFilter meshFilter;

        /// <summary>
        /// Cached MeshRenderer.
        /// </summary>
        public MeshRenderer meshRenderer;

        /// <summary>
        /// Cached SkinnedMeshRenderer.
        /// </summary>
        public SkinnedMeshRenderer skr;

        /// <summary>
        /// Restriction component.
        /// </summary>
        [FormerlySerializedAs ("restriction")]
        public DRestriction rest;

        /// <summary>
        /// Sound component.
        /// </summary>
        public DSound sound;
       
        [NonSerialized] public bool corState;
        [NonSerialized] public List<Transform> particleList;
        [NonSerialized] public List<DDebris> debrisList;
        [NonSerialized] public List<DDust> dustList;
        [NonSerialized] public RFDictionary[] subIds;
        [NonSerialized] public Vector3[] pivots;
        [NonSerialized] public Mesh[] meshes;
        [NonSerialized] public DRigid meshRoot;
        [NonSerialized] public DRigidRoot rigidRoot;
        [NonSerialized] public int debrisState = 1;
        [NonSerialized] public int dustState = 1;
        
        /// <summary>
        /// Event triggered on demolition.
        /// </summary>
        public RFDemolitionEvent demolitionEvent = new RFDemolitionEvent();

        /// <summary>
        /// Event triggered on activation.
        /// </summary>
        public RFActivationEvent activationEvent = new RFActivationEvent();

        /// <summary>
        /// Event triggered on restriction.
        /// </summary>
        public RFRestrictionEvent restrictionEvent = new RFRestrictionEvent();

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            MeshInput();
            
            if (initialization == InitType.AtStart)
                Initialize();
        }
        
        /// <summary>
        /// Initializes the component.
        /// </summary>
        public void Initialize()
        {
            if (gameObject.activeSelf == false)
                return;
            
            if (initialized == false)
            {
                AwakeMethods();

                RFSound.InitializationSound(sound, limitations.bboxSize);
            }
        }
        
        /// <summary>
        /// Internal initialization methods called on Awake/Start.
        /// </summary>
        void AwakeMethods()
        {
            DMan.DManInit();

            SetComponentsBasic();
            
            RFParticles.SetParticleComponents(this);
            
            if (SetupMeshRoot() == true)
                return;
            
            RFLimitations.Checks(this);
            
            SetComponentsPhysics();

            if (meshDemolition.inp == RFDemolitionMesh.MeshInputType.AtInitialization)
                MeshInput();
            
            RFDemolitionMesh.Awake(this);

            SetSkinnedMesh();

            if (physics.exclude == true)
                return;
            
            SetObjectType();

            if (Application.isPlaying == true)
            {
                StartAllCoroutines();

                initialized = true;
            }
        }

        /// <summary>
        /// Sets up SkinnedMesh specific properties.
        /// </summary>
        void SetSkinnedMesh()
        {
            if (objectType == ObjectType.SkinnedMesh)
            {
                Default();

                if (demolitionType != DemolitionType.None)
                    StartCoroutine (limitations.DemolishableCor(this));
                
                Default();

                physics.destructible = physics.Destructible;
                
                if (Application.isPlaying == true)
                    initialized = true;
            }
        }
        
        /// <summary>
        /// Called when the behaviour becomes disabled.
        /// </summary>
        void OnDisable()
        {
            corState                         = false;
            limitations.dmlCorState          = false;
            physics.physicsDataCorState      = false;
            activation.inactiveCorState      = false;
            activation.velocityCorState      = false;
            activation.offsetCorState        = false;
            fading.offsetCorState            = false;
        }

        /// <summary>
        /// Called when the behaviour becomes enabled.
        /// </summary>
        void OnEnable()
        {
            if (gameObject.activeSelf == true && initialized == true && corState == false)
            {
                StartAllCoroutines();
            }
        }

        /// <summary>
        /// Sets up the component in the Editor.
        /// </summary>
        public void EditorSetup()
        {
            if (gameObject.activeSelf == false)
                return;
            
            if (objectType == ObjectType.MeshRoot)
                EditorSetupMeshRoot();

            if (objectType == ObjectType.ConnectedCluster || objectType == ObjectType.NestedCluster)
                RFDemolitionCluster.ClusterizeEditor (this);
        }
        
        /// <summary>
        /// Resets the setup in the Editor.
        /// </summary>
        public void ResetSetup()
        {
            if (gameObject.activeSelf == false)
                return;
            
            if (objectType == ObjectType.MeshRoot)
                ResetMeshRootSetup();
            
            if (objectType == ObjectType.ConnectedCluster || objectType == ObjectType.NestedCluster)
                RFDemolitionCluster.ResetClusterize (this);
        }

        /// <summary>
        /// Sets basic components (Transform, MeshFilter, Renderer, etc.).
        /// </summary>
        public void SetComponentsBasic()
        {
            meshDemolition.sht = meshDemolition.use == true 
                ? GetComponent<DShatter>() 
                : null;
            
            transForm = GetComponent<Transform>();
            
            if (objectType == ObjectType.Mesh)
            {
                meshFilter   = GetComponent<MeshFilter>();
                meshRenderer = GetComponent<MeshRenderer>();
            }
            else if (objectType == ObjectType.SkinnedMesh)
                skr = GetComponent<SkinnedMeshRenderer>();
            
            rest = GetComponent<DRestriction>();

            if (meshFilter != null && meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (reset.action == RFReset.PostDemolitionType.DeactivateToReset)
                limitations.desc = new List<DRigid>();
        }
        
        /// <summary>
        /// Sets physics components (Rigidbody, Collider).
        /// </summary>
        public void SetComponentsPhysics()
        {
            if (physics.exclude == true)
                return;
            
            physics.rigidBody = GetComponent<Rigidbody>();
            physics.meshCollider = GetComponent<Collider>();
            
            if (objectType == ObjectType.Mesh)
                RFPhysic.SetRigidCollider (this);
            
            if (objectType == ObjectType.NestedCluster || objectType == ObjectType.ConnectedCluster) 
                RFDemolitionCluster.Clusterize (this);
            
            if (Application.isPlaying == true)
                if (simulationType != SimType.Static)
                    if (physics.rigidBody == null)
                        physics.rigidBody = gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// Sets up MeshRoot in the Editor.
        /// </summary>
        void EditorSetupMeshRoot()
        {
            bool destroyMan = DMan.inst == null;

            DMan.DManInit();
            
            ResetMeshRootSetup();
                
            SetupMeshRoot();
                
            if (destroyMan == true)
                DestroyImmediate (DMan.inst.transform.gameObject);
        }
        
        /// <summary>
        /// Sets up MeshRoot properties and children.
        /// </summary>
        /// <returns>True if MeshRoot setup was successful.</returns>
        bool SetupMeshRoot()
        {
            if (objectType == ObjectType.MeshRoot)
            {
                if (limitations.demolished == true || physics.exclude == true)
                    return true;
                
                physics.SaveInitTransform (transform);

                if (Application.isPlaying == true)
                    RFLimitations.MeshRootCheck(this);

                if (HasFragments == false)
                    AddMeshRootRigid(transform);
                
                if (Application.isPlaying == true)
                    for (int i = 0; i < fragments.Count; i++)
                    {
                        fragments[i].Initialize();
                        fragments[i].meshRoot = this;
                    }

                if (Application.isPlaying == false)
                {
                    for (int i = 0; i < fragments.Count; i++)
                    {
                        fragments[i].SetComponentsBasic();

                        RFLimitations.SetBound (fragments[i]);
                    }
                    
                    RFPhysic.SetupMeshRootColliders (this);
                }
                
                RFPhysic.SetIgnoreColliders (physics, fragments);
                
                if (Application.isPlaying == true)
                {
                    DShatter.CopyRootMeshShatter (this, fragments);
                    RFParticles.CopyRootMeshParticles (this, fragments);
                    RFSound.CopySound (sound, fragments);
                }
                
                DUnyielding.MeshRootSetup (this);

                InitConnectivity();
                
                if (Application.isPlaying == true)
                {
                    demolitionType  = DemolitionType.None;
                    physics.exclude = true;
                    initialized     = true;
                }

                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Adds Rigid component to children of MeshRoot.
        /// </summary>
        /// <param name="tm">Root transform.</param>
        void AddMeshRootRigid(Transform tm)
        {
            List<Transform> children = new List<Transform>(tm.childCount);
            for (int i = 0; i < tm.childCount; i++)
                children.Add (tm.GetChild (i));
            
            fragments = new List<DRigid>();
            for (int i = 0; i < children.Count; i++)
            {
                MeshFilter mf = children[i].GetComponent<MeshFilter>();
                if (mf != null)
                {
                    DRigid childRigid = children[i].gameObject.GetComponent<DRigid>();
                    
                    if (childRigid != null)
                        childRigid.rootParent = tm;

                    if (childRigid == null)
                    {
                        childRigid = children[i].gameObject.AddComponent<DRigid>();
                        CopyPropertiesTo (childRigid);
                    }

                    childRigid.meshFilter = mf;

                    fragments.Add (childRigid);

                    childRigid.meshRoot = this;
                }
            }
        }
        
        /// <summary>
        /// Initializes connectivity if available.
        /// </summary>
        void InitConnectivity()
        {
            activation.cnt = GetComponent<DConnectivity>();
            if (activation.cnt != null && activation.cnt.rigidRootHost == null)
            {
                activation.cnt.meshRootHost = this;
                activation.cnt.Initialize();
            }
            
            if (activation.con == true && activation.cnt == null)
                Debug.Log ("DRigid: " + name + " object has enabled Connectivity activation but has no Connectivity component.", gameObject);
        }
        
        /// <summary>
        /// Resets MeshRoot setup.
        /// </summary>
        void ResetMeshRootSetup()
        {
            if (activation.cnt != null)
                activation.cnt.ResetSetup();
            activation.cnt = null;
            
            DUnyielding.ResetMeshRootSetup (this);
            
            if (HasFragments == true)
            {
                if (physics.clusterColliders != null)
                {
                    for (int i = fragments.Count - 1; i >= 0; i--)
                        if (fragments[i] == null)
                            fragments.RemoveAt (i);

                    HashSet<Collider> collidersHash = new HashSet<Collider> (physics.clusterColliders);
                    for (int i = 0; i < fragments.Count; i++)
                        if (fragments[i].physics.meshCollider != null)
                            if (collidersHash.Contains (fragments[i].physics.meshCollider) == false)
                                DestroyImmediate (fragments[i].physics.meshCollider);
                    physics.clusterColliders = null;

                    for (int i = 0; i < fragments.Count; i++)
                        if (fragments[i].rootParent == null)
                            DestroyImmediate (fragments[i]);
                        else
                        {
                            fragments[i].rootParent           = null;
                            fragments[i].meshFilter           = null;
                            fragments[i].meshRenderer         = null;
                            fragments[i].physics.meshCollider = null;
                            fragments[i].meshRoot             = null;
                        }
                }
            }

            transForm          = null;
            physics.ignoreList = null;
            fragments          = null;
        }
        
        /// <summary>
        /// Sets properties based on object type.
        /// </summary>
        public void SetObjectType ()
        {
            if (objectType == ObjectType.Mesh ||
                objectType == ObjectType.NestedCluster ||
                objectType == ObjectType.ConnectedCluster)
            
                Default();
                
                SetPhysics();
        }
        
        /// <summary>
        /// Resets properties to default values.
        /// </summary>
        public void Default()
        {
            limitations.LocalReset();
            meshDemolition.LocalReset();
            clusterDemolition.LocalReset();
            
            limitations.birthTime = Time.time + Random.Range (0f, 0.05f);

            physics.SaveInitTransform (transForm);

            RFLimitations.SetBound(this);

            RFActivation.BackupActivationLayer (this);
        }
        
        /// <summary>
        /// Sets physics properties based on configuration.
        /// </summary>
        void SetPhysics()
        {
            if (physics.exclude == true)
                return;

            RFPhysic.SetColliderMaterial (this);

            if (HasDebris == true)
                RFPhysic.SetParticleColliderMaterial (debrisList);
            
            if (physics.rigidBody != null)
            {
                if (Application.isPlaying == true)
                    RFPhysic.SetSimulationType (physics.rigidBody, simulationType, objectType, physics.gr, physics.si, physics.st);

                if (simulationType == SimType.Static)
                    return;
                
                RFPhysic.SetColliderConvex (this);

                RFPhysic.SetDensity (this);

                RFPhysic.SetDrag (this);
            }

            physics.solidity     = physics.Solidity;
            physics.destructible = physics.Destructible;
        }

        /// <summary>
        /// Starts all runtime coroutines.
        /// </summary>
        public void StartAllCoroutines()
        {
            if (simulationType == SimType.Static)
                return;
            
            if (gameObject.activeSelf == false)
                return;
            
            if (physics.exclude == true)
                return;
            
            if (demolitionType != DemolitionType.None)
                StartCoroutine (limitations.DemolishableCor(this));
            
            if (fading.byOffset > 0)
            {
                fading.offsetEnum = RFFade.FadeOffsetCor (this);
                StartCoroutine (fading.offsetEnum);
            }

            InactiveCors();
            
            physics.physicsEnum = physics.PhysicsDataCor (this);
            StartCoroutine (physics.physicsEnum);

            corState = true;
        }

        /// <summary>
        /// Starts coroutines for inactive objects (activation checking).
        /// </summary>
        public void InactiveCors()
        {
            if (simulationType == SimType.Inactive || simulationType == SimType.Kinematic)
            {
                if (activation.vel > 0)
                {
                    activation.velocityEnum = activation.ActivationVelocityCor (this);
                    StartCoroutine (activation.velocityEnum);
                }

                if (activation.off > 0)
                {
                    activation.offsetEnum = activation.ActivationOffsetCor (this);
                    StartCoroutine (activation.offsetEnum);
                }
            }

            if (simulationType == SimType.Inactive)
                StartCoroutine (activation.InactiveCor(this));
        }
        
        /// <summary>
        /// Handles mesh input during initialization.
        /// </summary>
        public void MeshInput()
        {
            if (objectType == ObjectType.Mesh && 
                demolitionType == DemolitionType.Runtime && 
                meshDemolition.inp == RFDemolitionMesh.MeshInputType.AtStart)
            {
                SetComponentsBasic();

                RFFragment.InputMesh (this);
            }
        }
        
        /// <summary>
        /// Handles collision events.
        /// </summary>
        /// <param name="collision">Collision data.</param>
        protected virtual void OnCollisionEnter (Collision collision)
        {
            if (demolitionType == DemolitionType.None)
                return;
            
            if (limitations.CollisionCheck(this) == false)
                return;

            if (DemolitionState() == false) 
                return;

            if (limitations.tag.Length > 0 && limitations.tag != "Untagged" && collision.collider.CompareTag (limitations.tag) == false)
                return;
            
            if (CollisionDemolition (collision) == true)
                limitations.demolitionShould = true;
        }
        
        /// <summary>
        /// Checks if collision should trigger demolition.
        /// </summary>
        /// <param name="collision">Collision data.</param>
        /// <returns>True if demolition should happen.</returns>
        protected virtual bool CollisionDemolition (Collision collision)
        {
            float finalSolidity = physics.solidity * limitations.sol * DMan.inst.globalSolidity;

            if (limitations.col == true)
            {
                if (limitations.KinematicCollisionCheck(collision, finalSolidity) == true)
                    return true;

                if (limitations.ContactPointsCheck(collision, finalSolidity) == true)
                    return true;
            }

            if (damage.en == true && damage.col == true)
                if (limitations.DamagePointsCheck(collision, this) == true)
                    return true;

            return false;
        }
        
        /// <summary>
        /// Checks general state for demolition availability.
        /// </summary>
        /// <returns>True if state allows demolition.</returns>
        public bool State ()
        {
            if (limitations.demolished == true)
                return false;

            if (meshDemolition.ch.inProgress == true)
                return false;
            
            if (meshDemolition.badMesh > DMan.inst.advancedDemolitionProperties.badMeshTry)
                return false;

            if (DMan.MaxAmountCheck == false)
                return false;
            
            if (limitations.depth > 0 && limitations.currentDepth >= limitations.depth)
                return false;

            if (limitations.bboxSize < limitations.size)
                return false;

            if (Time.time - limitations.birthTime < limitations.time)
                return false;
            
            if (simulationType == SimType.Static)
                return false;
            
            if (gameObject.isStatic == true)
                return false;
            
            if (fading.state == 2)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Comprehensive check if object should be demolished.
        /// </summary>
        /// <returns>True if demolition allowed.</returns>
        public virtual bool DemolitionState ()
        {
            if (demolitionType == DemolitionType.None)
                return false;
           
            if (physics.destructible == false)
                return false;
           
            if (Visible == false)
                return false;

            if (State() == false)
                return false;
            
            if (DMan.inst.timeQuota > 0 && DMan.inst.maxTimeThisFrame > DMan.inst.timeQuota)
                return false;

            return true;
        }
        
        /// <summary>
        /// Performs the demolition of the object.
        /// </summary>
        public void Demolish()
        {
            if (initialized == false)
                Initialize();

            float t1 = Time.realtimeSinceStartup;

            if (RFReferenceDemolition.DemolishReference(this) == false)
                return;

            if (RFDemolitionMesh.DemolishMesh (this) == true)
            {
                DUnyielding.SetUnyieldingFragments (this);

                RFDemolitionMesh.ChildrenToFragments(this);
                
                RFDemolitionMesh.ClusterizeFragments (this);
            }
            else
                return;
            
            if (RFDemolitionCluster.DemolishCluster (this) == true)
                return;

            if (limitations.demolished == false)
            {
                limitations.demolitionShould = false;
                demolitionType = DemolitionType.None;
                return;
            }
            
            activation.CheckConnectivity();
            
            InitMeshFragments();
            
            DMan.inst.maxTimeThisFrame += Time.realtimeSinceStartup - t1;
            
            RFParticles.InitDemolitionParticles(this);

            RFSound.DemolitionSound(sound, limitations.bboxSize);

            RFDemolitionEvent.RigidDemolitionEvent (this);
            
            DMan.DestroyFragment (this, rootParent);
        }
        
        /// <summary>
        /// Copies properties to another DRigid component.
        /// </summary>
        /// <param name="toScr">Target component.</param>
        public void CopyPropertiesTo (DRigid toScr)
        {
            if (objectType == ObjectType.MeshRoot)
                toScr.meshRoot = this;
            else if (meshRoot != null)
                    toScr.meshRoot = meshRoot;

            toScr.objectType = objectType;
            if (objectType == ObjectType.MeshRoot || objectType == ObjectType.SkinnedMesh)
                toScr.objectType = ObjectType.Mesh;
            
            toScr.simulationType = simulationType;
            if (objectType != ObjectType.MeshRoot)
                if (simulationType == SimType.Kinematic || simulationType == SimType.Static || simulationType == SimType.Sleeping)
                    toScr.simulationType = SimType.Dynamic;

            toScr.demolitionType = demolitionType;
            if (objectType != ObjectType.MeshRoot)
                if (demolitionType != DemolitionType.None)
                    toScr.demolitionType = DemolitionType.Runtime;

            toScr.physics.CopyFrom (physics);
            toScr.activation.CopyFrom (activation);
            toScr.limitations.CopyFrom (limitations);
            toScr.meshDemolition.CopyFrom (meshDemolition);
            toScr.clusterDemolition.CopyFrom (clusterDemolition);

            if (objectType == ObjectType.MeshRoot)
                toScr.referenceDemolition.CopyFrom (referenceDemolition);
            
            toScr.materials.CopyFrom (materials);
            toScr.damage.CopyFrom (damage);
            toScr.fading.CopyFrom (fading);
            toScr.reset.CopyFrom (reset, objectType);
        }
        
        /// <summary>
        /// Initializes fragments after mesh demolition.
        /// </summary>
        public void InitMeshFragments()
        {
            if (HasFragments == false)
                return;
            
            RFPhysic.SetFragmentsVelocity (this);
            
            DMan.inst.advancedDemolitionProperties.ChangeCurrentAmount (fragments.Count);
            
            RFLimitations.SetAncestor (this);
            RFLimitations.SetDescendants (this);

            if (fading.onDemolition == true)
                fading.DemolitionFade (fragments);
        }
        
        /// <summary>
        /// Deletes cached mesh data.
        /// </summary>
        public void DeleteCache()
        {
            meshes   = null;
            pivots   = null;
            rfMeshes = null;
            subIds   = null;
        }
        
        /// <summary>
        /// Deletes all fragments.
        /// </summary>
        public void DeleteFragments()
        {
            if (rootChild != null)
            {
                if (Application.isPlaying == true)
                    Destroy (rootChild.gameObject);
                else
                    DestroyImmediate (rootChild.gameObject);

                rootChild = null;
            }

            fragments = null;
        }
        
        /// <summary>
        /// Adds a slicing plane defined by points.
        /// </summary>
        /// <param name="slicePlane">Array of points defining the plane.</param>
        public void AddSlicePlane (Vector3[] slicePlane)
        {
            if (slicePlane.Length % 2 == 1)
                return;

            if (limitations.slicePlanes == null)
                limitations.slicePlanes = new List<Vector3>();
            limitations.slicePlanes.AddRange (slicePlane);
        }
        
        /// <summary>
        /// Executes slicing operation.
        /// </summary>
        public void Slice()
        {
            if (HasSlices == false)
            {
                Debug.Log ("D Rigid: " + name + " has no defined slicing planes.", gameObject);
                return;
            }
            
            if (IsMesh == true)
            {
                if (RFDemolitionMesh.SliceMesh (this) == false)
                    return;
                
                RFDemolitionMesh.ChildrenToFragments(this);
            }
            else if (objectType == ObjectType.ConnectedCluster)
                RFDemolitionCluster.SliceConnectedCluster (this);

            RFParticles.InitDemolitionParticles(this);

            RFSound.DemolitionSound(sound, limitations.bboxSize);
            
            RFDemolitionEvent.RigidDemolitionEvent (this);

            if (IsMesh == true)
                DMan.DestroyFragment (this, rootParent);
        }
        
        /// <summary>
        /// Starts runtime caching coroutine.
        /// </summary>
        public void CacheFrames()
        {
            StartCoroutine (meshDemolition.ch.RuntimeCachingCor(this));
        }

        /// <summary>
        /// Saves initial transform data.
        /// </summary>
        [ContextMenu("SaveInitTransform")]
        public void SaveInitTransform ()
        {
            if (objectType == ObjectType.Mesh)
                physics.SaveInitTransform (transForm);
            
            else if (objectType == ObjectType.MeshRoot)
            {
                if (HasFragments == true)
                {
                    for (int i = 0; i < fragments.Count; i++)
                        if (fragments[i] != null)
                            fragments[i].physics.SaveInitTransform (fragments[i].transForm);

                    if (activation.cnt != null && reset.connectivity == true )
                        if (activation.cnt.backup != null)
                            RFBackupCluster.SaveTmRecursive (activation.cnt.backup.cluster);
                }
            }
        }
        
        /// <summary>
        /// Applies damage to the object.
        /// </summary>
        /// <param name="damageValue">Amount of damage.</param>
        /// <param name="damagePoint">Point of impact.</param>
        /// <param name="damageRadius">Radius of damage.</param>
        /// <param name="coll">Collider hit.</param>
        /// <returns>True if damage was applied.</returns>
        public bool ApplyDamage (float damageValue, Vector3 damagePoint, float damageRadius = 0f, Collider coll = null)
        {
            return RFDamage.ApplyDamage (this, damageValue, damagePoint, damageRadius, coll);
        }
        
        /// <summary>
        /// Activates the object physics.
        /// </summary>
        /// <param name="connCheck">Check connectivity.</param>
        public void Activate(bool connCheck = true)
        {
            if (objectType != ObjectType.MeshRoot)
                RFActivation.ActivateRigid (this, connCheck);
            else
                for (int i = 0; i < fragments.Count; i++)
                    RFActivation.ActivateRigid (fragments[i], connCheck);
        }
        
        /// <summary>
        /// Fades out the object.
        /// </summary>
        public void Fade()
        {
            if (objectType != ObjectType.MeshRoot)
                RFFade.FadeRigid (this);
            else
                for (int i = 0; i < fragments.Count; i++)
                    RFFade.FadeRigid (fragments[i]);
        }
        
        /// <summary>
        /// Resets the rigid object to its initial state.
        /// </summary>
        public void ResetRigid()
        {
            RFReset.ResetRigid (this);
        }

        /// <summary>
        /// Destroys a GameObject.
        /// </summary>
        /// <param name="go">Object to destroy.</param>
        public void DestroyObject(GameObject go) { Destroy (go); }

        /// <summary>
        /// Destroys a DRigid component/object.
        /// </summary>
        /// <param name="rigid">Rigid component to destroy.</param>
        public void DestroyRigid(DRigid rigid) { Destroy (rigid); }

        /// <summary>
        /// Checks if the object has fragments.
        /// </summary>
        public bool HasFragments { get { return fragments != null && fragments.Count > 0; } }

        /// <summary>
        /// Checks if the object has meshes.
        /// </summary>
        public bool HasMeshes    { get { return meshes != null && meshes.Length > 0; } }

        /// <summary>
        /// Checks if the object has RayFire meshes.
        /// </summary>
        public bool HasRfMeshes  { get { return rfMeshes != null && rfMeshes.Length > 0; } }

        /// <summary>
        /// Checks if the object has debris.
        /// </summary>
        public bool HasDebris    { get { return debrisList != null && debrisList.Count > 0; } }

        /// <summary>
        /// Checks if the object has dust.
        /// </summary>
        public bool HasDust      { get { return dustList != null && dustList.Count > 0; } }

        /// <summary>
        /// Checks if the object has slicing planes.
        /// </summary>
        bool        HasSlices    { get { return limitations.slicePlanes != null && limitations.slicePlanes.Count > 0; } }

        /// <summary>
        /// Checks if the object is a cluster.
        /// </summary>
        public bool IsCluster    { get { return objectType == ObjectType.ConnectedCluster || objectType == ObjectType.NestedCluster; } }

        /// <summary>
        /// Checks if the object is a mesh or skinned mesh.
        /// </summary>
        bool        IsMesh       { get { return objectType == ObjectType.Mesh || objectType == ObjectType.SkinnedMesh; } }
        
        /// <summary>
        /// Checks if the object is visible by any camera.
        /// </summary>
        public bool Visible
        { get {
                if (objectType == ObjectType.Mesh && meshRenderer != null) return meshRenderer.isVisible;
                if (objectType == ObjectType.SkinnedMesh && skr != null) return skr.isVisible;
                return true;
        }}

        /// <summary>
        /// Gets the integrity percentage of a cluster.
        /// </summary>
        public float AmountIntegrity
        {
            get
            {
                if (objectType == ObjectType.ConnectedCluster)
                    return  (float)clusterDemolition.cluster.shards.Count / (float)clusterDemolition.am * 100f;
                return 0f;
            }
        }
    }
}
