using System;
using System.Collections.Generic;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{ 
    public static class ModifierExtensions
    {
        public interface IStat { }

        
        //-----------------------------------------------------------------------------------
        //                 Modify               [User] -> [Stat]
        //------------------------------------------------------------------------------------
        // Float specific overload
        public static (TMod, IStatUser) Modify<TMod>(this IStatUser user, TMod mod)
            where TMod : struct, IStatModStrategy<float>
        => user.Modify<float, TMod>(mod);
        
        // Generic base Modify
        public static (TMod, IStatUser) Modify<T, TMod>(this IStatUser user, TMod mod)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Stat<T, TMod> stat = (user.Stats[typeof(TMod)] as Stat<T, TMod>);
            stat.AddModifier(mod);
            return (mod, user);
        }
        
        
        //-----------------------------------------------------------------------------------
        //                 Remove Modifier      [User] -> [Stat]
        //------------------------------------------------------------------------------------
        // float override
        public static (TMod, IStatUser) RemoveModifier<TMod>(this IStatUser user, TMod mod)
            where TMod : struct, IStatModStrategy<float>
        => user.RemoveModifier<float, TMod>(mod);
        
        // Generic base Modify
        public static (TMod, IStatUser) RemoveModifier<T, TMod>(this IStatUser user, TMod mod)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Stat<T, TMod> stat = (user.Stats[typeof(TMod)] as Stat<T, TMod>);
            stat.RemoveModifier(mod.hash);
            return (mod, user);
        }
        
        
                
        //-----------------------------------------------------------------------------------
        //                 Decorator Add/Remove   [User] -> [Stat]
        //------------------------------------------------------------------------------------
        // Generic base Modify
        public static void AddDecorator<T, TMod>(this IStatUser user, IStatModDecorator<T, TMod> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Stat<T, TMod> stat = (user.Stats[typeof(TMod)] as Stat<T, TMod>);
            stat.AddDecorator(decorator);
        }
        
        public static void RemoveDecorator<T, TMod>(this IStatUser user,  TMod mod, IStatModDecorator<T, TMod> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Stat<T, TMod> stat = (user.Stats[typeof(TMod)] as Stat<T, TMod>);
            stat.RemoveDecorator(mod.hash, decorator);
        }
        
        

        
        
        
        
        //-----------------------------------------------------------------------------------
        //                          Modifier Slot Remove  
        //------------------------------------------------------------------------------------
        public static bool RemoveModifierSlot<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, ulong hash)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Debug.Log("Removal STARTED");

            for (int i = 0; i < modslots.Count; i++)
            {
                if (modslots[i].modifier.GetType() != typeof(TMod)) { continue; } // must be same TMod
                if (modslots[i].modifier.hash != hash)              { continue; } // must be same hash 
                
                if (modslots[i].hasDecorators)
                    foreach (var dec in modslots[i].decorators)
                        dec?.OnRemove?.Invoke();
                
                modslots.RemoveAt(i); 
                Debug.Log("Sucessfully Removed Modifier Slot");
                return true;
                
            }
            return false;
        }
        
        //-----------------------------------------------------------------------------------
        //                          Decorator Add/Remove 
        //------------------------------------------------------------------------------------
        public static bool RemoveDecoOnMod<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, Stat<T, TMod> stat, ulong hash, IStatModDecorator<T, TMod> deco)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            Debug.Log("Removal STARTED");

            for (int i = 0; i < modslots.Count; i++)
            {
                Debug.Log($"Checking Mod slot {i} (continuing...) ");
                if (modslots[i].modifier.GetType() != typeof(TMod)) continue; // Has to be same TMod
                if (modslots[i].modifier.hash != hash) continue;              // Has to be same hash (Can be mult modifiers on the same Modifier Name)
                if (modslots[i].hasDecorators) 
                    return modslots[i].RemoveDecorator(deco, stat); // If it even has decs
            }
            return false;
        }
        
        public static List<Stat<T, TMod>.ModifierSlot> AddDecorator<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots,
            IStatModDecorator<T, TMod> decorator,
            Stat<T, TMod> stat
        )
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            bool added = false;
            for (int i = 0; i < modslots.Count; i++) {

                // ? Is this already typed for this? Do i need this check at all
                if (modslots[i].modifier.GetType() != decorator.linkType) continue; // No Match
                Debug.Log($"[AddDecorator] Matched type");
                
                decorator.stat = stat;
                    
                if (modslots[i].decorators == null)
                {
                    // Still ned to struct quick copy on the first dec tho
                    var slot = modslots[i];
                        
                    //Specify that the slot owns their own decorators, to avoid killing of the ref somewhere else
                    slot.decorators = new List<IStatModDecorator<T, TMod>>();
                    modslots[i] = slot;
                    Debug.Log($"[AddDecorator] Lazy Initialized new list of decorators for slot {i}");
                }
                
                // Avoid struct quick copy on subsequent adds
                // because we can just go straight to the list which is a ref
                // previous was only 1, so it had to be re-assigned
                modslots[i].decorators.AddGet(decorator).OnAdd?.Invoke();
                Debug.Log($"[AddDecorator] Added new decorator to slot {i}");
                added = true;
            }
            if(added) Debug.Log($"[AddDecorator] Added Decorator Success");
            else Debug.Log($"[AddDecorator] Added Decorator FAILED [!] ");
            return modslots;
        }
        
        
        //-----------------------------------------------------------------------------------
        //            Modifier Applying Funcs / Decorator Flexible Thoroughfare
        //------------------------------------------------------------------------------------
        
        public static T ApplyAll<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots, T val)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Decorator first, then modifier
            foreach (var slot in modslots) val = slot.SlotApply(val);
            return val;
        }
        
        public static T ApplyDecorators<T, TMod>(this List<IStatModDecorator<T, TMod>> decorators, T val)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            foreach (var dec in decorators) val = dec.ApplyThruDecoratorFirst(val);
            return val;
        }
    }
}


