using System;

namespace HVR
{
    public class TriggerActionJob : Job
    {
        Action action;
        public TriggerActionJob(Action _action)
        {
            action = _action;
        }

        public override bool OnRun()
        {
            action();
            return true;
        }
    }
}