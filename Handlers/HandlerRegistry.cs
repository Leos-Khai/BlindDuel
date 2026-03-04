using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace BlindDuel
{
    /// <summary>
    /// Auto-discovers and manages IMenuHandler implementations.
    /// Call Init() once at startup. GetHandler() finds the matching handler for a VC name.
    /// </summary>
    public static class HandlerRegistry
    {
        private static readonly List<IMenuHandler> _handlers = new();
        private static IMenuHandler _current;

        public static IMenuHandler Current => _current;

        public static void Init()
        {
            _handlers.Clear();

            var handlerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IMenuHandler).IsAssignableFrom(t));

            foreach (var type in handlerTypes)
            {
                try
                {
                    var handler = (IMenuHandler)Activator.CreateInstance(type);
                    _handlers.Add(handler);
                    Log.Write($"[Registry] Registered handler: {type.Name}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[Registry] Failed to create {type.Name}: {ex.Message}");
                }
            }

            MelonLogger.Msg($"[Registry] {_handlers.Count} menu handlers registered");
        }

        /// <summary>
        /// Finds the handler that can handle the given VC name and sets it as current.
        /// Returns the handler, or null if none match.
        /// </summary>
        public static IMenuHandler SetCurrentFromVC(string viewControllerName)
        {
            _current = _handlers.FirstOrDefault(h => h.CanHandle(viewControllerName));
            return _current;
        }

        /// <summary>
        /// Returns the handler matching a VC name without changing current.
        /// </summary>
        public static IMenuHandler GetHandler(string viewControllerName)
        {
            return _handlers.FirstOrDefault(h => h.CanHandle(viewControllerName));
        }
    }
}
