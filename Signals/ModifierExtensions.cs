using System;
using System.Collections.Generic;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{ 
    public static class ModifierExtensions
    {
            
        public static bool RemoveModifier<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, ulong hash)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Debug.Log("Removal STARTED");

            for (int i = 0; i < modslots.Count; i++)
            {
                Debug.Log($"Checking Mod slot {i} (continuing...) ");

                if (modslots[i].modifier.GetType() != typeof(TMod)) continue;
                
                Debug.Log($"Found Mod slot of same type TMod {i}, which is {typeof(TMod)} (continuing...) ");

                
                // Edge Case Fix:
                // Naked modifiers can be explicitly targed via their funcs
                if (modslots[i].modifier.hash != hash) continue;
                
                Debug.Log("Found modifier with same hash  (continuing...)");
                
                if (modslots[i].hasDecorators)
                {
                    Debug.Log("Slot has decorators (continuing...)");

                    foreach (var dec in modslots[i].decorators)
                        dec?.OnRemove?.Invoke();
                }
                modslots.RemoveAt(i);
                return true;
            }
            return false;
        }
        
        public static bool RemoveDecoOnMod<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, Stat<T, TMod> stat, ulong hash, IStatModCustom<T, TMod> deco)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Debug.Log("Removal STARTED");

            for (int i = 0; i < modslots.Count; i++)
            {
                Debug.Log($"Checking Mod slot {i} (continuing...) ");
                if (modslots[i].modifier.GetType() != typeof(TMod)) continue;
                if (modslots[i].modifier.hash != hash) continue;
                if (modslots[i].hasDecorators) modslots[i].RemoveDecorator(deco, stat);
                return true;
            }
            return false;
        }
        
        public static List<Stat<T, TMod>.ModifierSlot> Add<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, ref TMod modifier)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Stat<T,TMod>.ModifierSlot newSlot = new Stat<T, TMod>.ModifierSlot { modifier = modifier, };
            
            modslots.Add(newSlot);
            return modslots;
        }
        
        public static List<Stat<T, TMod>.ModifierSlot> AddDecorator<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots,
            List<IStatModCustom<T, TMod>> decorators,
            Stat<T, TMod> stat
        )
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            for (int i = 0; i < modslots.Count; i++) {

                foreach (var dec in decorators)
                {
                    // ? Is this already typed for this? Do i need this check at all
                    if (modslots[i].modifier.GetType() != dec.linkType) continue; // No Match
                    
                    dec.stat = stat;
                    
                    if (modslots[i].decorators == null)
                    {
                        // Still ned to struct quick copy on the first dec tho
                        var slot = modslots[i];
                        
                        //Specify that the slot owns their own decorators, to avoid killing of the ref somewhere else
                        slot.decorators = new List<IStatModCustom<T, TMod>>(decorators);
                        modslots[i] = slot;
                    }
                
                    // Avoid struct quick copy on subsequent adds
                    // because we can just go straight to the list which is a ref
                    // previous was only 1, so it had to be re-assigned
                    modslots[i].decorators.AddGet(dec).OnAdd?.Invoke();
                }
            }
            
            return modslots;
        }
        
        public static T ApplyAll<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots, T val)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Decorator first, then modifier
            foreach (var slot in modslots) val = slot.Apply(val);
            return val;
        }
        
        public static T ApplyDecorators<T, TMod>(this List<IStatModCustom<T, TMod>> decorators, T val)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            foreach (var dec in decorators) val = dec.Apply(val);
            return val;
        }
        
        // Float only ext for Customs
        public static IStatModCustom<float, TMod> WithTimed<TMod>(this TMod mod, float duration)
            where TMod : struct, IStatModStrategy<float>
        {
            IStatModCustom<float, TMod> timedMod = new TimedModifier<float, TMod>(
                mod.func, 
                mod.hash,
                new CountdownTimer(duration));
            
            return timedMod;
        }
        
        public static IStatModCustom<T, TMod> WithTimed<T, TMod>(this TMod mod, float duration)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModCustom<T, TMod> timedMod = new TimedModifier<T, TMod>(
                mod.func,
                mod.hash,
                new CountdownTimer(duration));
            
            return timedMod;
        }
        
        
        
        // Float ext for regular
        public static void ModifyStatUser<TMod>(this IStatUser recipient, ref TMod strat)
            where TMod : struct, IStatModStrategy<float>
        {
            // Redirects to the main method with float explicitly set
            recipient.ModifyStatUser<float, TMod>(ref strat);
        }
        
        // Float ext for regular
        public static void RemoveModifier<TMod>(this IStatUser recipient, ref TMod strat)
            where TMod : struct, IStatModStrategy<float>
        {
            // Redirects to the main method with float explicitly set
            recipient.RemoveModifier<float, TMod>(ref strat);
        }
    }
}


