// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

using System.Runtime.Serialization;

namespace System.Net.Http.HPack
{
    // TODO: Should this be public?
    [Serializable]
    internal class HuffmanDecodingException : Exception, ISerializable
    {
        public HuffmanDecodingException()
        {
        }

        public HuffmanDecodingException(string message)
            : base(message)
        {
        }

        protected HuffmanDecodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}
