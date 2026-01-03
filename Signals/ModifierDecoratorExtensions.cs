using System;
using EMILtools.Timers;
using JetBrains.Annotations;
using UnityEngine;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{
    public static class ModifierDecoratorExtensions
    {

        //--------------------------------------------------------------------------------------
        //                  Decorator Timer Overrides (Custom Timer)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        
        //---------------------------------  FLOAT  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<TMod>(this (TMod mod, IStatUser user) data, CountdownTimer timer,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where TMod : struct, IStatModStrategy<float>
                => data.WithTimer<float, TMod>(timer, OnAddDecorCBs, OnRemoveDecorCBs);

        //---------------------------------  GENERIC  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<T, TMod>(this (TMod mod, IStatUser user) data, CountdownTimer timer,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TMod> decor = new StatModDecTimed<T, TMod>(
                data.mod.hash,
                timer,
                OnAddDecorCBs,
                OnRemoveDecorCBs);

            data.user.AddDecorator(decor);

            return data;
        }


        //--------------------------------------------------------------------------------------
        //             Decorator Timer Overrides (out for debugs, and flexibility)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        
        //---------------------------------  FLOAT  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<TMod>(this (TMod mod, IStatUser user) data, float duration,
                out CountdownTimer timer,
                out StatModDecTimed<float, TMod> decor,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where TMod : struct, IStatModStrategy<float>
                => data.WithTimer<float, TMod>(duration, out timer, out decor, OnAddDecorCBs, OnRemoveDecorCBs);
        
        //---------------------------------  GENERIC  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<T, TMod>(this (TMod mod, IStatUser user) data, float duration,
                out CountdownTimer timer,
                out StatModDecTimed<T, TMod> decor,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            decor = new StatModDecTimed<T, TMod>(
                data.mod.hash,
                timer = new CountdownTimer(duration),
                OnAddDecorCBs,
                OnRemoveDecorCBs);

            data.user.AddDecorator(decor);

            return data;

        }
        
        //--------------------------------------------------------------------------------------
        //                          Decorator Timer Overrides (NO OUTS)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        
        //---------------------------------  FLOAT  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<TMod>(this (TMod mod, IStatUser user) data, float duration,
            Action[] OnAddDecorCBs = null,
            Action[] OnRemoveDecorCBs = null)
            where TMod : struct, IStatModStrategy<float> 
                => data.WithTimer<float, TMod>(duration, OnAddDecorCBs, OnRemoveDecorCBs);
        
        //---------------------------------  GENERIC  ------------------------------------------
        public static (TMod, IStatUser) WithTimer<T, TMod>(this (TMod mod, IStatUser user) data, float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TMod> decor = new StatModDecTimed<T, TMod>(
                data.mod.hash,
                new CountdownTimer(duration));
            
            data.user.AddDecorator(decor);

            return data;
        }
        
    }

}
