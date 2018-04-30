using System.Messaging;

namespace SampleMsmqHost
{
    public class MsmqOptions
    {
        public string Path { get; set; }

        public bool SharedModeDenyReceive { get; set; } = false;

        public bool EnableCache { get; set; } = false;

        public QueueAccessMode AccessMode { get; set; } = QueueAccessMode.SendAndReceive;
    }
}