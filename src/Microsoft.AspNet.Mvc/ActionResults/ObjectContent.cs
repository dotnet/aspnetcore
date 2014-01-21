// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Contains a value as well as an associated <see cref="MediaTypeFormatter"/> that will be
    /// used to serialize the value when writing this content.
    /// </summary>
    public class ObjectContent : HttpContent
    {
        private object _value;
        private readonly MediaTypeFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="formatter">The formatter to use when serializing the value.</param>
        public ObjectContent(Type type, object value, MediaTypeFormatter formatter)
            : this(type, value, formatter, (MediaTypeHeaderValue)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="formatter">The formatter to use when serializing the value.</param>
        /// <param name="mediaType">The authoritative value of the content's Content-Type header. Can be <c>null</c> in which case the
        /// <paramref name="formatter">formatter's</paramref> default content type will be used.</param>
        public ObjectContent(Type type, object value, MediaTypeFormatter formatter, string mediaType)
            : this(type, value, formatter, BuildHeaderValue(mediaType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="formatter">The formatter to use when serializing the value.</param>
        /// <param name="mediaType">The authoritative value of the content's Content-Type header. Can be <c>null</c> in which case the
        /// <paramref name="formatter">formatter's</paramref> default content type will be used.</param>
        public ObjectContent(Type type, object value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }

            if (!formatter.CanWriteType(type))
            {
                throw new ArgumentNullException(formatter.GetType().FullName + " cannot write " + type.Name);
            }

            _formatter = formatter;
            ObjectType = type;
            
            VerifyAndSetObject(value);
            _formatter.SetDefaultContentHeaders(type, Headers, mediaType);
        }

        /// <summary>
        /// Gets the type of object managed by this <see cref="ObjectContent"/> instance.
        /// </summary>
        public Type ObjectType { get; private set; }

        /// <summary>
        /// The <see cref="MediaTypeFormatter">formatter</see> associated with this content instance.
        /// </summary>
        public MediaTypeFormatter Formatter
        {
            get { return _formatter; }
        }

        /// <summary>
        /// Gets or sets the value of the current <see cref="ObjectContent"/>.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        internal static MediaTypeHeaderValue BuildHeaderValue(string mediaType)
        {
            return mediaType != null ? new MediaTypeHeaderValue(mediaType) : null;
        }

        /// <summary>
        /// Asynchronously serializes the object's content to the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
        {
            return _formatter.WriteToStreamAsync(ObjectType, Value, stream, this, context);
        }

        /// <summary>
        /// Computes the length of the stream if possible.
        /// </summary>
        /// <param name="length">The computed length of the stream.</param>
        /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        private static bool IsTypeNullable(Type type)
        {
            return !type.IsValueType() ||
                   (type.IsGenericType() &&
                    type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private void VerifyAndSetObject(object value)
        {
            Contract.Assert(ObjectType != null, "Type cannot be null");

            if (value == null)
            {
                // Null may not be assigned to value types (unless Nullable<T>)
                if (!IsTypeNullable(ObjectType))
                {
                    throw new InvalidOperationException("CannotUseNullValueType " + typeof(ObjectContent).Name + " " + ObjectType.Name);
                }
            }
            else
            {
                // Non-null objects must be a type assignable to Type
                Type objectType = value.GetType();
                if (!ObjectType.IsAssignableFrom(objectType))
                {
                    throw new ArgumentException("value Resources.ObjectAndTypeDisagree, objectType.Name, ObjectType.Name");
                }
            }

            _value = value;
        }
    }
}
