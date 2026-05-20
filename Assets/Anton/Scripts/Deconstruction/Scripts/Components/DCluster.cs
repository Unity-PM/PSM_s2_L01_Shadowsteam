using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace D
{
    /// <summary>
    /// Component for clustering objects or shards.
    /// </summary>
    [SelectionBase]
    [AddComponentMenu("D/D Cluster")]
    public class DCluster : MonoBehaviour
    {
        /// <summary>
        /// Defines the clustering method.
        /// </summary>
        public enum ClusterType
        {
            /// <summary>
            /// Cluster by point cloud (Voronoi-like).
            /// </summary>
            ByPointCloud = 0,

            /// <summary>
            /// Cluster by shared area.
            /// </summary>
            BySharedArea = 1
        }
        
        [Space(2)]
        [Header("  Properties")]
        [Space(2)]
        
        /// <summary>
        /// The clustering type.
        /// </summary>
        public ClusterType type = ClusterType.ByPointCloud;

        /// <summary>
        /// Depth of clustering hierarchy.
        /// </summary>
        [Range(1, 7)] public int depth = 1;

        /// <summary>
        /// Seed for random generation.
        /// </summary>
        [Range(0, 100)] public int seed = 1;

        /// <summary>
        /// Number of smoothing passes.
        /// </summary>
        [Range(0, 4)] public int smoothPass = 1;

        [Header("  By Point Cloud")]
        [Space(2)]
        
        /// <summary>
        /// Base amount of clusters for point cloud.
        /// </summary>
        [Range(2, 100)] public int baseAmount = 5;

        /// <summary>
        /// Amount of clusters for depth levels.
        /// </summary>
        [Range(2, 50)] public int depthAmount = 2;

        /// <summary>
        /// Connectivity type for clusters.
        /// </summary>
        public ConnectivityType connectivity = ConnectivityType.ByBoundingBox;

        [Header("  By Shared Area")]
        [Space(2)]
        
        /// <summary>
        /// Minimum shards per cluster.
        /// </summary>
        [Range(2, 8)] public int minimumAmount = 2;

        /// <summary>
        /// Maximum shards per cluster.
        /// </summary>
        [Range(2, 8)] public int maximumAmount = 5;

        /// <summary>
        /// Toggle gizmo visibility.
        /// </summary>
        [HideInInspector] public bool showGizmo = true;

        /// <summary>
        /// Toggle color preview.
        /// </summary>
        [HideInInspector] public bool colorPreview = false;

        /// <summary>
        /// Toggle scale preview.
        /// </summary>
        [HideInInspector] public bool scalePreview = false;

        /// <summary>
        /// Wireframe color for gizmos.
        /// </summary>
        [HideInInspector] public Color wireColor = new Color(0.58f, 0.77f, 1f);

        /// <summary>
        /// Scale factor for preview.
        /// </summary>
        [HideInInspector] public float previewScale = 0f;

        /// <summary>
        /// List of all generated clusters.
        /// </summary>
        [HideInInspector] public List<RFCluster> allClusters = new List<RFCluster>();

        /// <summary>
        /// List of all shards in clusters.
        /// </summary>
        [HideInInspector] public List<RFShard> allShards = new List<RFShard>();

        int clusterId = 0;
        
        /// <summary>
        /// Extracts all children under the root, flattening the hierarchy.
        /// </summary>
        public void Extract()
        {
            previewScale = 0f;
            allShards.Clear();
            allClusters.Clear();

            List<Transform> allTm = GetComponentsInChildren<Transform>().ToList();

            for (int i = allTm.Count - 1; i >= 0; i--)
            {
                Component[] allComponents = allTm[i].GetComponents(typeof(Component));

                if (allComponents.Length == 1)
                {
                    DestroyImmediate(allTm[i].gameObject);
                    allTm.RemoveAt(i);
                    continue;
                }

                allTm[i].parent = transform;
            }
        }

        /// <summary>
        /// Performs clustering on all children.
        /// </summary>
        public void Clusterize()
        {
            Extract();

            allShards.Clear();
            allClusters.Clear();

            ClusterizeVoronoi();
            ClusterizeRange();
        }

        /// <summary>
        /// Clusters shards using Voronoi method (Point Cloud).
        /// </summary>
        void ClusterizeVoronoi()
        {
            if (type == ClusterType.ByPointCloud)
            {
                RFCluster mainCluster = SetupMainCluster(connectivity);

                if (baseAmount >= mainCluster.shards.Count)
                    return;

                RFShard.SetShardNeibs(mainCluster.shards, connectivity);

                List<RFCluster> clusters = new List<RFCluster> {mainCluster};

                allClusters.Add(mainCluster);

                while (clusters.Count > 0)
                {
                    RFCluster cls = clusters[0];

                    clusters.RemoveAt(0);

                    if (cls.shards.Count < 4)
                        continue;

                    int amount = baseAmount;
                    if (cls.depth > 0)
                        amount = depthAmount;

                    cls.childClusters = ClusterizeClusterByAmount(cls, amount);

                    allClusters.AddRange(cls.childClusters);

                    if (cls.childClusters.Count > 0 && depth > cls.depth + 1)
                        clusters.AddRange(cls.childClusters);
                }

                SetClusterNames();
            }
        }

        /// <summary>
        /// Clusterizes a cluster into smaller clusters by amount.
        /// </summary>
        /// <param name="parentCluster">The parent cluster.</param>
        /// <param name="amount">Amount of child clusters.</param>
        /// <returns>List of new child clusters.</returns>
        List<RFCluster> ClusterizeClusterByAmount(RFCluster parentCluster, int amount)
        {
            List<RFCluster> childClusters = new List<RFCluster>();

            if (parentCluster.tm.childCount <= 2)
                return childClusters;

            if (amount >= parentCluster.shards.Count)
                return childClusters;

            Bounds bound = RFCluster.GetChildrenBound(parentCluster.tm);

            List<Vector3> voronoiPoints = VoronoiPointCloud(bound, amount);

            foreach (Vector3 point in voronoiPoints)
            {
                RFCluster childCluster = new RFCluster();
                childCluster.pos = point;
                childCluster.depth = parentCluster.depth + 1;

                clusterId++;
                childCluster.id = clusterId;

                childClusters.Add(childCluster);
            }

            foreach (RFShard shard in parentCluster.shards)
            {
                float rootDist = Vector3.Distance(shard.tm.position, childClusters[0].pos);
                float minDist = rootDist;
                shard.cluster = childClusters[0];

                if (childClusters.Count > 1)
                {
                    for (int i = 1; i < childClusters.Count; i++)
                    {
                        rootDist = Vector3.Distance(shard.tm.position, childClusters[i].pos);
                        if (rootDist < minDist)
                        {
                            minDist = rootDist;
                            shard.cluster = childClusters[i];
                        }
                    }
                }

                shard.cluster.shards.Add(shard);
                shard.cluster = null;
            }

            List<RFShard> soloShards = new List<RFShard>();
            for (int i = childClusters.Count - 1; i >= 0; i--)
            {
                if (childClusters[i].shards.Count < 2)
                {
                    soloShards.AddRange(childClusters[i].shards);
                    childClusters.RemoveAt(i);
                }
            }

            SetSoloShardToCluster(soloShards, childClusters);

            SetSoloShardToCluster(soloShards, childClusters);

            if (smoothPass > 0 && connectivity == ConnectivityType.ByTriangles)
                for (int i = 0; i < smoothPass; i++)
                    RoughnessPassShards(childClusters);

            if (connectivity == ConnectivityType.ByTriangles)
                ConnectivityCheck(childClusters);

            if (childClusters.Count == 1)
            {
                childClusters.Clear();
                return childClusters;
            }

            foreach (RFCluster childCluster in childClusters)
                CreateRoot(childCluster, parentCluster.tm);

            return childClusters;
        }

        /// <summary>
        /// Checks connectivity of clusters and splits disconnected parts.
        /// </summary>
        /// <param name="childClusters">List of clusters to check.</param>
        void ConnectivityCheck(List<RFCluster> childClusters)
        {
            List<RFShard> soloShards = new List<RFShard>();
            List<RFCluster> newChildClusters = new List<RFCluster>();

            foreach (RFCluster childCluster in childClusters)
            {
                for (int i = childCluster.shards.Count - 1; i >= 0; i--)
                    if (childCluster.shards[i].neibShards.Count == 0)
                        soloShards.Add(childCluster.shards[i]);

                List<RFShard> allShardsLoc = new List<RFShard>();
                foreach (RFShard shard in childCluster.shards)
                    allShardsLoc.Add(shard);

                int shardsAmount = allShardsLoc.Count;
                List<RFCluster> newClusters = new List<RFCluster>();
                while (allShardsLoc.Count > 0)
                {
                    List<RFShard> newClusterShards = new List<RFShard>();

                    List<RFShard> checkShards = new List<RFShard>();

                    checkShards.Add(allShardsLoc[0]);
                    newClusterShards.Add(allShardsLoc[0]);

                    while (checkShards.Count > 0)
                    {
                        foreach (RFShard neibShard in checkShards[0].neibShards)
                        {
                            if (allShardsLoc.Contains(neibShard) == true)
                            {
                                if (newClusterShards.Contains(neibShard) == false)
                                {
                                    checkShards.Add(neibShard);
                                    newClusterShards.Add(neibShard);
                                }
                            }
                        }

                        checkShards.RemoveAt(0);
                    }

                    if (shardsAmount == newClusterShards.Count)
                        allShardsLoc.Clear();

                    else
                    {
                        RFCluster newCluster = new RFCluster();
                        newCluster.pos = childCluster.pos;
                        newCluster.depth = childCluster.depth;
                        newCluster.shards = newClusterShards;

                        clusterId++;
                        newCluster.id = clusterId;
                        newClusters.Add(newCluster);

                        for (int i = allShardsLoc.Count - 1; i >= 0; i--)
                            if (newClusterShards.Contains(allShardsLoc[i]) == true)
                                allShardsLoc.RemoveAt(i);
                    }
                }

                if (newClusters.Count > 0)
                {
                    childCluster.shards.Clear();
                    newChildClusters.AddRange(newClusters);
                }
            }

            for (int i = childClusters.Count - 1; i >= 0; i--)
                if (childClusters[i].shards.Count == 0)
                    childClusters.RemoveAt(i);

            childClusters.AddRange(newChildClusters);

            RFCluster.SetClusterNeib(childClusters, true);

            SetSoloShardToCluster(soloShards, childClusters);

            if (smoothPass > 0)
                RoughnessPassShards(childClusters);
        }

        /// <summary>
        /// Clusters shards by shared area (range).
        /// </summary>
        void ClusterizeRange()
        {
            if (type == ClusterType.BySharedArea)
            {
                Random.InitState(seed);

                RFCluster mainCluster = SetupMainCluster(ConnectivityType.ByTriangles);
                allClusters.Add(mainCluster);

                RFShard.SetShardNeibs(mainCluster.shards, ConnectivityType.ByTriangles);

                List<RFCluster> childClusters = ClusterizeRangeShards(mainCluster);

                foreach (RFCluster childCluster in childClusters)
                    CreateRoot(childCluster, transform);

                allClusters.AddRange(childClusters);

                if (depth > 1)
                {
                    for (int i = 1; i < depth; i++)
                    {
                        RFCluster.SetClusterNeib(mainCluster.childClusters, true);

                        List<RFCluster> newClusters = ClusterizeRangeClusters(mainCluster);

                        if (newClusters.Count > 1)
                        {
                            foreach (RFCluster cls in newClusters)
                            {
                                CreateRoot(cls, mainCluster.tm);
                                foreach (RFCluster childCLuster in cls.childClusters)
                                    childCLuster.tm.parent = cls.tm;
                            }

                            mainCluster.childClusters = newClusters;

                            allClusters.AddRange(newClusters);

                            foreach (RFCluster cls in allClusters)
                                if (cls.id != 0)
                                    cls.depth += 1;
                        }
                    }
                }

                SetClusterNames();
            }
        }

        /// <summary>
        /// Clusterizes shards based on shared area.
        /// </summary>
        /// <param name="mainCluster">The main cluster.</param>
        /// <returns>List of child clusters.</returns>
        List<RFCluster> ClusterizeRangeShards(RFCluster mainCluster)
        {
            List<RFShard> soloShards = new List<RFShard>();

            List<RFCluster> childClusters = new List<RFCluster>();

            mainCluster.shards.Sort();

            while (mainCluster.shards.Count > 0)
            {
                int shardsAmount = Random.Range(minimumAmount, maximumAmount);

                RFShard startShard = mainCluster.shards[0];

                mainCluster.shards.RemoveAt(0);

                List<RFShard> shardGroup = new List<RFShard>();
                shardGroup.Add(startShard);

                for (int s = 0; s < shardsAmount - 1; s++)
                {
                    RFShard biggestShard = GetNeibShardArea(shardGroup, mainCluster.shards);

                    if (biggestShard == null)
                        break;

                    shardGroup.Add(biggestShard);

                    mainCluster.shards.RemoveAll(t => t.id == biggestShard.id);
                }

                if (shardGroup.Count == 1)
                    soloShards.Add(startShard);

                else if (shardGroup.Count > 1)
                {
                    RFCluster childCluster = new RFCluster();
                    childCluster.shards.AddRange(shardGroup);
                    childCluster.depth = 1;

                    clusterId++;
                    childCluster.id = clusterId;

                    childClusters.Add(childCluster);
                    mainCluster.childClusters.Add(childCluster);
                }
            }

            SetSoloShardToCluster(soloShards, childClusters);

            SetSoloShardToCluster(soloShards, childClusters);

            if (smoothPass > 0)
                for (int i = 0; i < smoothPass; i++)
                    RoughnessPassShards(childClusters);

            int startId = 1;
            for (int i = 0; i < childClusters.Count; i++)
                childClusters[i].id = startId + i;

            mainCluster.shards.Clear();
            mainCluster.shards.AddRange(soloShards);

            return childClusters;
        }

        /// <summary>
        /// Clusterizes existing clusters into larger clusters.
        /// </summary>
        /// <param name="parentCluster">The parent cluster.</param>
        /// <returns>List of new cluster groups.</returns>
        List<RFCluster> ClusterizeRangeClusters(RFCluster parentCluster)
        {
            List<RFCluster> soloClusters = new List<RFCluster>();

            List<RFCluster> newClusters = new List<RFCluster>();

            parentCluster.childClusters.Sort();

            while (parentCluster.childClusters.Count > 0)
            {
                int clustersAmount = Random.Range(minimumAmount, maximumAmount);

                RFCluster startCluster = parentCluster.childClusters[0];

                parentCluster.childClusters.RemoveAt(0);

                List<RFCluster> clusterGroup = new List<RFCluster>();
                clusterGroup.Add(startCluster);
                for (int s = 0; s < clustersAmount - 1; s++)
                {
                    RFCluster biggestCluster = RFCluster.GetNeibClusterArea(clusterGroup, parentCluster.childClusters);

                    if (biggestCluster == null)
                        break;

                    clusterGroup.Add(biggestCluster);

                    parentCluster.childClusters.RemoveAll(t => t.id == biggestCluster.id);
                }

                if (clusterGroup.Count == 1)
                    soloClusters.Add(startCluster);

                else
                {
                    RFCluster newCluster = new RFCluster();
                    newCluster.childClusters.AddRange(clusterGroup);

                    newCluster.depth = 0;

                    clusterId++;
                    newCluster.id = clusterId;

                    newClusters.Add(newCluster);
                }
            }

            SetSoloClusterToCluster(soloClusters, newClusters);

            SetSoloClusterToCluster(soloClusters, newClusters);

            if (smoothPass > 0)
                for (int i = 0; i < smoothPass; i++)
                    RoughnessPassClusters(newClusters);

            return newClusters;
        }

        /// <summary>
        /// Smooths clustering by reassigning shards to neighbors if better fit.
        /// </summary>
        /// <param name="clusters">List of clusters.</param>
        static void RoughnessPassShards(List<RFCluster> clusters)
        {
            RFCluster.SetClusterNeib(clusters, true);

            for (int s = clusters.Count - 1; s >= 0; s--)
            {
                RFCluster cluster = clusters[s];

                if (cluster.shards.Count == 2)
                    continue;

                if (cluster.neibClusters.Count == 0)
                    continue;

                List<RFShard> excludeShards = new List<RFShard>();
                List<RFCluster> attachToClusters = new List<RFCluster>();

                foreach (RFShard shard in cluster.shards)
                {
                    float areaInCluster = 0f;
                    for (int i = 0; i < shard.neibShards.Count; i++)
                        if (cluster.shards.Contains(shard.neibShards[i]) == true)
                            areaInCluster += shard.nArea[i];

                    List<float> neibAreaList = new List<float>();
                    foreach (RFCluster neibCluster in cluster.neibClusters)
                    {
                        float areaInNeibCluster = 0f;
                        for (int i = 0; i < shard.neibShards.Count; i++)
                            if (neibCluster.shards.Contains(shard.neibShards[i]) == true)
                                areaInNeibCluster += shard.nArea[i];
                        neibAreaList.Add(areaInNeibCluster);
                    }

                    float maxArea = neibAreaList.Max();

                    if (areaInCluster >= maxArea)
                        continue;

                    for (int i = 0; i < neibAreaList.Count; i++)
                    {
                        if (maxArea == neibAreaList[i])
                        {
                            excludeShards.Add(shard);
                            attachToClusters.Add(cluster.neibClusters[i]);
                        }
                    }
                }

                if (excludeShards.Count > 0)
                {
                    for (int i = 0; i < excludeShards.Count; i++)
                    {
                        for (int c = cluster.shards.Count - 1; c >= 0; c--)
                            if (cluster.shards[c] == excludeShards[i])
                                cluster.shards.RemoveAt(c);

                        attachToClusters[i].shards.Add(excludeShards[i]);
                    }
                }
            }

            for (int i = clusters.Count - 1; i >= 0; i--)
            {
                if (clusters[i].shards.Count == 1)
                {
                    clusters[i].shards.Clear();
                }

                if (clusters[i].shards.Count == 0)
                    clusters.RemoveAt(i);
            }
        }

        /// <summary>
        /// Smooths clustering by reassigning child clusters to neighbors if better fit.
        /// </summary>
        /// <param name="clusters">List of clusters.</param>
        void RoughnessPassClusters(List<RFCluster> clusters)
        {
            RFCluster.SetClusterNeib(clusters, true);

            foreach (RFCluster bigCluster in clusters)
            {
                if (bigCluster.childClusters.Count <= 2)
                    continue;

                if (bigCluster.neibClusters.Count == 0)
                    continue;

                List<RFCluster> excludeClusters = new List<RFCluster>();
                List<RFCluster> attachToClusters = new List<RFCluster>();

                foreach (RFCluster childCluster in bigCluster.childClusters)
                {
                    float areaInCluster = 0f;
                    for (int i = 0; i < childCluster.neibClusters.Count; i++)
                        if (bigCluster.childClusters.Contains(childCluster.neibClusters[i]) == true)
                            areaInCluster += childCluster.neibArea[i];

                    List<float> neibAreaList = new List<float>();
                    foreach (RFCluster bigNeibCluster in bigCluster.neibClusters)
                    {
                        float areaInNeibCluster = 0f;
                        for (int i = 0; i < childCluster.neibClusters.Count; i++)
                            if (bigNeibCluster.childClusters.Contains(childCluster.neibClusters[i]) == true)
                                areaInNeibCluster += childCluster.neibArea[i];
                        neibAreaList.Add(areaInNeibCluster);
                    }

                    float maxArea = neibAreaList.Max();

                    if (areaInCluster >= maxArea)
                        continue;

                    for (int i = 0; i < neibAreaList.Count; i++)
                    {
                        if (maxArea == neibAreaList[i])
                        {
                            excludeClusters.Add(childCluster);
                            attachToClusters.Add(bigCluster.neibClusters[i]);
                        }
                    }
                }

                if (excludeClusters.Count + 1 >= bigCluster.childClusters.Count)
                    continue;

                if (excludeClusters.Count > 0)
                {
                    for (int i = 0; i < excludeClusters.Count; i++)
                    {
                        for (int s = bigCluster.shards.Count - 1; s >= 0; s--)
                            if (bigCluster.childClusters[s] == excludeClusters[i])
                                bigCluster.childClusters.RemoveAt(s);

                        attachToClusters[i].childClusters.Add(excludeClusters[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Reassigns solo shards to the closest cluster.
        /// </summary>
        /// <param name="soloShards">List of solo shards.</param>
        /// <param name="childClusters">List of clusters.</param>
        void SetSoloShardToCluster(List<RFShard> soloShards, List<RFCluster> childClusters)
        {
            if (soloShards.Count == 0)
                return;

            for (int i = soloShards.Count - 1; i >= 0; i--)
            {
                int ind = GetNeibIndArea(soloShards[i]);
                if (ind >= 0)
                {
                    RFShard neibShard = soloShards[i].neibShards[ind];
                    for (int c = 0; c < childClusters.Count; c++)
                    {
                        if (childClusters[c].shards.Contains(neibShard) == true)
                        {
                            childClusters[c].shards.Add(soloShards[i]);
                            soloShards.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the index of the neighbor shard with the largest shared area.
        /// </summary>
        /// <param name="shard">The shard to check.</param>
        /// <param name="shardList">Optional filter list.</param>
        /// <returns>Index of the neighbor.</returns>
        int GetNeibIndArea(RFShard shard, List<RFShard> shardList = null)
        {
            float biggestArea = 0f;
            int neibInd = 0;
            for (int i = 0; i < shard.neibShards.Count; i++)
            {
                if (shardList != null)
                    if (shardList.Contains(shard.neibShards[i]) == false)
                        continue;

                if (shard.nArea[i] > biggestArea)
                {
                    biggestArea = shard.nArea[i];
                    neibInd = i;
                }
            }

            if (biggestArea > 0)
                return neibInd;

            return -1;
        }

        /// <summary>
        /// Reassigns solo clusters to the closest cluster group.
        /// </summary>
        /// <param name="soloClusters">List of solo clusters.</param>
        /// <param name="childClusters">List of parent cluster groups.</param>
        void SetSoloClusterToCluster(List<RFCluster> soloClusters, List<RFCluster> childClusters)
        {
            if (soloClusters.Count == 0)
                return;

            for (int i = soloClusters.Count - 1; i >= 0; i--)
            {
                int ind = soloClusters[i].GetNeibIndArea();
                if (ind >= 0)
                {
                    RFCluster neibCluster = soloClusters[i].neibClusters[ind];
                    for (int c = 0; c < childClusters.Count; c++)
                    {
                        if (childClusters[c].childClusters.Contains(neibCluster) == true)
                        {
                            childClusters[c].childClusters.Add(soloClusters[i]);
                            soloClusters.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets up the main root cluster.
        /// </summary>
        /// <param name="connect">Connectivity type.</param>
        /// <returns>The main cluster.</returns>
        RFCluster SetupMainCluster(ConnectivityType connect)
        {
            RFCluster cluster = new RFCluster();

            cluster.tm = transform;
            cluster.depth = 0;
            cluster.pos = transform.position;

            cluster.id = 0;

            RFShard.SetShards(cluster, connectivity);
            
            clusterId = 0;

            allShards.Clear();
            allShards.AddRange(cluster.shards);

            return cluster;
        }

        /// <summary>
        /// Naming convention for clusters.
        /// </summary>
        void SetClusterNames()
        {
            foreach (RFCluster cls in allClusters)
                if (cls.id > 0)
                    if (cls.tm != null)
                        cls.tm.name = gameObject.name + "_cls_" + cls.id;
        }

        /// <summary>
        /// Creates a root GameObject for a cluster.
        /// </summary>
        /// <param name="childCluster">The child cluster.</param>
        /// <param name="parentTm">Parent transform.</param>
        void CreateRoot(RFCluster childCluster, Transform parentTm)
        {
            Bounds childBound = GetShardsBound(childCluster.shards, childCluster.childClusters);

            childCluster.bound = childBound;

            GameObject childRoot = new GameObject();

            childCluster.tm = childRoot.transform;
            childCluster.pos = childBound.center;
            childCluster.tm.position = childBound.center;

            childCluster.tm.parent = parentTm;

            foreach (RFShard shard in childCluster.shards)
                shard.tm.parent = childCluster.tm;
        }

        /// <summary>
        /// Calculates bounds for a list of shards and sub-clusters.
        /// </summary>
        /// <param name="shards">List of shards.</param>
        /// <param name="clusters">List of sub-clusters.</param>
        /// <returns>Combined Bounds.</returns>
        Bounds GetShardsBound(List<RFShard> shards, List<RFCluster> clusters = null)
        {
            List<Bounds> bounds = new List<Bounds>();

            foreach (RFShard shard in shards)
                bounds.Add(shard.bnd);

            if (clusters != null)
                foreach (RFCluster cluster in clusters)
                    bounds.Add(cluster.bound);

            return RFCluster.GetBoundsBound(bounds.ToArray());
        }

        /// <summary>
        /// Gets neighbor shard with the largest shared area.
        /// </summary>
        /// <param name="shardGroup">Group of shards.</param>
        /// <param name="shardList">Candidate shards.</param>
        /// <returns>Neighbor shard.</returns>
        static RFShard GetNeibShardArea(List<RFShard> shardGroup, List<RFShard> shardList)
        {
            if (shardList.Count == 0)
                return null;

            List<RFShard> allNeibs = new List<RFShard>();

            float biggestArea = 0f;
            RFShard biggestShard = null;

            foreach (RFShard shard in shardGroup)
            {
                for (int i = 0; i < shard.neibShards.Count; i++)
                {
                    if (biggestArea >= shard.nArea[i])
                        continue;

                    if (allNeibs.Contains(shard.neibShards[i]) == true)
                        continue;

                    if (shardList.Contains(shard.neibShards[i]) == false)
                        continue;

                    allNeibs.Add(shard.neibShards[i]);
                    biggestArea = shard.nArea[i];
                    biggestShard = shard.neibShards[i];
                }
            }
            allNeibs = null;

            return biggestShard;
        }
        
        /// <summary>
        /// Generates a random point cloud within bounds.
        /// </summary>
        /// <param name="bound">Bounds.</param>
        /// <param name="am">Amount of points.</param>
        /// <returns>List of points.</returns>
        List<Vector3> VoronoiPointCloud(Bounds bound, int am)
        {
            Random.InitState(seed + clusterId);
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < am; i++)
            {
                float randomX = Random.Range(bound.min.x, bound.max.x);
                float randomY = Random.Range(bound.min.y, bound.max.y);
                float randomZ = Random.Range(bound.min.z, bound.max.z);
                Vector3 randomPoint = new Vector3(randomX, randomY, randomZ);
                points.Add(randomPoint);
            }

            return points;
        }
    }
}
