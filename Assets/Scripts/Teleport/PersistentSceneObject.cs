using System.Collections.Generic;
using UnityEngine;

public class PersistentSceneObject : MonoBehaviour
{
    [SerializeField] private bool keepOnlyOneInstance;
    [SerializeField] private string uniqueId;

    private static readonly HashSet<string> ActiveIds = new HashSet<string>();
    private string runtimeId;

    private void Awake()
    {
        runtimeId = string.IsNullOrWhiteSpace(uniqueId) ? gameObject.name : uniqueId;

        if (keepOnlyOneInstance)
        {
            if (ActiveIds.Contains(runtimeId))
            {
                Destroy(gameObject);
                return;
            }

            ActiveIds.Add(runtimeId);
        }

        // DontDestroyOnLoad only works on root objects; detach if nested.
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (keepOnlyOneInstance && !string.IsNullOrEmpty(runtimeId))
            ActiveIds.Remove(runtimeId);
    }
}
