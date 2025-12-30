using System;
using EMILtools.Signals;
using UnityEngine;
using static EMILtools.Signals.ModifierStrategies;

public static class ModifierExtensions
{
    public static IStatModStrategy WithTimed(this IStatModStrategy strategy, float duration) 
    {
        // ITimedStatModStrategy timedStrategy = strategy as ITimedStatModStrategy;
        // return strategy.WithTimed(duration);
    }
}
