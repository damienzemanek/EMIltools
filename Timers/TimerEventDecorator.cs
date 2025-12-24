using System;

namespace EMILtools
{
    namespace Timers
    {
        /// <summary>
        /// A persistent container for multicast delegates that ensures reference stability across the timer's lifecycle.
        /// </summary>
        /// <remarks>
        /// Purpose:
        /// - Reference Stability: Standard C# events/actions are immutable; every subscription (+=) re-assigns the reference. 
        ///   This class provides a constant heap-allocated container so that internal tracking systems (like <see cref="TimerHook"/>) 
        ///   can maintain a permanent link to the event without needing to re-bind every time the underlying delegate is modified.
        /// - Lifecycle Decoupling: Separates the "storage" of the event from the "logic" of the timer, allowing the <see cref="TimerUtility"/> 
        ///   to manage subscriptions safely without knowing the internal state of the <see cref="Timer"/> instance.
        /// - Null-Safety: Encapsulates the invocation logic with a default empty delegate to prevent NullReferenceExceptions during high-frequency ticks.
        /// 
        /// Use Cases:
        /// - Centralized Cleanup: Essential for <see cref="TimerUtility.ShutdownTimers"/>; it allows the utility to iterate through a list 
        ///   of Hooks and call Unsubscribe on a stable target, even if other objects have subscribed/unsubscribed in the meantime.
        /// - Fluent API Support: Enables the <c>.Sub().Sub()</c> chaining pattern by providing a consistent object to return and operate upon.
        /// </remarks>
        public sealed class TimerEventDecorator
        {
            private Action _action = delegate { };
            public void Invoke() => _action?.Invoke();
            public void Add(Action cb) => _action += cb;
            public void Remove(Action cb) => _action -= cb;
        }
    }    
}
