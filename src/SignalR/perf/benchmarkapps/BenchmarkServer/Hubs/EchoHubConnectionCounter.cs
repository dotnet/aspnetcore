using System;

public class EchoHubConnectionCounter
{
    private int _connectionCount;
    private int _peakConnectionCount;

    private readonly object _lock = new object();

    private DateTime _start = DateTime.Now;

    public string Status
    {
        get
        {
            lock (_lock)
            {
                return $"{_connectionCount} current, {_peakConnectionCount} peak.";
            }
        }
    }

    private void LogConnections(string label)
    {
        int connectionCount;
        lock (_lock)
        {
            connectionCount = _connectionCount;
        }
        if (connectionCount < 100 || connectionCount % 100 == 0)
        {
            var timeSinceServerStart = DateTime.Now.Subtract(_start).ToString(@"hh\:mm\:ss");
            Console.WriteLine($"[{timeSinceServerStart}] {label}: {Status}");
        }
    }

    public void Connected()
    {
        lock (_lock)
        {
            _connectionCount++;
            _peakConnectionCount = Math.Max(_connectionCount, _peakConnectionCount);
        }
        LogConnections("Connected");
    }

    public void Disconnected() {
        lock (_lock) {
            _connectionCount--;
        }
        LogConnections("Disconnected");
    }
}