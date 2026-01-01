using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        public bool removable { get; set; }
        public Type linkType => typeof(TMod);
        public ulong hash { get; set; }
        public Func<T, T> func { get; set; }
        public Stat<T, TMod> stat { get; set; }
        public T Apply(T input) => ApplyThruDecoratorFirst(input);
        public T ApplyThruDecoratorFirst(T input);
        public Action OnAdd { get; set; }
        public Action OnRemove{ get; set; }
    }
    
    public abstract class ModifierDecorator<T, TMod> : IStatModStrategy<T>, IStatModCustom<T, TMod>
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        public bool removable { get; set; }
        public Stat<T, TMod> stat { get; set; }
        public abstract T ApplyThruDecoratorFirst(T input);
        public ulong hash { get; set; }
        public Func<T, T> func { get; set; }
        public Action OnAdd { get; set; } = delegate { };
        public Action OnRemove { get; set; } = delegate { };

        public ModifierDecorator(Func<T, T> func, ulong hash)
        {
            removable = false;
            this.hash = hash;
            this.func = func;
        }
    }
    
    public class TimedModifier<T, TMod> : ModifierDecorator<T, TMod>, IStatModStrategy<T>, ITimerUser
        where T : struct
        where TMod : struct, IStatModStrategy<T>
    {
        public CountdownTimer timer;
        public override T ApplyThruDecoratorFirst(T input) => input;

        public TimedModifier(Func<T, T> func, ulong hash, CountdownTimer timer, Action add = null, Action rm = null) : base(func, hash)
        {
            removable = false;
            this.timer = timer;
            this.InitializeTimers((timer, false));
            
            OnAdd += timer.Start;
            OnAdd += SetupTimerRemove;
            OnAdd += add;

            OnRemove += this.ShutdownTimers;
            OnRemove += rm;
        }

        void SetupTimerRemove() => timer.OnTimerStop.Add(() =>
        {
            removable = true;
            stat.RemoveModifier(hash);
        });

        public void ForceStop(Stat<T,TMod> stat)
        {
            removable = true;
            this.stat = stat;
            timer.OnTimerStop?.Invoke();
        }

    }
}
