// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// MSTest utility for testing code operating against a stream.
    /// </summary>
    public class SerializerAssert
    {
        private static SerializerAssert singleton = new SerializerAssert();

        public static SerializerAssert Singleton { get { return singleton; } }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="XmlSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public void UsingXmlSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (codeThatChecks == null)
            {
                throw new ArgumentNullException("codeThatChecks");
            }

            XmlSerializer serializer = new XmlSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="XmlSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public void UsingXmlSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingXmlSerializer(typeof(T), objectInstance, codeThatChecks);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public void UsingDataContractSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (codeThatChecks == null)
            {
                throw new ArgumentNullException("codeThatChecks");
            }

            DataContractSerializer serializer = new DataContractSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public void UsingDataContractSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingDataContractSerializer(typeof(T), objectInstance, codeThatChecks);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractJsonSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingDataContractJsonSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (codeThatChecks == null)
            {
                throw new ArgumentNullException("codeThatChecks");
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractJsonSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public void UsingDataContractJsonSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingDataContractJsonSerializer(typeof(T), objectInstance, codeThatChecks);
        }
    }
}
