using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CachedDataAnnotationsMetadataAttributes
    {
        public CachedDataAnnotationsMetadataAttributes(IEnumerable<Attribute> attributes)
        {
            Display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            DisplayName = attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
            DisplayFormat = attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
            Editable = attributes.OfType<EditableAttribute>().FirstOrDefault();
            ReadOnly = attributes.OfType<ReadOnlyAttribute>().FirstOrDefault();
        }

        public DisplayAttribute Display { get; protected set; }

        public DisplayNameAttribute DisplayName { get; protected set; }

        public DisplayFormatAttribute DisplayFormat { get; protected set; }

        public EditableAttribute Editable { get; protected set; }

        public ReadOnlyAttribute ReadOnly { get; protected set; }
    }
}
