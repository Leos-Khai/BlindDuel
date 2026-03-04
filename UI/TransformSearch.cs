using System;
using UnityEngine;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    /// <summary>
    /// Name-based transform search utilities.
    /// Prefer these over magic child indices for resilience to UI hierarchy changes.
    /// </summary>
    public static class TransformSearch
    {
        /// <summary>
        /// Safe child access by index. Returns null (with a log) instead of throwing.
        /// Use when index-based access is unavoidable.
        /// </summary>
        public static Transform GetChild(Transform parent, int index, string context = null)
        {
            if (parent == null || index < 0 || index >= parent.childCount)
            {
                Log.Write($"[GetChild] {context ?? "unknown"}: index {index}, parent {parent?.name ?? "null"} has {parent?.childCount ?? 0} children");
                return null;
            }
            return parent.GetChild(index);
        }

        /// <summary>
        /// Find a child transform by name at any depth.
        /// More resilient than index-based access.
        /// </summary>
        public static Transform FindByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;

            // Transform.Find supports "/" paths for nested lookups
            var found = root.Find(name);
            if (found != null) return found;

            // Recursive search if path-based lookup fails
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name) return child;

                var result = FindByName(child, name);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Find the first active child with a specific component.
        /// </summary>
        public static T FindComponent<T>(Transform root) where T : Component
        {
            if (root == null) return null;
            return root.GetComponentInChildren<T>();
        }

        /// <summary>
        /// Get the position of a SelectionButton among its active siblings.
        /// Returns "N of M" string, or null if not applicable.
        /// </summary>
        public static string GetButtonPosition(SelectionButton button)
        {
            try
            {
                Transform parent = button.transform.parent;
                if (parent == null) return null;

                int index = 0;
                int total = 0;
                bool found = false;

                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (!child.gameObject.activeInHierarchy) continue;
                    if (child.GetComponent<SelectionButton>() == null) continue;

                    total++;
                    if (child.gameObject == button.gameObject)
                    {
                        index = total;
                        found = true;
                    }
                }

                if (found && total > 1)
                    return $", {index} of {total}";
            }
            catch (Exception ex) { Log.Write($"[GetButtonPosition] {ex.Message}"); }
            return null;
        }
    }
}
