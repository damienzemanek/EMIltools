using System;
using UnityEngine;

namespace EMILtools.Signals
{
    public static class ModifierStrategies
    {
        public interface IStatModStrategy
        {
            public object Invoke(object val);
        }
        
        public interface IStatModStrategy<T> : IStatModStrategy
        {
            public T Apply(T val);
        }
        
        public abstract class StatModStrategy<T> : IStatModStrategy<T>
        {
            public abstract T Apply(T value);

            // non-generic interface pass-through
            public object Invoke(object value) => Apply((T)value);
        }
        
        [Serializable]
        public struct SpeedModStrategy<T> : IStatModStrategy<T>
        {
            public Func<T, T> func;

            public SpeedModStrategy(Func<T, T> func)
            {
                this.func = func;
            }

            public T Apply(T value) => func(value);
            public object Invoke(object value) => Apply((T)value);
        }
    }

}
