using System.Collections.Generic;

namespace semestral_work.Graphics
{
    internal class FpsCounter
    {
        private readonly Queue<double> _timestamps = new();
        private double _timeSinceStart = 0;

        public double CurrentFps { get; private set; }

        public void Update(double deltaTime)
        {
            _timeSinceStart += deltaTime;
            _timestamps.Enqueue(_timeSinceStart);

            while (_timestamps.Count > 0 && _timestamps.Peek() < _timeSinceStart - 1.0)
                _timestamps.Dequeue();

            CurrentFps = _timestamps.Count;
        }
    }
}
