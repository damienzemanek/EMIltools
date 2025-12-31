using System;
using System.Collections.Generic;
using EMILtools.Signals;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;

public static class ModifierExtensions
{
    
    // Contains modifier type
    public static bool ContainsModifierType<T, TMod>(
        this List<Stat<T, TMod>.ModifierSlot> slots)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        foreach (var slot in slots)
            if (slot.modifier.GetType() == typeof(TMod))
                return true;

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
        
    public static bool Remove<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, TMod modifier)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        for (int i = 0; i < modslots.Count; i++)
        {
            if (modslots[i].modifier.GetType() != modifier.GetType()) continue;
            
            modslots[i].decorator?.OnRemove?.Invoke();
            modslots.RemoveAt(i);
            return true;
        }
        return false;
    }
    
    public static List<Stat<T, TMod>.ModifierSlot> Add<T, TMod>(this List<Stat<T, TMod>.ModifierSlot> modslots, TMod modifier)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        // Contains check
        foreach (var slot in modslots)
            if (slot.modifier.GetType() == typeof(TMod)) return modslots;
        
        Stat<T,TMod>.ModifierSlot newSlot = new Stat<T, TMod>.ModifierSlot
        {
            modifier = modifier
        };
        modslots.Add(newSlot);
        return modslots;
    }
    
    public static List<Stat<T, TMod>.ModifierSlot> Add<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots, IStatModCustom<T, TMod> decorator)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        for (int i = 0; i < modslots.Count; i++) {
            var slot = modslots[i];
            if (slot.modifier.GetType() == decorator.linkType)
            {
                slot.decorator = decorator;
                
                if(slot.decorator.strat == null) slot.decorator.strat = new Ref<TMod>(ref slot.modifier);
                else Debug.Log("Slot already has decorator assigned!, only 1 allowed atm");
                
                slot.decorator.OnAdd?.Invoke();
                
                modslots[i] = slot; 
                break;
            }
        }
                
        return modslots;
    }
    
    public static T ApplyAll<T, TMod>(this List<Stat<T,TMod>.ModifierSlot> modslots, T val)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        foreach (var slot in modslots)
            if (slot.decorator == null)
                val = slot.modifier.Apply(val);
            else
                val = slot.decorator.Apply(val);

        return val;
    }
    
    // Float only ext for Customs
    public static IStatModCustom<float, TMod> WithTimed<TMod>(this TMod mod, float duration)
        where TMod : struct, IStatModStrategy<float>
    {
        IStatModCustom<float, TMod> timedMod = new TimedModifier<float, TMod>(new CountdownTimer(duration));
        return timedMod;
    }
    
    public static IStatModCustom<T, TMod> WithTimed<T, TMod>(this TMod mod, float duration)
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        // Not setting the ref to the modifier strategy here
        // that happens after sending the modifier to the IStatUser
        IStatModCustom<T, TMod> timedMod = new TimedModifier<T, TMod>(new CountdownTimer(duration));
        return timedMod;
    }
    
    
    
    // Float ext for regular
    public static void ModifyStatUser<TMod>(this IStatUser recipient, ref TMod strat)
        where TMod : struct, IStatModStrategy<float>
    {
        // Redirects to the main method with float explicitly set
        recipient.ModifyStatUser<float, TMod>(ref strat);
    }
}
