using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EMILtools.Core;
using static EMILtools.Core.ExpressionBinder;
using UnityEngine;
using static EMILtools.Signals.ModifierExtensions;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{
    public static class ModiferRouting
    {
        public interface IStatUser
        {
            public Dictionary<Type, IStat> Stats { get; set; } 
        }
        
        /// <summary>
        /// Call in Awake to cache the add modifiers for the IStatUser
        /// </summary>
        /// <param name="statuser"></param>
        public static void CacheStats(this IStatUser user)
        {
            if (user == null) return;
            Debug.Log($"[CacheStatFields] Starting cache for IStatUser: {user.GetType().Name}");
            
            var fields = user.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(IStat).IsAssignableFrom(f.FieldType))
                .ToList();

            if (fields.Count <= 0) { Debug.Log("No IStat fields found to cache, Please declare your Stat fields in your IStatUser concrete implementation.");return;}
            user.Stats = new Dictionary<Type, IStat>(fields.Count);

            Debug.Log($"[CacheStatFields] Found {fields.Count} Stat fields: {string.Join(", ", fields.Select(f => f.Name))}");

            foreach (var field in fields)
            {
                var instance = field.GetValue(user);
                var genericArgs = field.FieldType.GetGenericArguments();
                var t = genericArgs[0];
                var tmod = genericArgs[1];
                
                user.Stats[tmod] = instance as IStat;
                Debug.Log($"[CacheStatFields] Cached stat of TMod {tmod} in user {user}");
            }
        }
    }
}
