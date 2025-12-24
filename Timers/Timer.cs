using System;
using EMILtools.Extensions;
using Extensions;
using static EMILtools.SignalUtility;

namespace EMILtools.Timers
{
    [Serializable]
    public abstract class Timer
    {
        //Test 6
        
        protected Reference<float> initialTime;
        protected float Time { get; set; }

        public bool isRunning { get; protected set; } = false;
        public float Progress => Time / initialTime.Value;

        public TimerEventDecorator OnTimerStart = new();
        public TimerEventDecorator OnTimerStop = new();
        public TimerEventDecorator OnTimerTick = new();

        public Timer(Reference<float> _initialTime)
        {
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