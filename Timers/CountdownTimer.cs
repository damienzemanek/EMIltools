using System;
using EMILtools.Signals;

namespace EMILtools.Timers
{
    [Serializable]
    public class CountdownTimer : Timer
    {
        public CountdownTimer(Ref<float> _initialTime) : base(_initialTime) { }
        public CountdownTimer(float _initialTime) : base(new Ref<float>(_initialTime)) { }


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

