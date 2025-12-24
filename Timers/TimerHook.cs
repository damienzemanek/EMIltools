using System;

namespace EMILtools
{
    namespace Timers
    {
        /// <summary>
        /// A lightweight handle that pairs a specific event with a callback for automated lifecycle management.
        /// </summary>
        /// <remarks>
        /// Purpose:
        /// - Subscription Tracking: Encapsulates the link between a <see cref="TimerEventDecorator"/> and an <see cref="Action"/> 
        ///   into a single object, allowing the system to remember exactly what was subscribed and where.
        /// - Unified Cleanup: Provides a standard interface for <see cref="TimerUtility"/> to perform a bulk "Unsubscribe" 
        ///   during shutdown without needing to manage multiple disparate lists of actions.
        /// 
        /// Use Cases:
        /// - Manual Unbinding: Allows <see cref="TimerUtility.Unsub"/> to locate and remove a specific callback from a decorator.
        /// - Automated Shutdown: Used by <see cref="TimerUtility.ShutdownTimers"/> to break all event links for an object 
        ///   in a single contiguous loop, preventing memory leaks and orphaned callbacks.
        /// </remarks>
        public sealed class TimerHook
        {
            private readonly TimerEventDecorator targEvent;
            readonly Action cb;
            
            public TimerEventDecorator Event => targEvent;
                
            public TimerHook(TimerEventDecorator _targEvent, Action _cb)
            {
                targEvent = _targEvent;
                cb = _cb;
            }

            public TimerHook Subscribe()
            {
                targEvent.Add(cb);
                return this;
            }

            public TimerHook Unsubscribe()
            {
                targEvent.Remove(cb);
                return this;
            }
        }
    }    
}
