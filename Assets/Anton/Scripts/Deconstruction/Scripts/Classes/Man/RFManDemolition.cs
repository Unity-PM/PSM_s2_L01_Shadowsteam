using System;
using UnityEngine;

namespace D
{
    [Serializable]
    public class RFManDemolition
    {
        public enum FragmentParentType
        {
            Manager      = 0,
            LocalParent  = 1,
            GlobalParent = 2
            
        }

        // UI
        public FragmentParentType parent;
        public Transform          globalParent;
        public int                maximumAmount;
        public int                badMeshTry;
        public float              sizeThreshold;
        public int                currentAmount;

        // Non Serialized
        [NonSerialized] bool amountWaring;
        // TODO Inherit velocity by impact normal

        public RFManDemolition()
        {
            parent        = FragmentParentType.Manager;
            maximumAmount = 1000;
            badMeshTry    = 3;
            sizeThreshold = 0.05f;
            currentAmount = 0;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Change current amount value
        public void ChangeCurrentAmount (int am)
        {
            // Add/subtract
            currentAmount += am;

            // One time Warning to avoid Debug spam in game build
            if (currentAmount >= maximumAmount)
                AmountWarning();
        }

        public void AmountWarning()
        {
            if (amountWaring == false)
                Debug.Log ("D Man: Maximum fragments amount reached. Increase Maximum Amount property in D Man / Advanced Properties.");
            amountWaring = true;
            
        }

        public void ResetCurrentAmount()
        {
            currentAmount = 0;
        }
    }
}