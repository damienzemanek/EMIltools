using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EMILtools.Core;
using EMILtools.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EMILtools.Timers
{
        public static class TimerUtility
        {
            public static bool GlobalTickerInitialized = false;
            
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            public static void InitializeGlobalTicker()
            {
                if(GameObject.FindAnyObjectByType(typeof(GlobalTicker))) return;
                GlobalTicker ticker = new GameObject("GlobalTicker").AddComponent<GlobalTicker>();
                Object.DontDestroyOnLoad(ticker.gameObject);
                ticker.gameObject.hideFlags = HideFlags.DontSave;
                GlobalTickerInitialized = true;
            }
        
            public class GlobalTicker : MonoBehaviour
            {
                void Update() => TimerUtility.TickAllUpdates(Time.deltaTime);
                void FixedUpdate() => TimerUtility.TickAllFixed(Time.fixedDeltaTime);
            }
            
            /// <summary>
            /// A local "receipt" class that tracks all timers and hooks owned by a specific object.
            /// </summary>
            /// <remarks>
            /// Purpose: Serves as a cleanup manifest. It stores which timers were added to which global 
            /// buffers and tracks active event subscriptions so they can be bulk-removed during Shutdown.
            /// </remarks>
            [Serializable]
            public class TimerUser
            {
                //Typically there will be less than 20 timers for each, so Remove() in shutdown is faster for list than hashset
                public readonly List<Timer> UpdateTimers = new();
                public readonly List<Timer> FixedTimers = new();
                public readonly List<TimerHook> Hooks = new();
            }
            
            /// <summary>
            /// Global storage engine for the Timer system.
            /// </summary>
            /// <remarks>
            /// - registry: A leak-safe CWT mapping owners to their TimerUser manifest.
            /// - updateBuffer/fixedBuffer: High-performance linear lists used for O(1) frame-by-frame ticking.
            /// </remarks>
            private static readonly ConditionalWeakTable<ITimerUser, TimerUser> registry = new();
            private static readonly List<Timer> updateBuffer = new();
            private static readonly List<Timer> fixedBuffer = new();
            
            public interface ITimerUser { }
            
            /// <summary>
            /// Adds timers to the global buffers.
            /// You still need to call Start() on the timers to run them
            /// </summary>
            /// <param name="itimeruser"></param>
            /// <param name="initialize"></param>
            /// <returns></returns>
            public static ITimerUser InitializeTimers(this ITimerUser itimeruser, params (Timer timer, bool isFixed)[] initialize)
            {
                if(initialize == null || initialize.Length == 0) return itimeruser;
                
                //Add the user from the registry
                var user = registry.GetOrCreateValue(itimeruser);
                foreach (var (timer, isFixed) in initialize)
                {
                    // Add the timer to the user's correct timer list, so ShutdownTimers() knows which timer to remove from the buffer
                    if (isFixed)
                    {
                        user.FixedTimers.Add(timer);
                        fixedBuffer.Add(timer);
                    }
                    else
                    {
                        user.UpdateTimers.Add(timer);
                        updateBuffer.Add(timer);
                    }
                }
                return itimeruser;
            }
            
            public static ITimerUser Sub(this ITimerUser itimeruser, ActionDecorator targEvent, Action cb)
            {
                if(targEvent == null || cb == null) return itimeruser;

                //Initialize the hook
                var hook = new TimerHook(targEvent, cb).Subscribe();
                
                //Add the hook to the hook list (doesn't matter if it's fixed or not)
                if (!registry.TryGetValue(itimeruser, out var user))
                { Debug.LogError("Please initialize timers before subscribing to events"); return itimeruser;}
                user.Hooks.Add(hook);
                
                return itimeruser;
            }

            public static void ShutdownTimers(this ITimerUser itimeruser)
            {
                if (!registry.TryGetValue(itimeruser, out var user)) return;
                
                //Unsubscribe all hooks (break their links)
                foreach (var hook in user.Hooks) hook.Unsubscribe();

                //Remove all timers from the buffers
                // List Remove is faster than hashset remove for small lists
                foreach(var timer in user.UpdateTimers) updateBuffer.FastRemove(timer);
                foreach (var timer in user.FixedTimers) fixedBuffer.FastRemove(timer);
            }

            public static void TickAllUpdates(float dt)
            {
                // Using a for loop is safer if a timer is removed during its own tick
                for (int i = updateBuffer.Count - 1; i >= 0; i--)
                    updateBuffer[i].TryTick(dt);
            }

            public static void TickAllFixed(float dt)
            {
                // Using a for loop is safer if a timer is removed during its own tick
                for (int i = fixedBuffer.Count - 1; i >= 0; i--)
                    fixedBuffer[i].TryTick(dt);

            }


        }
}
