using System;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Core;

namespace EMILtools.Timers
{
    [Serializable]
    public abstract class Timer
    {
        public float Duration { get => initialTime.Value;}
        protected Ref<float> initialTime;
        protected float Time { get; set; }

        public bool isRunning { get; protected set; } = false;
        public float Progress => Time / initialTime.Value;

        public ActionDecorator OnTimerStart = new();
        public ActionDecorator OnTimerStop = new();
        public ActionDecorator OnTimerTick = new();

        public Timer(Ref<float> _initialTime)
        {
            initialTime = _initialTime;
            isRunning = false;
        }
        
        public Timer(Ref<float> _initialTime,
                Action[] OnTimerStartCbs = null,
                Action[] OnTimerTickCbs = null,
                Action[] OnTimerStopCbs = null)
        {
            if(OnTimerStartCbs != null && OnTimerStartCbs.Length > 0) OnTimerStart.Add(OnTimerStartCbs);
            if(OnTimerTickCbs != null && OnTimerTickCbs.Length > 0) OnTimerTick.Add(OnTimerTickCbs);
            if(OnTimerStopCbs != null && OnTimerStopCbs.Length > 0) OnTimerStop.Add(OnTimerStopCbs);
            
            initialTime = _initialTime;
            isRunning = false;
        }

        public void Start()
        {
            if (initialTime.Value <= 0)
            {
                this.Warn("Please set an initial time for this timer");
                return;
            }

            Time = initialTime.Value;
            if (!isRunning)
            {
                isRunning = true;
                OnTimerStart?.Invoke();
                this.Log("Started Timer");
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            OnTimerStop?.Invoke();
            this.Log("Stopped Timer");
        }

        public void Pause() => isRunning = false;
        public void Resume() => isRunning = true;

        public abstract void TickImplementation(float deltaTime);

        public void TryTick(float deltaTime)
        {
            if (!isRunning) return;
            // this.Log($"Ticking, Prog: {Progress}");
            TickImplementation(deltaTime);
            OnTimerTick.Invoke();

        }
    }
}