using System.Collections.Generic;
using UnityEngine;

namespace D
{
    // Event
    public class RFEvent
    {
        // Rigid Delegate & events
        public delegate void     EventAction(DRigid rigid);
        public event EventAction LocalEvent;
        
        
        // MeshRoot Rigid Delegate & events
        public delegate void         EventActionMeshRoot(DRigid rigid, DRigid meshRoot);
        public event EventActionMeshRoot LocalEventMeshRoot;
        
        
        // RigidRoot Delegate & events
        public delegate void         EventActionRoot(RFShard shard, DRigidRoot root);
        public event EventActionRoot LocalEventRoot;

        // Local Rigid
        public void InvokeLocalEvent(DRigid rigid)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(rigid);
        }
        
        // Local MeshRoot Rigid
        public void InvokeLocalEventMeshRoot(DRigid rigid, DRigid meshRoot)
        {
            if (LocalEventMeshRoot != null)
                LocalEventMeshRoot.Invoke(rigid, meshRoot);
        }
        
        // Local RigidRoot Shard
        public void InvokeLocalEventRoot(RFShard shard, DRigidRoot rigidRoot)
        {
            if (LocalEventRoot != null)
                LocalEventRoot.Invoke(shard, rigidRoot);
        }
    }

    /// /////////////////////////////////////////////////////////
    /// Demolition
    /// /////////////////////////////////////////////////////////
    
    public class RFDemolitionEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction GlobalEvent;

        // Demolition event
        public static void InvokeGlobalEvent (DRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke (rigid);
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Demolition event
        public static void RigidDemolitionEvent (DRigid scr)
        {
            scr.demolitionEvent.InvokeLocalEvent (scr);
            InvokeGlobalEvent (scr);
        }
    }
    
    /// /////////////////////////////////////////////////////////
    /// Activation
    /// /////////////////////////////////////////////////////////
    
    public class RFActivationEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction     GlobalEvent;
        public static event EventActionRoot GlobalEventRoot;
        
        // Activation event
        public static void InvokeGlobalEvent(DRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(rigid);
        }
        
        // Activation event
        public static void InvokeGlobalEventRoot(RFShard shard, DRigidRoot rigidRoot)
        {
            if (GlobalEventRoot != null)
                GlobalEventRoot.Invoke(shard, rigidRoot);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Rigid Activation event
        public static void RigidActivationEvent(DRigid scr)
        {
            scr.activationEvent.InvokeLocalEvent (scr);
            if (scr.meshRoot != null)
                scr.meshRoot.activationEvent.InvokeLocalEventMeshRoot (scr, scr.meshRoot);
            InvokeGlobalEvent (scr);
        }
        
        // Rigid Activation event
        public static void ShardActivationEvent(RFShard shard, DRigidRoot rigidRoot)
        {
            rigidRoot.activationEvent.InvokeLocalEventRoot (shard, rigidRoot);
            InvokeGlobalEventRoot (shard, rigidRoot);
        }
    }
    
    /// /////////////////////////////////////////////////////////
    /// Restriction
    /// /////////////////////////////////////////////////////////
    
    public class RFRestrictionEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction GlobalEvent;

        // Restriction event
        public static void InvokeGlobalEvent(DRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(rigid);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Restriction event
        public static void RestrictionEvent(DRigid rigid)
        {
            rigid.restrictionEvent.InvokeLocalEvent (rigid);
            InvokeGlobalEvent (rigid);
        }
    }
    
    /// /////////////////////////////////////////////////////////
    /// Shot
    /// /////////////////////////////////////////////////////////
    
    public class RFShotEvent
    {
        // Delegate & events
        public delegate void EventAction(DGun gun);
        public static event EventAction GlobalEvent;
        public event EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(DGun gun)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(gun);
        }
        
        // Local
        public void InvokeLocalEvent(DGun gun)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(gun);
        }
    }

    /// /////////////////////////////////////////////////////////
    /// Explosion
    /// /////////////////////////////////////////////////////////

    public class RFExplosionEvent
    {
        // Delegate & events
        public delegate void EventAction (DBomb bomb);

        public static event EventAction GlobalEvent;
        public event        EventAction LocalEvent;

        // Global
        public static void InvokeGlobalEvent (DBomb bomb)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke (bomb);
        }

        // Local
        public void InvokeLocalEvent (DBomb bomb)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke (bomb);
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Connectivity event
        public static void ExplosionEvent (DBomb scr)
        {
            scr.explosionEvent.InvokeLocalEvent (scr);
            RFExplosionEvent.InvokeGlobalEvent (scr);
        }
    }

    /// /////////////////////////////////////////////////////////
    /// Slice
    /// /////////////////////////////////////////////////////////
    
    public class RFSliceEvent
    {
        // Delegate & events
        public delegate void EventAction(DBlade blade);
        public static event EventAction GlobalEvent;
        public event EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(DBlade blade)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(blade);
        }
        
        // Local
        public void InvokeLocalEvent(DBlade blade)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(blade);
        }
    }
    
    /// /////////////////////////////////////////////////////////
    /// Connectivity
    /// /////////////////////////////////////////////////////////
    
    public class RFConnectivityEvent
    {
        // Delegate & events
        public delegate void            EventAction(DConnectivity connectivity, List<RFShard> shards, List<RFCluster> clusters);
        public static event EventAction GlobalEvent;
        public event        EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(DConnectivity connectivity, List<RFShard> shards, List<RFCluster> clusters)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(connectivity, shards, clusters);
        }
        
        // Local
        public void InvokeLocalEvent(DConnectivity connectivity, List<RFShard> shards, List<RFCluster> clusters)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(connectivity, shards, clusters);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Connectivity event
        public static void ConnectivityEvent(DConnectivity scr, List<RFShard> shards, RFCluster cluster)
        {
            if (shards.Count > 0 || cluster.HasChildClusters == true)
            {
                scr.connectivityEvent.InvokeLocalEvent (scr, shards, cluster.childClusters);
                InvokeGlobalEvent (scr, shards, cluster.childClusters);  
            }
        }
    }
    
    /// /////////////////////////////////////////////////////////
    /// Fading
    /// /////////////////////////////////////////////////////////
    
    // Fading Event
    public class RFFadingEvent
    {
        // Delegate & events
        public delegate void            EventAction(Transform tm);
        public static event EventAction GlobalEvent;
        public event        EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(Transform tm)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(tm);
        }
        
        // Local
        public void InvokeLocalEvent(Transform tm)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(tm);
        }
    }
}