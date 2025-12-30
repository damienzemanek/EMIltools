using System;
using EMILtools.Timers;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Timers.TimerUtility;

namespace EMILtools.Signals
{
    public interface ITimedStatModStrategy : IStatModStrategy 
    {
        CountdownTimer timer { get; set; }
        public IStatModStrategy modifier { get; set; }
    }
    
    public struct TimedStatModStrategy<T> : ITimedStatModStrategy, IStatModStrategy<T> where T : struct
    {
        public CountdownTimer timer { get; set; }
        public IStatModStrategy modifier { get; set; }

        public Func<T, T> func
        {
            get => ((IStatModStrategy<T>)modifier).func;
            set => ((IStatModStrategy<T>)modifier).func = value;
        }
        
        public TimedStatModStrategy(IStatModStrategy modifier, float duration)
        {
            this.modifier = modifier;
            timer = CreateCountdownTimer(duration);
        }
        
    }

    
}
