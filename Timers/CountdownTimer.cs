using System;
using static EMILtools.SignalUtility;

namespace EMILtools.Timers
{
    [Serializable]
    public class CountdownTimer : Timer
    {
        public CountdownTimer(Reference<float> _initialTime) : base(_initialTime) { }

        public override void TickImplementation(float deltaTime)
        {
            if (Time > 0) Time -= deltaTime;
            if (Time <= 0) { Time = 0; Stop(); }
        }

        public bool isFinished() => Time <= 0;
        public void Reset() => Time = initialTime.Value;
        public void Reset(float newInitialTime) => Time = newInitialTime;
            
    }   
}

