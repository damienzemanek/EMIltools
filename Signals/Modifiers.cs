using System;
using EMILtools.Timers;
using static EMILtools.Timers.TimerUtility;

/// (System Leve ) Dependent on Timer.cs and Signals.Stat.cs
namespace EMILtools.Signals
{
    public static class TimerModifierFluentFactory
    {
        /// <summary>
        /// Creates a new TimedModifier. 
        /// Chain with WithAutoStart() if you want to start the timer right away
        ///
        /// Ex: Player gets a speed boost that lasts 10 seconds.
        /// Call: this.NewTimedModifier(myStat, myModifier, 10f).WithAutoStart();
        /// </summary>
        /// <param name="itimeruser"></param>
        /// <param name="stat"></param>
        /// <param name="modifier"></param>
        /// <param name="duration"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static TimedModifier<T> NewTimedModifier<T>(
            this ITimerUser itimeruser,
            Stat<T> stat, 
            Func<T, T> modifier, 
            float duration) 
        where T : IEquatable<T>
        {
            var timedModifier = new TimedModifier<T>(stat, modifier, duration);
            stat.AddModifier(modifier);
            itimeruser.InitializeTimers((timedModifier.Timer, false));
            return timedModifier;
        }

        public static TimedModifier<T> WithAutoStart<T>(this TimedModifier<T> timedModifier) 
        where T : IEquatable<T>
        {
            timedModifier.Start();
            return timedModifier;
        }
    }
    
    /// <summary>
    /// Timed Decorator of type Func<T,T> for Stat<T>
    /// Original is kept as Func<T,T> to remain Lightweight
    /// </summary>
    public class TimedModifier<T> where T : IEquatable<T>
    {
        readonly Stat<T> Stat;
        readonly Func<T, T> Modifier;
        public readonly CountdownTimer Timer;
        
        public TimedModifier(Stat<T> stat, Func<T, T> modifier, float initialTime)
        {
            Stat = stat;
            Modifier = modifier;
            
            Timer = new CountdownTimer(initialTime);

            Timer.OnTimerStop.Add(() => stat.RemoveModifier(Modifier));
        }
        
        public void Start() => Timer.Start();
    }

}
