using System;
using System.Collections.Generic;
using EMILtools.Signals;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{ 
    public static class ModifierExtensions
    {
        
        // Contains modifier type
        public static bool ContainsModifierType<T, TMod>(
            this List<Stat<T, TMod>.ModifierSlot> slots)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // foreach (var slot in slots)
            //     if (slot.modifier.GetType() == typeof(TMod))
            //         return true;

            return false;
        }

        // Contains decorator instance
        public static bool ContainsDecorator<T, TMod>(
            this List<Stat<T, TMod>.ModifierSlot> slots,
            IStatModCustom<T, TMod> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            foreach (var slot in slots)
                if (ReferenceEquals(slot.decorator, decorator))
                    return true;

            return false;
        }
            
        public static bool Remove<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, Stat<T, TMod> stat, Func<T, T> func)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            for (int i = 0; i < modslots.Count; i++)
            {
                if (modslots[i].modifier.GetType() != typeof(TMod)) continue;
                
                // Edge Case Fix:
                // Naked modifiers can be explicitly targed via their funcs
                if (modslots[i].modifier.func != func) continue;
                
                if (modslots[i].decorator != null)
                {
                    // Edge Case Fix:
                    // Multiple of the same Modifier (Speed Modifier)
                    // Multiple of the Same Func (x => x * 2)
                    // One will be removable
                    if (modslots[i].decorator.removable == false) continue;

                    modslots[i].decorator.stat = stat;
                    modslots[i].decorator?.OnRemove?.Invoke();
                }
                modslots.RemoveAt(i);
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
        
        public static List<Stat<T, TMod>.ModifierSlot> Add<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots, IStatModCustom<T, TMod> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            for (int i = 0; i < modslots.Count; i++) {
                
                if (modslots[i].modifier.GetType() != decorator.linkType) continue; // No Match
                
                // Struct quick copy
                var slot = modslots[i];
                slot.decorator = decorator;
                modslots[i] = slot; 
                
                slot.decorator.OnAdd?.Invoke();
                break;
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
        
        // Float only ext for Customs
        public static IStatModCustom<float, TMod> WithTimed<TMod>(this TMod mod, float duration)
            where TMod : struct, IStatModStrategy<float>
        {
            IStatModCustom<float, TMod> timedMod = new TimedModifier<float, TMod>(
                mod.func,
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


