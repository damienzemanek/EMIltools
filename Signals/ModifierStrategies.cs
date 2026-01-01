using System;
using System.Linq.Expressions;
using EMILtools.Timers;
using UnityEngine;

namespace EMILtools.Signals
{
    public static class ModifierStrategies
    {
        public interface IStatModStrategy<T> where T : struct
        {
            public ulong hash { get; set; }
            public Func<T, T> func { get; set; }

            public T Apply(T input) => func(input);
        }
        
        /// <summary>
        /// Simple struct wrapper for Func<T, T> to modify Health on IStatUser
        /// Used for reflection to find the Stat to modify on the IStatUser
        /// </summary>
        [Serializable]
        public struct SpeedModifier : IStatModStrategy<float>
        {
            public ulong hash { get; set; } // For removal comparisons 
            public Func<float, float> func { get; set; }

            public SpeedModifier(Expression<Func<float, float>> expr)
            {
                hash = Hashing.Fnv1a64(expr.ToString());
                func = expr.Compile();
            }
        }
        
        
    }

}
