using System.Collections.Concurrent;
using System.Timers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    public class Timer : PmdComponent
    {

        private readonly System.Timers.Timer _timer;
        private readonly ConcurrentBag<Action> _callbacks;
        private readonly ProcessingOrder _order;

        public Timer(PmdComponentConfig config, double intervalMs, ProcessingOrder order) : base(config)
        {
            _callbacks = [];
            _order = order;
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Tick();
        }

        private void Tick()
        {
            foreach (var callback in _callbacks)
            {
                SubmitTask(callback, _order);
            }
        }

        public void RegisterTask(Action task)
        {
            _callbacks.Add(task);
        }

        public override void Cleanup()
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Enabled = false;
        }

    }
}
