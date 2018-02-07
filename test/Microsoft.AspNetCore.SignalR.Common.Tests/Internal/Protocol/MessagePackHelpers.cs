using System;
using System.Linq;
using MsgPack;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public static class MessagePackHelpers
    {
        public static MessagePackObject Array(params MessagePackObject[] items) =>
            new MessagePackObject(items);

        public static MessagePackObject Map(params (MessagePackObject Key, MessagePackObject Value)[] items) =>
            new MessagePackObject(new MessagePackObjectDictionary(items.ToDictionary(i => i.Key, i => i.Value)));
    }
}
