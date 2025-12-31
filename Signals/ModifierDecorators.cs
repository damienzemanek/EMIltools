using System;
using System.Collections.Generic;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Timers.TimerUtility;

namespace EMILtools.Signals
{
    public interface IStatModCustom<T, TMod>
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        public Type linkType => typeof(TMod);
        public Ref<TMod> strat { get; set; }
        public Stat<T, TMod> stat { get; set; }
        public T Apply(T input) => ApplyThruDecoratorFirst(strat.Value.func(input));
        public T ApplyThruDecoratorFirst(T input);
        public Action OnAdd { get; set; }
        public Action OnRemove{ get; set; }
    }
    
    public abstract class ModifierDecorator<T, TMod> : IStatModStrategy<T>, IStatModCustom<T, TMod>
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        public Ref<TMod> strat { get; set; }
        public Stat<T, TMod> stat { get; set; }
        public abstract T ApplyThruDecoratorFirst(T input);
        public Func<T, T> func
        {
            get => (strat != null) ? strat.ValueRef.func : null;
            set { if (strat != null) strat.ValueRef.func = value; }
        }
        public Action OnAdd { get; set; } = delegate { };
        public Action OnRemove { get; set; } = delegate { };
    }
    
    public class TimedModifier<T, TMod> : ModifierDecorator<T, TMod>, IStatModStrategy<T>
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        public CountdownTimer timer;
        public override T ApplyThruDecoratorFirst(T input) => input;

        public TimedModifier(CountdownTimer timer, Action add = null, Action rm = null)
        {
            this.timer = timer;
            OnAdd += timer.Start;
            timer.OnTimerStop.Add(RemoveModifier);
            
            OnAdd += add;
            OnRemove += rm;
        }

        void RemoveModifier() => stat.RemoveModifier(strat.Value);

    }
}
