using UnityEngine;

namespace D
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("D/D Snapshot")]
    
    public class DSnapshot: MonoBehaviour
    {
        public string assetName;
        public bool   compress;
        public Object snapshotAsset;
        public float  sizeFilter;

        // Reset
        void Reset()
        {
            assetName = gameObject.name;
        }
        
#if UNITY_EDITOR
        
        // Save asset
        public void Snapshot()
        {
            RFSnapshotAsset.Snapshot (gameObject, compress, assetName);
        }

        // Load asset
        public void Load()
        {
            RFSnapshotAsset.Load (snapshotAsset, gameObject, sizeFilter);
        }
#endif     
        
    }
}
