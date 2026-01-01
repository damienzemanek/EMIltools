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
    [Serializable]
    public class ModifierRouter
    {
        // Reference casting from Action<TMod> to object (basically free)
        // Since Action is a refernce, this doesn't box
        public readonly Dictionary<Type, (object AddModifier,
                                          object AddDecorator, 
                                          object RemoveModifierSlot)> MethodCache_MutateModifiers;
        public ModifierRouter() => MethodCache_MutateModifiers = new Dictionary<Type, (object AddModifier, object AddDecorator, object RemoveModifierSlot)>();
    }
    
    public static class ModiferRouting
    {
        public static void ModifyStatUser<T, TMod>(this IStatUser recipient, ref TMod strat, params IStatModCustom<T, TMod>[] decorators)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Type modType = typeof(TMod);
            Debug.Log($"[ModifyStatUser] Recipient={recipient?.GetType().Name}, " +
                      $"Modifier={typeof(TMod).Name}, " +
                      $"Decorators=[{(decorators == null || decorators.Length == 0 ? "none" : string.Join(", ", decorators.Select(d => d?.GetType().Name ?? "null")))}]"
            );

            // This uses the TMod of the strat we sent in, so we save it using typeof(TMod) (no alloc, no box)

            if (!recipient.router.MethodCache_MutateModifiers.TryGetValue(modType, out var Methods))
            {
                Debug.Log($"Failed to retreive, and add modifier {strat.GetType().Name}");
                return;
            }
            
                
            if (Methods.AddModifier is not Action<TMod> AddModifier) return;
            AddModifier(strat);
            Debug.Log($"Added modifier {modType}");
            
            if (decorators.Length == 0) return;
            if (decorators is not IStatModCustom<T, TMod>[] decs) return;
            if (Methods.AddDecorator is not Action<List<IStatModCustom<T, TMod>>> AddDecorators) return;
            Debug.Log($"Custom Modifier being handled... : {recipient}");
            AddDecorators(decs.ToList());
        }
        
        /// <summary>
        ///             public void RemoveModifier(Func<T, T> func)
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="strat"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMod"></typeparam>
        public static void RemoveModifier<T, TMod>(this IStatUser recipient, ref TMod strat)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Type modType = typeof(TMod);

            // This uses the TMod of the strat we sent in, so we save it using typeof(TMod) (no alloc, no box)

            if (!recipient.router.MethodCache_MutateModifiers.TryGetValue(modType, out var Methods))
            {
                Debug.Log($"Failed to retreive, and remove modifier {strat.GetType().Name}");
                return;
            }
            
            if (Methods.RemoveModifierSlot is not Action<ulong> RemoveModifier) return;
            RemoveModifier(strat.hash);
            Debug.Log($"Removed modifier {modType}");
        }
        

        /// <summary>
        /// Call in Awake to cache the add modifiers for the IStatUser
        /// </summary>
        /// <param name="statuser"></param>
        public static void CacheStatFields(this IStatUser istatuser)
        {
            if (istatuser == null) return;
            
            Debug.Log($"[CacheStatFields] Starting cache for IStatUser: {istatuser.GetType().Name}");
            
            var fields = istatuser.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(IStat).IsAssignableFrom(f.FieldType))
                .ToList();

            Debug.Log($"[CacheStatFields] Found {fields.Count} Stat fields: {string.Join(", ", fields.Select(f => f.Name))}");

            foreach (var field in fields)
            {
                // Extract generic arguments  ex: <float, SpeedModifier>
                var genericArgs = field.FieldType.GetGenericArguments();
                var valueType = genericArgs[0];
                var stratType = genericArgs[1];
                var decoratorType = typeof(IStatModCustom<,>).MakeGenericType(valueType, stratType);
                var decListType = typeof(List<>).MakeGenericType(decoratorType);
                
                // Extract required methods
                var instance = field.GetValue(istatuser);
                var addModifierMethod = field.FieldType.GetMethod("AddModifier");
                var addDecoratorMethod = field.FieldType.GetMethod("AddDecorators",
                    new[] { decListType } );
                var addRemoveMethod = field.FieldType.GetMethod("RemoveModifier",
                    new [] { typeof(ulong) } );

                if(instance == null) { Debug.LogWarning($"[CacheStatFields] Skipping field {field.Name}: Instance not found."); continue; } 
                if(addModifierMethod == null) { Debug.LogWarning($"[CacheStatFields] Skipping field {field.Name}: AddModifierMethod not found."); continue; } 
                if(addDecoratorMethod == null) { Debug.LogWarning($"[CacheStatFields] Skipping field {field.Name}: AddDecoratorMethod not found."); continue; } 
                if(addRemoveMethod == null) { Debug.LogWarning($"[CacheStatFields] Skipping field {field.Name}: AddRemoveMethod not found."); continue; } 

                
                Debug.Log($"[CacheStatFields] Processing field '{field.Name}' -> ValueType: {valueType.Name}, StratType: {stratType.Name}");
                try 
                {
                    // Create delegates
                    var addModCb = Delegate.CreateDelegate(
                        typeof(Action<>).MakeGenericType(stratType),
                        instance,
                        addModifierMethod
                    );

                    Debug.Log($"[CacheStatFields] Add Modifier Cb Created");
                    var addDecCb = Delegate.CreateDelegate(
                        typeof(Action<>).MakeGenericType(decListType),
                        instance,
                        addDecoratorMethod
                    );
                    Debug.Log($"[CacheStatFields] Add Decorator Cb Created");


                    var AddRmCb = Delegate.CreateDelegate(
                        typeof(Action<ulong>),
                        instance,
                        addRemoveMethod
                    );
                    Debug.Log($"[CacheStatFields] Remove Method Cb Created");


                    // Update the Instance-specific Router (Multi-entity safety)
                    if (istatuser.router.MethodCache_MutateModifiers != null)
                    {
                        // We use the stratType (e.g. SpeedModifier) as the key
                        istatuser.router.MethodCache_MutateModifiers[stratType] = (addModCb, addDecCb, AddRmCb);
                        Debug.Log($"[CacheStatFields] Added {stratType.Name} callback to {istatuser.GetType().Name}.router.MethodCache");
                    }
                    else
                        Debug.LogError($"[CacheStatFields] Router Dictionary is NULL on {istatuser.GetType().Name}. Ensure it is initialized in the constructor or Awake.");

                }
                catch (Exception e) { Debug.LogError($"[CacheStatFields] Failed to bind field {field.Name}: {e.Message}");}
            }
            
            Debug.Log($"[CacheStatFields] Completed caching for {istatuser.GetType().Name}");
        }
    }
}
