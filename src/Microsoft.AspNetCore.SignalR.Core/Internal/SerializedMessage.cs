namespace Microsoft.AspNetCore.SignalR.Internal
{
    public readonly struct SerializedMessage
    {
        public string ProtocolName { get; }
        public byte[] Serialized { get; }

        public SerializedMessage(string protocolName, byte[] serialized)
        {
            ProtocolName = protocolName;
            Serialized = serialized;
        }
    }
}