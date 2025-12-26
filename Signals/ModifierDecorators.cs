using System;
using EMILtools.Timers;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Timers.TimerUtility;

/// (System Leve ) Dependent on Timer.cs and Signals.Stat.cs
namespace EMILtools.Signals
{
    // public static class TimerModifierFluentFactory
    // {
    //     /// <summary>
    //     /// Creates a new TimedModifier. 
    //     /// Chain with WithAutoStart() if you want to start the timer right away
    //     ///
    //     /// Ex: Player gets a speed boost that lasts 10 seconds.
    //     /// Call: this.NewTimedModifier(myStat, myModifier, 10f).WithAutoStart();
    //     /// </summary>
    //     /// <param name="itimeruser"></param>
    //     /// <param name="stat"></param>
    //     /// <param name="modifier"></param>
    //     /// <param name="duration"></param>
    //     /// <typeparam name="T"></typeparam>
    //     /// <returns></returns>
    //     public static TimedModifier<T, TModStrat> NewTimedModifier<T, TModStrat>(
    //         this ITimerUser itimeruser,
    //         Stat<T, TModStrat> stat, 
    //         float duration) 
    //     where T : IEquatable<T>
    //     where TModStrat : IStatModStrategy
    //     {
    //         var timedModifier = new TimedModifier<T, TModStrat>(stat, duration);
    //         stat.AddModifier(modifier);
    //         itimeruser.InitializeTimers((timedModifier.Timer, false));
    //         return timedModifier;
    //     }
    //
    //     public static TimedModifier<T, TModStrat> WithAutoStart<T, TModStrat>(this TimedModifier<T, TModStrat> timedModifier) 
    //     where T : IEquatable<T>
    //     where TModStrat : IStatModStrategy
    //     {
    //         timedModifier.Start();
    //         return timedModifier;
    //     }
    // }
    
    public class Modifier<T>
    {
        public virtual Func<T, T> func { get; set; }
        public Modifier(Func<T, T> func) => this.func = func;
    }
    
    // /// <summary>
    // /// Timed Decorator of type Func<T,T> for Stat<T>
    // /// Original is kept as Func<T,T> to remain Lightweight
    // /// </summary>
    // public class TimedModifier<T> : Modifier<T>
    //     where T : IEquatable<T>
    //     where TModStrat : IStatModStrategy
    // {
    //     readonly Stat<T, TModStrat> Stat;
    //     public readonly CountdownTimer Timer;
    //     
    //     public TimedModifier(Stat<T, TModStrat> stat, float initialTime) : base(initialTime)
    //     {
    //         Stat = stat;
    //         
    //         Timer = new CountdownTimer(initialTime);
    //
    //         Timer.OnTimerStop.Add(() => stat.RemoveModifier(Modifier));
    //     }
    //     
    //     public void Start() => Timer.Start();
    // }

}
