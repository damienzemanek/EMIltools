using System;
using System.Collections.Generic;
using System.Reflection;
using EMILtools.Core;
using UnityEngine;
using Sirenix.OdinInspector;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{
        public interface IStatUser
        {
            public ModifierRouter router { get; set; }
        }
    
        [Serializable]
        [InlineProperty]
        public class Ref<T> where T : struct
        {
            /// <summary>
            /// Backing field for the base value, Specifically for the custom drawer
            /// </summary>
            [HorizontalGroup("Split", Width = 0.3f)]
            [SerializeField, HideLabel] [VerticalGroup("Split/Right")] protected T _val;
            
            /// <summary>
            /// Overridable for Intercept and Notify on change
            /// </summary>
            protected virtual T val { get => _val; set => _val = value; }
            
            /// <summary>
            /// Public facing getter and setters for the actual value
            /// </summary>
            [ShowInInspector]
            public virtual T Value { get => val; set => val = value;}
            
            public virtual ref T ValueRef => ref _val;
            
            public Ref(T initialValue) => val = initialValue;
            public Ref(ref T initialValue) => val = initialValue;
        }

        public interface IStat { }
        
        /// <summary>
        /// A Multi-configurable event bus variable
        /// - Modifiers are functions that take in the base value and use FuncT T modifiers (that are 
        ///     not lambdas) to return a separate calculated value. The order matters
        /// - Intercepts are functions that intercept changes to the base value and Validate and Mutate it before it is stored.
        /// - Reactions are actions that are called when the base value changes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [Serializable]
        [InlineProperty]
        public sealed class Stat<T, TMod> : Ref<T>, IStat 
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            public struct ModifierSlot
            {
                public TMod modifier;
                public IStatModCustom<T, TMod> decorator;
            }
            
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
            [ShowInInspector, ReadOnly, HideLabel]
            [VerticalGroup("Split/Left")]
            [PropertyOrder(0)]
            public override T Value
            {
                get => (HasModifiers && !blockIntercepts) ? calculated : val;
                set => val = value;
            }

            // Not initialized here to save on memory, When using lazy initialize
            private List<ModifierSlot> _modifiers;
            private List<Func<T, T>> _intercepts;
            
            // Access for Configurations
            public IReadOnlyList<ModifierSlot> Modifiers => _modifiers;
            public IReadOnlyList<Func<T, T>> Intercepts => _intercepts;
            public event Action<T> Reactions; //Kept as event so outside scripts can't invoke it directly
            
            
            bool HasModifiers => _modifiers != null && _modifiers.Count > 0;
            bool HasIntercepts => _intercepts != null && _intercepts.Count > 0;

            public Stat(T initialValue) : base(initialValue) => Calculate();
            
            // Settings
            [VerticalGroup("Split/Right")]
            [LabelWidth(100)]
            [PropertyOrder(1)]
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
            [VerticalGroup("Split/Right")]
            [LabelWidth(100)]
            [PropertyOrder(2)]
            public bool notifyChanges = true;
            
            T Calculate()
            {
                if (!HasModifiers) return val;
                T beingModified = _modifiers.ApplyAll(val);
                Debug.Log("Old calculated value: " + val);
                Debug.Log("New calculated value: " + beingModified);
                return calculated = beingModified;
            }

            T Intercept(T newValue)
            {
                for(int i = 0; i < Intercepts.Count; i++) 
                    newValue = Intercepts[i].Invoke(newValue);
                return newValue;
            }
            
            
            
            /// <summary>
            /// Modifiers are functions that modify the base value and replace the Value getter with the calcualted value
            /// Example: Player picked up "Speed" ability. Player speed is increased
            /// Order of modifiers applied matters
            /// Do not use Inline Lambdas because they will be different instances and cannot be removed later
            /// </summary>
            /// <param name="modifier"></param>
            /// <returns></returns>
            
            // Struct
            public void AddModifier(TMod modifier)
            {
                Debug.Log("Adding Modifier: " + modifier);
                if(Modifiers == null) _modifiers = new List<ModifierSlot>();
                if (_modifiers.ContainsModifierType()) return;
                _modifiers.Add(modifier);
                Debug.Log($"Added Modifier : {modifier}. Total Modifiers now: {_modifiers.Count}");
                Calculate();
            }
            
            // Class
            public void AddDecorator(IStatModCustom<T, TMod> decorator)
            {
                Debug.Log("Appending Decorator: " + decorator);
                if (_modifiers.ContainsDecorator(decorator)) return;
                _modifiers.Add(decorator);
                Debug.Log($"Added Decorator : {decorator}. Total Modifiers now: {_modifiers.Count}");

                Calculate();
            }

            public void RemoveModifier(TMod modifier)
            {
                if (!_modifiers.Remove(modifier)) return;
                Calculate();
            }

            /// <summary>
            /// Intercepts are called before the base value is changed.
            /// Example: Player looses health, health is clamped to a minimum of 0
            /// 1st T is the input value, 2nd T is the output value
            /// </summary>
            /// <param name="intercept"></param>
            /// <returns></returns>
            public Stat<T, TMod> AddIntercept(Func<T, T> intercept)
            {
                if(Intercepts == null) _intercepts = new List<Func<T, T>>();
                if (_intercepts.Contains(intercept)) return this;
                _intercepts.Add(intercept);
                RefreshIntercept();
                return this;

            }
            
            public Stat<T, TMod> RemoveIntercept(Func<T, T> intercept)
            {
                _intercepts.Remove(intercept);
                RefreshIntercept();
                return this;
            }
            
            void RefreshIntercept() => val = val;
            

            /// <summary>
            /// Reactions events that are called when the base value changes
            /// Example: Player looses health, an "On Hit" event is called
            /// </summary>
            /// <param name="reaction"></param>
            /// <returns></returns>
            public Stat<T, TMod> AddReaction(Action<T> reaction)
            {
                if(Reactions == null) Reactions = reaction;
                Reactions += reaction;
                return this;
            }

            public Stat<T, TMod> RemoveReaction(Action<T> reaction)
            {
                Reactions -= reaction;
                return this;
            }
            
            
            /// <summary>
            /// Implicit conversion so we can treat this object like its raw value T in math or logic
            /// </summary>
            /// <param name="r"></param>
            /// <returns></returns>
            public static implicit operator T(Stat<T, TMod> r) => (r != null) ? r.Value : default;
            
        }
    
}
