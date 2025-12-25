using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EMILtools.Signals
{
        [Serializable]
        [InlineProperty]
        public class Reference<T> where T : IEquatable<T>
        {
            /// <summary>
            /// Backing field for the base value, Specifically for the custom drawer
            /// </summary>
            [SerializeField, HideLabel] T _val;
            
            /// <summary>
            /// Overridable for Intercept and Notify on change
            /// </summary>
            protected virtual T val { get => _val; set => _val = value; }
            
            /// <summary>
            /// Public facing getter and setters for the actual value
            /// </summary>
            public virtual T Value { get => val; set => val = value;}
            public Reference(T initialValue) => val = initialValue;
        }
        
        /// <summary>
        /// A Reference that can have Modifiers applied to it.
        /// Modifiers are functions that take in the base value and use FuncT T modifiers (that are 
        ///     not lambdas) to return a separate calculated value. The order matters
        /// Intercepts are functions that intercept changes to the base value and Validate and Mutate it before it is stored.
        /// Reactions are actions that are called when the base value changes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [Serializable]
        public sealed class ReferenceModifiable<T> : Reference<T> where T : IEquatable<T>
        {
            /// <summary>
            /// Used for INTERCEPTS when changed but before NOTIFY.
            /// Validates and Mutates the base value when changed to subscribed specifications
            /// 
            /// Used for NOTIFY when changed
            /// (Has nothing to do with Modifiers, except re-calculate them when the base is changed)
            /// </summary>
            protected override T val
            {
                get => base.val;
                set
                {
                    if (HasIntercepts) value = Intercept(value);
                    
                    if (EqualityComparer<T>.Default.Equals(base.val, value)) return;
                    base.val = value;
                    if(notifyChanges) Reactions?.Invoke(value);
                    Calculate(); //Refresh modifiers to new base value for math
                }
            }
            
            /// <summary>
            /// Used for MODIFIERS to store the final math value
            /// </summary>
            T calculated;

            /// <summary>
            /// Value is the pure value when there are no modifiers
            /// Value is the calculated value when there are modifiers
            /// </summary>
            public override T Value
            {
                get => (HasModifiers && !blockIntercepts) ? calculated : val;
                set => val = value;
            }
            
            public List<Func<T, T>> Modifiers = new List<Func<T, T>>();
            public List<Func<T, T>> Intercepts = new List<Func<T, T>>();
            public Action<T> Reactions;
            bool HasModifiers => Modifiers.Count > 0;
            bool HasIntercepts => Intercepts.Count > 0;

            public ReferenceModifiable(T initialValue) : base(initialValue) => Calculate();
            
            [SerializeField] private bool _blockIntercepts = false;
            public bool blockIntercepts
            {
                get => _blockIntercepts;
                set
                {
                    if (_blockIntercepts == value) return;
                    _blockIntercepts = value;
                    Calculate();
                }
            }
            public bool notifyChanges = true;
            
            T Calculate()
            {
                T beingModified = val;
                for(int i = 0; i < Modifiers.Count; i++) 
                    beingModified = Modifiers[i].Invoke(beingModified);
                return calculated = beingModified;
            }

            T Intercept(T newValue)
            {
                for(int i = 0; i < Intercepts.Count; i++) 
                    newValue = Intercepts[i].Invoke(newValue);
                return newValue;
            }
            
            
            
            /// <summary>
            /// Order of modifiers applied matters
            /// Do not use Inline Lambdas because they will be different instances and cannot be removed later
            /// </summary>
            /// <param name="modifier"></param>
            /// <returns></returns>
            public ReferenceModifiable<T> AddModifier(Func<T, T> modifier)
            {
                if (Modifiers.Contains(modifier)) return this;
                
                Modifiers.Add(modifier);
                Calculate();
                return this;
            }

            /// <summary>
            /// Order of modifiers removed matters
            /// </summary>
            /// <param name="modifier"></param>
            /// <returns></returns>
            public ReferenceModifiable<T> RemoveModifier(Func<T, T> modifier)
            {
                if (!Modifiers.Remove(modifier)) return this;
                Calculate();
                return this;
            }

            /// <summary>
            /// 1st T is the input value, 2nd T is the output value
            /// </summary>
            /// <param name="intercept"></param>
            /// <returns></returns>
            public ReferenceModifiable<T> AddIntercept(Func<T, T> intercept)
            {
                if (Intercepts.Contains(intercept)) return this;
                Intercepts.Add(intercept);
                RefreshIntercept();
                return this;

            }
            
            public ReferenceModifiable<T> RemoveIntercept(Func<T, T> intercept)
            {
                Intercepts.Remove(intercept);
                RefreshIntercept();
                return this;
            }
            
            void RefreshIntercept() => val = val;
            

            public ReferenceModifiable<T> AddReaction(Action<T> reaction)
            {
                Reactions += reaction;
                return this;
            }

            public ReferenceModifiable<T> RemoveReaction(Action<T> reaction)
            {
                Reactions -= reaction;
                return this;
            }
            
            
            /// <summary>
            /// Implicit conversion so we can treat this object like its raw value T in math or logic
            /// </summary>
            /// <param name="rmod"></param>
            /// <returns></returns>
            public static implicit operator T(ReferenceModifiable<T> r) => (r != null) ? r.Value : default;
            
        }
    
}
