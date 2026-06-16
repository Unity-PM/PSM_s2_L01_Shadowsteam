using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace D
{
    /// <summary>
    /// Component representing a bomb that explodes and affects destructible objects.
    /// </summary>
    [AddComponentMenu("D/D Bomb")]
    public class DBomb : MonoBehaviour
    {
        /// <summary>
        /// Defines the shape of the explosion range.
        /// </summary>
        public enum RangeType
        {
            /// <summary>
            /// Spherical explosion range.
            /// </summary>
            Spherical = 0
        }

        /// <summary>
        /// Defines how explosion strength fades over distance.
        /// </summary>
        public enum FadeType
        {
            /// <summary>
            /// Linear fade.
            /// </summary>
            Linear = 0,

            /// <summary>
            /// Exponential fade.
            /// </summary>
            Exponential = 1,

            /// <summary>
            /// Fade controlled by a curve.
            /// </summary>
            ByCurve = 3,

            /// <summary>
            /// No fade.
            /// </summary>
            None = 2
        }

        /// <summary>
        /// Represents an object affected by the explosion.
        /// </summary>
        [Serializable]
        public class Projectile
        {
            /// <summary>
            /// Pivot position of the projectile.
            /// </summary>
            public Vector3 positionPivot;

            /// <summary>
            /// Closest position on the projectile to the explosion center.
            /// </summary>
            public Vector3 positionClosest;

            /// <summary>
            /// Fade value (0-1) based on distance.
            /// </summary>
            public float fade;

            /// <summary>
            /// The Rigidbody component.
            /// </summary>
            public Rigidbody rb;

            /// <summary>
            /// The DRigid component, if present.
            /// </summary>
            public DRigid rigid;

            /// <summary>
            /// Rotation of the projectile.
            /// </summary>
            public Quaternion rotation;

            /// <summary>
            /// The specific shard, if part of a rigid root.
            /// </summary>
            public RFShard shard;

            /// <summary>
            /// The DRigidRoot component, if present.
            /// </summary>
            public DRigidRoot rigidRoot;
        }
        
        /// <summary>
        /// Toggle gizmo visibility.
        /// </summary>
        public bool showGizmo;

        /// <summary>
        /// Type of explosion range.
        /// </summary>
        public RangeType rangeType;

        /// <summary>
        /// Type of strength fade.
        /// </summary>
        public FadeType fadeType;

        /// <summary>
        /// Explosion range radius.
        /// </summary>
        public float range = 5f;

        /// <summary>
        /// Percentage of debris to delete after explosion.
        /// </summary>
        public int deletion;

        /// <summary>
        /// Explosion strength.
        /// </summary>
        public float strength = 1f;

        /// <summary>
        /// Variation in strength.
        /// </summary>
        public int variation = 50;

        /// <summary>
        /// Chaos factor for rotation.
        /// </summary>
        public int chaos = 30;

        /// <summary>
        /// Apply force based on mass.
        /// </summary>
        public bool forceByMass = true;

        /// <summary>
        /// Affect inactive objects.
        /// </summary>
        public bool affectInactive;

        /// <summary>
        /// Affect kinematic objects.
        /// </summary>
        public bool affectKinematic;

        /// <summary>
        /// Height offset for the explosion center.
        /// </summary>
        public float heightOffset;

        /// <summary>
        /// Delay before explosion.
        /// </summary>
        public float delay;

        /// <summary>
        /// Explode at start.
        /// </summary>
        public bool atStart;

        /// <summary>
        /// Destroy the bomb object after explosion.
        /// </summary>
        public bool destroy;

        /// <summary>
        /// Apply damage to objects.
        /// </summary>
        public bool applyDamage;

        /// <summary>
        /// Amount of damage to apply.
        /// </summary>
        public float damageValue;

        /// <summary>
        /// Play explosion sound.
        /// </summary>
        public bool play;

        /// <summary>
        /// Sound volume.
        /// </summary>
        public float volume = 1f;

        /// <summary>
        /// Explosion sound clip.
        /// </summary>
        public AudioClip clip;

        /// <summary>
        /// Layer mask for affected objects.
        /// </summary>
        public int mask = -1;

        /// <summary>
        /// Tag filter for affected objects.
        /// </summary>
        public string tagFilter = "Untagged";

        /// <summary>
        /// Curve for custom fade type.
        /// </summary>
        public AnimationCurve curve = new AnimationCurve(
            new Keyframe(0, 1, -1, 0), new Keyframe(0.5f, 1, 0, 0),
            new Keyframe(0.7f, 0, -1, 0), new Keyframe(1, 0, 0, -1));
        
        /// <summary>
        /// Event invoked upon explosion.
        /// </summary>
        public RFExplosionEvent explosionEvent = new RFExplosionEvent();
        
        [NonSerialized] Vector3 bombPosition;
        [NonSerialized] Vector3 explPosition;
        [NonSerialized] Collider[] colliders;
        [NonSerialized] List<Rigidbody> rigidbodies = new List<Rigidbody>();
        [NonSerialized] List<Projectile> projectiles = new List<Projectile>();
        [NonSerialized] List<Projectile> deletionProjectiles = new List<Projectile>();
        
        /// <summary>
        /// Initializes lists.
        /// </summary>
        void Awake()
        {
            ClearLists();
        }
        
        /// <summary>
        /// Handles auto-explosion at start.
        /// </summary>
        void Start()
        {
            if (Application.isPlaying == true)
                if (atStart == true)
                    Explode(delay);
        }
        
        /// <summary>
        /// Copies properties from another DBomb instance.
        /// </summary>
        /// <param name="scr">The source DBomb.</param>
        public void CopyFrom(DBomb scr)
        {
            rangeType = scr.rangeType;
            fadeType = scr.fadeType;
            range = scr.range;
            deletion = scr.deletion;
            strength = scr.strength;
            variation = scr.variation;
            chaos = scr.chaos;
            forceByMass = scr.forceByMass;
            affectKinematic = scr.affectKinematic;
            heightOffset = scr.heightOffset;
            delay = scr.delay;
            applyDamage = scr.applyDamage;
            damageValue = scr.damageValue;
            clip = scr.clip;
            volume = scr.volume;
        }

        /// <summary>
        /// Triggers the explosion with optional delay.
        /// </summary>
        /// <param name="delayLoc">Delay time.</param>
        public void Explode(float delayLoc)
        {
            if (delayLoc == 0)
                Explode();
            else if (delayLoc > 0)
                StartCoroutine(ExplodeCor());
        }

        /// <summary>
        /// Coroutine for delayed explosion.
        /// </summary>
        /// <returns>IEnumerator.</returns>
        IEnumerator ExplodeCor()
        {
            yield return new WaitForSeconds(delay);
            Explode();
        }

        /// <summary>
        /// Executes the explosion logic.
        /// </summary>
        void Explode()
        {
            SetPositions();

            if (Setup() == false)
                return;
            
            if (SetRigidDamage() == true)
                if (Setup() == false)
                    return;
            
            Deletion();
            Activate();
            SetForce();
            
            RFExplosionEvent.ExplosionEvent(this);
            PlayAudio();

            if (Application.isEditor == false)
                ClearLists();

            if (destroy == true)
                Destroy(gameObject, 1f);
        }

        /// <summary>
        /// Plays the explosion audio clip.
        /// </summary>
        void PlayAudio()
        {
            if (play == true && clip != null)
            {
                if (volume < 0)
                    volume = 1f;

                AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            }
        }

        /// <summary>
        /// Sets up colliders and projectiles for the explosion.
        /// </summary>
        /// <returns>True if targets were found.</returns>
        bool Setup()
        {
            ClearLists();
            SetColliders();
            SetProjectiles();
            
            if (projectiles.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Clears all temporary lists.
        /// </summary>
        void ClearLists()
        {
            colliders = null;
            rigidbodies.Clear();
            projectiles.Clear();
        }
        
        /// <summary>
        /// Restores exploded objects to their original state (not fully implemented for all cases).
        /// </summary>
        public void Restore()
        {
            RestoreProjectiles(projectiles);
            RestoreProjectiles(deletionProjectiles);
        }

        /// <summary>
        /// Restores a list of projectiles.
        /// </summary>
        /// <param name="prj">List of projectiles.</param>
        static void RestoreProjectiles(List<Projectile> prj)
        {
            for (int i = 0; i < prj.Count; i++)
                if (prj[i].rigid != null)
                    prj[i].rigid.ResetRigid();
                else if (prj[i].rb != null)
                {
                    prj[i].rb.linearVelocity = Vector3.zero;
                    prj[i].rb.angularVelocity = Vector3.zero;
                    prj[i].rb.transform.SetPositionAndRotation(prj[i].positionPivot, prj[i].rotation);
                }
        }

        /// <summary>
        /// Calculates explosion positions.
        /// </summary>
        void SetPositions()
        {
            bombPosition = transform.position;
            explPosition = transform.position;
            
            if (heightOffset != 0)
                explPosition = bombPosition + transform.TransformDirection(0f, heightOffset, 0f);
        }

        /// <summary>
        /// Finds colliders within the explosion range.
        /// </summary>
        void SetColliders()
        {
            if (rangeType == RangeType.Spherical)
                colliders = Physics.OverlapSphere(explPosition, range, mask);
        }

        /// <summary>
        /// Identifies affected rigidbodies and creates projectiles.
        /// </summary>
        void SetProjectiles()
        {
            projectiles.Clear();
            
            foreach (Collider col in colliders)
            {
                if (tagFilter != "Untagged" && col.gameObject.CompareTag(tagFilter) == false)
                    continue;
                
                Rigidbody rb = col.attachedRigidbody;

                if (rb == null)
                    continue;

                if (rigidbodies.Contains(rb) == false)
                {
                    Projectile projectile = new Projectile();
                    projectile.rb = rb;

                    projectile.positionPivot = rb.transform.position;
                    projectile.rotation = rb.transform.rotation;

                    projectile.positionClosest = col.bounds.ClosestPoint(explPosition);
  
                    projectile.fade = Fade(explPosition, projectile.positionClosest);
                    
                    if (projectile.fade <= 0)
                        continue;
                    
                    projectile.rigid = projectile.rb.GetComponent<DRigid>();

                    if (projectile.rigid == null)
                    {
                        projectile.rigidRoot = projectile.rb.GetComponentInParent<DRigidRoot>();
                        if (projectile.rigidRoot != null)
                        {
                            if (projectile.rigidRoot.collidersHash == null)
                            {
                                List<Collider> collidersTemp = new List<Collider>(projectile.rigidRoot.inactiveShards.Count);
                                for (int s = 0; s < projectile.rigidRoot.inactiveShards.Count; s++)
                                    collidersTemp.Add(projectile.rigidRoot.inactiveShards[s].col);
                                projectile.rigidRoot.collidersHash = new HashSet<Collider>(collidersTemp);
                            }
                            
                            if (projectile.rigidRoot.collidersHash.Contains(col) == true)
                            {
                                for (int i = 0; i < projectile.rigidRoot.inactiveShards.Count; i++)
                                {
                                    if (projectile.rigidRoot.inactiveShards[i].col == col)
                                    {
                                        projectile.shard = projectile.rigidRoot.inactiveShards[i];
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (affectInactive == false)
                    {
                        if (projectile.rigid != null)
                            if (projectile.rigid.simulationType == SimType.Inactive)
                                continue;
                        
                        if (projectile.shard != null)
                            if (projectile.shard.sm == SimType.Inactive)
                                continue;
                    }

                    projectiles.Add(projectile);
                    rigidbodies.Add(rb);
                }
            }
        }

        /// <summary>
        /// Applies damage to DRigid components and checks for state changes.
        /// </summary>
        /// <returns>True if state needs recollection.</returns>
        bool SetRigidDamage()
        {
            bool recollectState = false;

            if (applyDamage == true && damageValue > 0)
            {
                for (int i = 0; i < projectiles.Count; i++)
                {
                    if (projectiles[i].rigid != null && projectiles[i].rigid.damage.en == true)
                    {
                        if (projectiles[i].rigid.ApplyDamage(damageValue * projectiles[i].fade, explPosition, range) == true)
                            recollectState = true;
                    }
                }
            }

            return recollectState;
        }

        /// <summary>
        /// Handles deletion of projectiles based on deletion percentage/distance.
        /// </summary>
        void Deletion()
        {
            if (deletion > 0)
            {
                deletionProjectiles = new List<Projectile>();
                for (int i = projectiles.Count - 1; i >= 0; i--)
                    if (Vector3.Distance(projectiles[i].positionClosest, explPosition) < range * deletion / 100f)
                    {
                        deletionProjectiles.Add(projectiles[i]);
                        projectiles.RemoveAt(i);
                    }
                
                if (deletionProjectiles.Count > 0)
                    for (int i = 0; i < deletionProjectiles.Count; i++)
                    {
                        if (deletionProjectiles[i].rigid != null)
                            DMan.DestroyFragment(deletionProjectiles[i].rigid, null);
                        else
                            Destroy(deletionProjectiles[i].rb.gameObject); 
                    }
            }
        }
        
        /// <summary>
        /// Activates affected inactive or kinematic objects.
        /// </summary>
        void Activate()
        {
            if (affectInactive == false && affectKinematic == false)
                return;
            
            foreach (Projectile projectile in projectiles)
            {
                if (projectile.fade <= 0)
                    return;

                if (affectKinematic == true && projectile.rb.isKinematic == true)
                {
                    if (projectile.rigid != null)
                        projectile.rigid.Activate();

                    else if (projectile.shard != null)
                    {
                        if (projectile.shard.sm == SimType.Kinematic)
                            RFActivation.ActivateShard(projectile.shard, projectile.rigidRoot);
                    }
                    
                    else
                    {
                        projectile.rb.isKinematic = false;

                        MeshCollider meshCol = projectile.rb.gameObject.GetComponent<MeshCollider>();
                        if (meshCol != null && meshCol.convex == false)
                            meshCol.convex = true;
                    }
                    
                    continue;
                }
                
                if (affectInactive == true)
                {
                    if (projectile.rigid != null)
                    {
                        if (projectile.rigid.simulationType == SimType.Inactive)
                            projectile.rigid.Activate();
                    }
                    
                    else if (projectile.shard != null)
                    {
                        if (projectile.shard.sm == SimType.Inactive)
                            RFActivation.ActivateShard(projectile.shard, projectile.rigidRoot);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies explosion force, random variation, and torque to projectiles.
        /// </summary>
        void SetForce()
        {
            Random.InitState(1);

            ForceMode forceMode = ForceMode.Impulse;
            if (forceByMass == false)
                forceMode = ForceMode.VelocityChange;
            
            foreach (Projectile projectile in projectiles)
            {
                float strVar = strength * variation / 100f + strength;
                float str = Random.Range(strength, strVar);
                float strMult = projectile.fade * str * 10f;

                Vector3 vector = Vector(projectile);
                
                projectile.rb.AddForce(vector * strMult, forceMode);

                Vector3 rot = new Vector3(Random.Range(-chaos, chaos), Random.Range(-chaos, chaos), Random.Range(-chaos, chaos));

                projectile.rb.angularVelocity = rot;
            }
        }

        /// <summary>
        /// Calculates the fade factor based on distance and fade type.
        /// </summary>
        /// <param name="bombPos">Bomb position.</param>
        /// <param name="fragPos">Fragment position.</param>
        /// <returns>Fade factor (0-1).</returns>
        float Fade(Vector3 bombPos, Vector3 fragPos)
        {
            float fade = 1f;

            if (fadeType == FadeType.Linear)
                fade = 1f - Vector3.Distance(bombPos, fragPos) / range;

            else if (fadeType == FadeType.Exponential)
            {
                fade = 1f - Vector3.Distance(bombPos, fragPos) / range;
                fade *= fade;
            }
            
            else if (fadeType == FadeType.ByCurve)
            {
                fade = curve.Evaluate(Vector3.Distance(bombPos, fragPos) / range);;
            }

            if (fade < 0.01f)
                fade = 0;

            return fade;
        }

        /// <summary>
        /// Calculates the direction vector for the explosion force on a projectile.
        /// </summary>
        /// <param name="projectile">The target projectile.</param>
        /// <returns>Direction vector.</returns>
        Vector3 Vector(Projectile projectile)
        {
            Vector3 vector = Vector3.up;

            if (rangeType == RangeType.Spherical)
                vector = Vector3.Normalize(projectile.positionPivot - explPosition);

            return vector;
        }
    }
}
