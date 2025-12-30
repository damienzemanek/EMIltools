using System;
using UnityEngine;

namespace EMILtools.Signals
{
    public static class ModifierStrategies
    {
        /// <summary>
        /// Used for storage of different strategy types in a single collection
        /// </summary>
        public interface IStatModStrategy { }
        
        /// <summary>
        /// Used for invoke chaining
        /// </summary>
        public interface IStatModStrategy<T> : IStatModStrategy where T : struct
        {
            Func<T, T> func { get; set; }
            public T Apply(T input) => func(input);
        }
        
        /// <summary>
        /// Simple struct wrapper for Func<T, T> to modify Health on IStatUser
        /// Used for reflection to find the Stat to modify on the IStatUser
        /// </summary>
        [Serializable]
        public struct SpeedModifier : IStatModStrategy<float>
        {
            public Func<float, float> func { get; set; }
            public SpeedModifier(Func<float, float> func) => this.func = func;
        }
        
        
    }

}
