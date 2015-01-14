using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Wrapper class for <see cref="SerializableError"/> to enable it to be serialized by the xml formatters.
    /// </summary>
    [XmlRoot("Error")]
    public sealed class SerializableErrorWrapper : IXmlSerializable
    {
        // Note: XmlSerializer requires to have default constructor
        public SerializableErrorWrapper()
        {
            SerializableError = new SerializableError();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableErrorWrapper"/> class.
        /// </summary>
        /// <param name="error">The <see cref="SerializableError"/> object that needs to be wrapped.</param>
        public SerializableErrorWrapper([NotNull] SerializableError error)
        {
            SerializableError = error;
        }

        /// <summary>
        /// Gets the wrapped object which is serialized/deserialized into XML
        /// representation.
        /// </summary>
        public SerializableError SerializableError { get; }

        /// <inheritdoc />
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates a <see cref="SerializableError"/> object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/> stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var key = XmlConvert.DecodeName(reader.LocalName);
                var value = reader.ReadInnerXml();

                SerializableError.Add(key, value);
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// Converts the wrapped <see cref="SerializableError"/> object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            foreach (var keyValuePair in SerializableError)
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;
                writer.WriteStartElement(XmlConvert.EncodeLocalName(key));
                if (value != null)
                {
                    writer.WriteValue(value);
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Gets the 
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="deserializedObject"></param>
        /// <returns></returns>
        public static object UnwrapSerializableErrorObject([NotNull] Type modelType, object deserializedObject)
        {
            // Since we expect users to typically bind with SerializableError type,
            // we should try to unwrap and get the actual SerializableError.
            if (modelType == typeof(SerializableError))
            {
                var serializableErrorWrapper = deserializedObject as SerializableErrorWrapper;
                if (serializableErrorWrapper != null)
                {
                    deserializedObject = serializableErrorWrapper.SerializableError;
                }
            }

            return deserializedObject;
        }

        /// <summary>
        /// Checks if an object is an instance of type <see cref="SerializableError"/> and if yes,
        /// gets and returns the wrapped <see cref="SerializableErrorWrapper"/> object in it.
        /// </summary>
        /// <param name="obj">An </param>
        /// <returns></returns>
        public static object WrapSerializableErrorObject(object obj)
        {
            var serializableError = obj as SerializableError;
            if (serializableError == null)
            {
                return obj;
            }

            return new SerializableErrorWrapper(serializableError);
        }

        /// <summary>
        /// Checks if the given type is of type <see cref="SerializableError"/> and if yes, returns
        /// the wrapper type <see cref="SerializableErrorWrapper"/>.
        /// </summary>
        /// <param name="type">The type to be checked</param>
        /// <returns><see cref="SerializableErrorWrapper"/> type, else the original type.</returns>
        public static Type CreateSerializableType([NotNull] Type type)
        {
            // Since the type "SerializableError" is not compatible
            // with the xml serializers, we create a compatible wrapper type for serialization.
            if (type == typeof(SerializableError))
            {
                type = typeof(SerializableErrorWrapper);
            }

            return type;
        }
    }
}