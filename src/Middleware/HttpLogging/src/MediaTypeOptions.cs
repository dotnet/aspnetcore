using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Options for HttpLogging to configure which encoding to use for each media type.
    /// </summary>
    public class MediaTypeOptions
    {
        /// <summary>
        /// Default MediaTypes. Defaults to UTF8 for application/json, application/*+json,
        /// application/xml, application/*+xml, and text/*.
        /// </summary>
        public static MediaTypeOptions Default = BuildDefaultMediaTypeOptions();

        private List<MediaTypeState> _mediaTypeStates = new List<MediaTypeState>();

        internal MediaTypeOptions()
        {
        }

        internal List<MediaTypeState> MediaTypeStates => _mediaTypeStates;

        internal static MediaTypeOptions BuildDefaultMediaTypeOptions()
        {
            var options = new MediaTypeOptions();
            options.AddText("application/json", Encoding.UTF8);
            options.AddText("application/*+json", Encoding.UTF8);
            options.AddText("application/xml", Encoding.UTF8);
            options.AddText("application/*+xml", Encoding.UTF8);
            options.AddText("text/*", Encoding.UTF8);

            return options;
        }

        /// <summary>
        /// Adds a <see cref="MediaTypeHeaderValue"/> to be used for logging as text.
        /// </summary>
        /// <param name="mediaType">The MediaType to add.</param>
        public void AddText(MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            _mediaTypeStates.Add(new MediaTypeState(mediaType) { Encoding = mediaType.Encoding ?? Encoding.UTF8 });
        }

        /// <summary>
        /// Adds a contentType to be used for logging as text.
        /// </summary>
        /// <remarks>
        /// If charset is not specified in the contentType, the encoding will default to UTF-8.
        /// </remarks>
        /// <param name="contentType">The content type to add.</param>
        public void AddText(string contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            AddText(new MediaTypeHeaderValue(contentType));
        }

        /// <summary>
        /// Adds a contentType to be used for logging as text.
        /// </summary>
        /// <param name="contentType">The content type to add.</param>
        /// <param name="encoding">The encoding to use.</param>
        public void AddText(string contentType, Encoding encoding)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var mediaType = new MediaTypeHeaderValue(contentType);
            mediaType.Encoding = encoding;
            AddText(mediaType);
        }

        /// <summary>
        /// Adds a <see cref="MediaTypeHeaderValue"/> to be used for logging as binary.
        /// </summary>
        /// <param name="mediaType">The MediaType to add.</param>
        public void AddBinary(MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            _mediaTypeStates.Add(new MediaTypeState(mediaType) { IsBinary = true });
        }

        /// <summary>
        /// Adds a content to be used for logging as text.
        /// </summary>
        /// <param name="contentType">The content type to add.</param>
        public void AddBinary(string contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            AddBinary(new MediaTypeHeaderValue(contentType));
        }

        /// <summary>
        /// Clears all MediaTypes.
        /// </summary>
        public void Clear()
        {
            _mediaTypeStates.Clear();
        }

        internal class MediaTypeState
        {
            public MediaTypeState(MediaTypeHeaderValue mediaTypeHeaderValue)
            {
                MediaTypeHeaderValue = mediaTypeHeaderValue;
            }

            public MediaTypeHeaderValue MediaTypeHeaderValue { get; }
            public Encoding? Encoding { get; set; }
            public bool IsBinary { get; set; }
        }
    }
}
