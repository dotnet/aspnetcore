using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class BufferSizeControl : IBufferSizeControl
    {
        private readonly long _maxSize;
        private readonly IConnectionControl _connectionControl;
        private readonly KestrelThread _connectionThread;

        private readonly object _lock = new object();

        private long _size;
        private bool _connectionPaused;

        public BufferSizeControl(long maxSize, IConnectionControl connectionControl, KestrelThread connectionThread)
        {
            _maxSize = maxSize;
            _connectionControl = connectionControl;
            _connectionThread = connectionThread;
        }

        private long Size
        {
            get
            {
                return _size;
            }
            set
            {
                // Caller should ensure that bytes are never consumed before the producer has called Add()
                Debug.Assert(value >= 0);
                _size = value;
            }
        }

        public void Add(int count)
        {
            Debug.Assert(count >= 0);

            if (count == 0)
            {
                // No-op and avoid taking lock to reduce contention
                return;
            }

            lock (_lock)
            {
                Size += count;
                if (!_connectionPaused && Size >= _maxSize)
                {
                    _connectionPaused = true;
                    _connectionThread.Post(
                        (connectionControl) => ((IConnectionControl)connectionControl).Pause(),
                        _connectionControl);
                }
            }
        }

        public void Subtract(int count)
        {
            Debug.Assert(count >= 0);

            if (count == 0)
            {
                // No-op and avoid taking lock to reduce contention
                return;
            }

            lock (_lock)
            {
                Size -= count;
                if (_connectionPaused && Size < _maxSize)
                {
                    _connectionPaused = false;
                    _connectionThread.Post(
                        (connectionControl) => ((IConnectionControl)connectionControl).Resume(),
                        _connectionControl);
                }
            }
        }
    }
}
