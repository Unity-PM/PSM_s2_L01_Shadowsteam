using System;
using System.Collections.Generic;
using UnityEngine;

namespace D
{
    /// <summary>
    /// Main manager for the Deconstruction system.
    /// Handles object pooling, fragment storage, and global physics settings.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu ("D/D Man")]
    public class DMan : MonoBehaviour
    {
        /// <summary>
        /// Apply custom gravity to the physics simulation.
        /// </summary>
        public bool setGravity;

        /// <summary>
        /// Multiplier for the custom gravity.
        /// </summary>
        public float multiplier = 1f;

        /// <summary>
        /// Interpolation mode for rigidbodies.
        /// </summary>
        public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;

        /// <summary>
        /// Collision detection mode for mesh fragments.
        /// </summary>
        public CollisionDetectionMode meshCollision = CollisionDetectionMode.ContinuousDynamic;

        /// <summary>
        /// Collision detection mode for cluster fragments.
        /// </summary>
        public CollisionDetectionMode clusterCollision = CollisionDetectionMode.Discrete;

        /// <summary>
        /// Minimum mass for fragments.
        /// </summary>
        public float minimumMass = 0.1f;

        /// <summary>
        /// Maximum mass for fragments.
        /// </summary>
        public float maximumMass = 400f;

        /// <summary>
        /// Preset materials for debris and fragments.
        /// </summary>
        public RFMaterialPresets materialPresets = new RFMaterialPresets();

        /// <summary>
        /// Parent object for fragments (optional).
        /// </summary>
        public GameObject parent;

        /// <summary>
        /// Global solidity multiplier for all destructible objects.
        /// </summary>
        public float globalSolidity = 1f;

        /// <summary>
        /// Time quota for processing demolition per frame (in seconds).
        /// </summary>
        public float timeQuota = 0.033f;

        /// <summary>
        /// Advanced settings for demolition management.
        /// </summary>
        public RFManDemolition advancedDemolitionProperties = new RFManDemolition();

        /// <summary>
        /// Pooling settings for fragments.
        /// </summary>
        public RFPoolingFragment fragments = new RFPoolingFragment();

        /// <summary>
        /// Pooling settings for particles.
        /// </summary>
        public RFPoolingParticles particles = new RFPoolingParticles();

        /// <summary>
        /// Storage for managing active fragments.
        /// </summary>
        public RFStorage storage;
        
        /// <summary>
        /// Size of the collider for fragments.
        /// </summary>
        public float colliderSize = 0.05f;

        /// <summary>
        /// Limit for coplanar vertices optimization.
        /// </summary>
        public int coplanarVerts = 30;

        /// <summary>
        /// Options for cooking mesh colliders.
        /// </summary>
        public MeshColliderCookingOptions cookingOptions = (MeshColliderCookingOptions)30;

        /// <summary>
        /// Enable debug information.
        /// </summary>
        public bool debug = true;
        
        /// <summary>
        /// Cached transform component.
        /// </summary>
        [NonSerialized] public Transform transForm;

        /// <summary>
        /// Accumulated time used for processing in the current frame.
        /// </summary>
        [NonSerialized] public float maxTimeThisFrame;

        /// <summary>
        /// Singleton instance of DMan.
        /// </summary>
        public static DMan inst;

        /// <summary>
        /// Major build version.
        /// </summary>
        public static int buildMajor = 1;

        /// <summary>
        /// Minor build version.
        /// </summary>
        public static int buildMinor = 61;

        /// <summary>
        /// Static reference to collider size.
        /// </summary>
        public static float colliderSizeStatic = 0.05f;

        /// <summary>
        /// Static reference to coplanar vertex limit.
        /// </summary>
        public static int coplanarVertLimit = 30;

        /// <summary>
        /// Static reference to cooking options.
        /// </summary>
        public static MeshColliderCookingOptions cookingOptionsStatic = (MeshColliderCookingOptions)30;

        /// <summary>
        /// Static reference to debug state.
        /// </summary>
        public static bool debugStatic = true;
        
        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        void Awake()
        {
            SetInstance();
        }

        /// <summary>
        /// Resets the frame timer.
        /// </summary>
        void LateUpdate()
        {
            maxTimeThisFrame = 0f;
        }
        
        /// <summary>
        /// Sets up the singleton instance and initializes variables.
        /// </summary>
        void SetInstance()
        {
            if (inst == null)
            {
                inst = this;
            }

            if (inst != null)
            {
                if (inst == this)
                {
                    SetVariables();

                    if (Application.isPlaying == true) 
                    {
                        SetPooling();
                        SetStorage();
                    }
                }

                if (inst != this)
                {
                    if (Application.isPlaying == true)
                        Destroy (gameObject);
                    else if (Application.isEditor == true)
                        DestroyImmediate (gameObject);
                }
            }
        }

        /// <summary>
        /// Stops background processes when disabled.
        /// </summary>
        void OnDisable()
        {
            fragments.inProgress = false;
            particles.inProgress = false;
            if (storage != null)
                storage.inProgress   = false;
        }

        /// <summary>
        /// Resumes background processes when enabled.
        /// </summary>
        void OnEnable()
        {
            if (Application.isPlaying == true && gameObject.activeSelf == true)
            {
                SetPooling();
                SetStorage();
            }
        }

        /// <summary>
        /// Initializes internal variables and settings.
        /// </summary>
        void SetVariables()
        {
            transForm = GetComponent<Transform>();

            advancedDemolitionProperties.ResetCurrentAmount();

            SetGravity();

            materialPresets.SetMaterials();

            colliderSizeStatic   = colliderSize;
            cookingOptionsStatic = cookingOptions;
            debugStatic          = debug;
            coplanarVertLimit    = coplanarVerts;
        }

        /// <summary>
        /// Applies custom gravity settings to the physics engine.
        /// </summary>
        void SetGravity()
        {
            if (setGravity == true)
                Physics.gravity = -9.81f * multiplier * Vector3.up;
        }

        /// <summary>
        /// Initializes the DMan instance if it doesn't exist.
        /// </summary>
        public static void DManInit()
        {
            if (inst == null)
            {
                GameObject rfMan = new GameObject ("DMan");
                inst = rfMan.AddComponent<DMan>();
            }

            if (Application.isPlaying == false)
            {
                inst.SetInstance();
            }
        }
        
        /// <summary>
        /// Checks if the maximum amount of fragments has been reached.
        /// </summary>
        public static bool MaxAmountCheck
        {
            get
            {
                if (inst.advancedDemolitionProperties.currentAmount < inst.advancedDemolitionProperties.maximumAmount)
                    return true;

                inst.advancedDemolitionProperties.AmountWarning();
                return false;
            }
        }
        
        /// <summary>
        /// Sets up object pooling for fragments and particles.
        /// </summary>
        void SetPooling()
        {
            fragments.CreatePoolRoot (transform);

            fragments.CreateInstance (transform);

            if (Application.isPlaying == true && fragments.enable == true && fragments.inProgress == false)
                StartCoroutine (fragments.StartPoolingCor (transForm));

            particles.CreatePoolRoot (transform);

            particles.CreateInstance ();

            if (Application.isPlaying == true && particles.enable == true && particles.inProgress == false)
                StartCoroutine (particles.StartPoolingCor ());
        }
        
        /// <summary>
        /// Sets up the storage system for managing fragments.
        /// </summary>
        void SetStorage()
        {
            if (storage == null)
                storage = new RFStorage();
            
            storage.CreateStorageRoot (transform);
            
            if (Application.isPlaying == true && storage.inProgress == false)
                StartCoroutine (storage.StorageCor ());
        }

        /// <summary>
        /// Destroys all objects in storage.
        /// </summary>
        public void DestroyStorage()
        {
            storage.DestroyAll();
        }

        /// <summary>
        /// Sets the parent of a transform based on manager settings.
        /// </summary>
        /// <param name="tm">Transform to reparent.</param>
        /// <param name="original">Original transform for reference.</param>
        /// <param name="noRegister">If true, skips registering in storage.</param>
        public static void SetParentByManager (Transform tm, Transform original, bool noRegister = false)
        {
            if (inst != null && inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.Manager)
                tm.parent = inst.storage.storageRoot;
            
            else if (inst != null && inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.GlobalParent
                                  && inst.advancedDemolitionProperties.globalParent != null)
                tm.parent = inst.advancedDemolitionProperties.globalParent;
            
            else if (original == null || original.parent == null)
                tm.parent = inst.storage.storageRoot;
            
            else
                tm.parent = original.parent;

            if (noRegister == false)
                inst.storage.Register (tm);
        }
        
        /// <summary>
        /// Sets the parent of a transform based on manager settings.
        /// </summary>
        /// <param name="tm">Transform to reparent.</param>
        public static void SetParentByManager (Transform tm)
        {
            if (inst != null && inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.Manager)
                tm.parent = inst.storage.storageRoot;
            
            else if (inst != null && inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.GlobalParent
                                  && inst.advancedDemolitionProperties.globalParent != null)
                tm.parent = inst.advancedDemolitionProperties.globalParent;
            
            inst.storage.Register (tm);
        }
        
        /// <summary>
        /// Gets the appropriate parent for a rigid object.
        /// </summary>
        /// <param name="scr">The rigid component.</param>
        /// <returns>The parent transform.</returns>
        public static Transform GetParentByManager(DRigid scr)
        {
            if (inst != null && inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.Manager)
                return inst.storage.storageRoot;
            
            if (scr.clusterDemolition.cluster.mainCluster != null && scr.clusterDemolition.cluster.mainCluster.tm != null)
                return scr.clusterDemolition.cluster.mainCluster.tm.parent;
            
            return scr.transform.parent;
        }

        /// <summary>
        /// Destroys or deactivates a fragment.
        /// </summary>
        /// <param name="scr">The rigid component.</param>
        /// <param name="tm">The transform root.</param>
        /// <param name="time">Delay time.</param>
        public static void DestroyFragment (DRigid scr, Transform tm, float time = 0f)
        {
            if (Application.isPlaying == true)
                inst.advancedDemolitionProperties.currentAmount--;
            
            scr.gameObject.SetActive (false);

            if (scr.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
                DestroyOp (scr, tm, time);
        }
        
        /// <summary>
        /// Destroys a shard from a RigidRoot.
        /// </summary>
        /// <param name="scr">The RigidRoot component.</param>
        /// <param name="shard">The shard data.</param>
        public static void DestroyShard (DRigidRoot scr, RFShard shard)
        {
            shard.tm.gameObject.SetActive (false);
            
            if (scr.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
                DestroyGo (shard.tm.gameObject);
        }
        
        /// <summary>
        /// Destroys a GameObject.
        /// </summary>
        /// <param name="go">Object to destroy.</param>
        public static void DestroyGo (GameObject go)
        {
            Destroy (go);
        }

        /// <summary>
        /// Handles the destruction operation with optional delay.
        /// </summary>
        /// <param name="scr">The rigid component.</param>
        /// <param name="tm">The transform root.</param>
        /// <param name="time">Delay time.</param>
        static void DestroyOp (DRigid scr, Transform tm, float time = 0f)
        {
            if (time == 0)
                time = scr.reset.destroyDelay;

            scr.reset.toBeDestroyed = true;

            inst.fragments.DestroyOrReset (scr, time);

            if (tm != null && tm.childCount == 0)
            {
                Destroy (tm.gameObject, time);
            }
        }
    }
}
