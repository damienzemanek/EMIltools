using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EMILtools.Core;
using static EMILtools.Core.ExpressionBinder;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{
    public static class ModiferRouting
    {
        /// <summary>
        ///  Cache to access each stat user's stat modification strategies via MethodCall from AddModifier
        /// </summary>
        private static readonly ConditionalWeakTable<IStatUser, ModifierRouter> instanceRegistry = new();

        [SerializeField]
        public class ModifierRouter
        {
            // Using a dictionary here because we are binding the Type of strategy to its AddModifier
            // Which is (DATA TO DATA) and not (INSTANCE TO DATA)
            public readonly Dictionary<Type, Action<IStatModStrategy>> AddModifierCache = new();
        }
        
        public static void ModifyStatUser(this IStatUser recipient, IStatModStrategy strategy)
        {
            if (!instanceRegistry.TryGetValue(recipient, out ModifierRouter router)) return;
            Debug.Log($"Retrieved Recicpient Router : {recipient}");
            if (!router.AddModifierCache.TryGetValue(strategy.GetType(), out var AddModifier)) return;
            Debug.Log($"Adding Modifier Strategy : {strategy.GetType()} to Recipient : {recipient}");
            AddModifier(strategy);
        }

        /// <summary>
        /// Call in Awake to cache the add modifiers for the IStatUser
        /// </summary>
        /// <param name="statuser"></param>
        public static void CacheStatFields(this IStatUser istatuser)
        {
            if(istatuser == null) return;
            
            Debug.Log("Caching stat fields on IStatUser: " + istatuser);
            
            var fields = istatuser.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(IStat).IsAssignableFrom(f.FieldType))
                .ToList();

            Debug.Log($"Retrieved fields: {string.Join(", ", fields.Select(f => f.Name))} on IStatUser: {istatuser}");

            
            ModifierRouter router = instanceRegistry.GetOrCreateValue(istatuser);

            Debug.Log("Created router for IStatUser: " + istatuser);
            
            // Loop through the Stat fields
            // Collect the Instance and Method 
            // Delegate an Action that invoked the Method with the Instance
            // Store that delagete in the router cache with the strategy TYPE as the key
            // Router cache is already stored in the instance registry
            foreach (var field in fields)
            {
                // Collect the Instance and Method
                var instance = field.GetValue(istatuser);
                var method = field.FieldType.GetMethod("AddModifier");
                if(instance == null || method == null) continue;
                Debug.Log("Retrieved instance and method for Stat Field: " + field.Name +
                          " on IStatUser: " + istatuser);
                
                Action<IStatModStrategy> cb = (strategy) =>
                {
                    Debug.Log("Attempting to invoke instance.AddModifier for Stat Field: " + field.Name +
                              " on IStatUser: " + istatuser + " with strategy: " + strategy);
                    method.Invoke(instance, new object[] { strategy });
                };
                
                
                // Using ExpressionTrees to avoid object[]{} boxing
                //Action<IStatModStrategy> cb = BindAction<IStatModStrategy>(instance, method);
                if (cb == null) continue;
                
                Debug.Log("Created callback for Stat Field: " + field.Name +
                          " on IStatUser: " + istatuser);
                
                // Get the strategy type
                Type stratType = field.FieldType.GetGenericArguments()[1];
                
                // Store that delagete in the router cache with the strategy TYPE as the key
                // dictionary[ ] the [ ] is for the key, and retrieves the instance
                router.AddModifierCache[stratType] = cb;
                
                Debug.Log($"Added Modifier Router Cache with callback {cb} for Stat Field: " +
                          $"{field.Name} with Strategy Type: {stratType} on IStatUser: {istatuser}");
            }
        }
    }
}
