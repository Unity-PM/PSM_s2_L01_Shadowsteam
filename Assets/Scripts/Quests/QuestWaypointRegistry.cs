using System.Collections.Generic;
using UnityEngine;

namespace Platformer {
    internal static class QuestWaypointRegistry {
        static readonly Dictionary<string, List<Transform>> TargetsByKey = new Dictionary<string, List<Transform>>();

        internal static string KeyForKill(string killId) => BuildKey("kill", killId);
        internal static string KeyForCollect(string collectId) => BuildKey("collect", collectId);
        internal static string KeyForBreak(string breakableId) => BuildKey("break", breakableId);
        internal static string KeyForReach(string locationId) => BuildKey("reach", locationId);
        internal static string KeyForTalk(string npcId) => BuildKey("talk", npcId);

        internal static string KeyForObjective(QuestObjective objective) {
            switch (objective) {
                case KillObjective kill:
                    return KeyForKill(kill.TargetTag);
                case CollectObjective collect:
                    return KeyForCollect(collect.ItemId);
                case BreakObjectObjective breakObject:
                    return KeyForBreak(breakObject.BreakableId);
                case ReachLocationObjective reach:
                    return KeyForReach(reach.LocationId);
                case TalkObjective talk:
                    return KeyForTalk(talk.NpcId);
                default:
                    return null;
            }
        }

        internal static void Register(string key, Transform target) {
            if (string.IsNullOrEmpty(key) || target == null)
                return;

            if (!TargetsByKey.TryGetValue(key, out List<Transform> targets)) {
                targets = new List<Transform>();
                TargetsByKey.Add(key, targets);
            }

            CleanDeadTargets(targets);
            if (!targets.Contains(target))
                targets.Add(target);
        }

        internal static void Unregister(string key, Transform target) {
            if (string.IsNullOrEmpty(key) || target == null)
                return;
            if (!TargetsByKey.TryGetValue(key, out List<Transform> targets))
                return;

            targets.RemoveAll(candidate => candidate == null || candidate == target);
            if (targets.Count == 0)
                TargetsByKey.Remove(key);
        }

        internal static bool TryGetBestTarget(QuestObjective objective, Vector3 origin, out Transform target) {
            target = null;
            string key = KeyForObjective(objective);
            if (string.IsNullOrEmpty(key))
                return false;
            if (!TargetsByKey.TryGetValue(key, out List<Transform> targets))
                return false;

            CleanDeadTargets(targets);
            if (targets.Count == 0) {
                TargetsByKey.Remove(key);
                return false;
            }

            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < targets.Count; i++) {
                Transform candidate = targets[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                    continue;

                float distance = (candidate.position - origin).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                target = candidate;
            }

            return target != null;
        }

        static string BuildKey(string prefix, string id) {
            if (string.IsNullOrEmpty(id))
                return null;

            return $"{prefix}:{id}";
        }

        static void CleanDeadTargets(List<Transform> targets) {
            targets.RemoveAll(target => target == null);
        }
    }
}
