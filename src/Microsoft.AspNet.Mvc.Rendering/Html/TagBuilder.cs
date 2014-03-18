using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class TagBuilder
    {
        private string _innerHtml;

        public TagBuilder(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                throw new ArgumentException(Resources.FormatArgumentNullOrEmpty("tagName"));
            }

            TagName = tagName;
            Attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        }

        public IDictionary<string, string> Attributes { get; private set; }

        public string InnerHtml
        {
            get { return _innerHtml ?? string.Empty; }
            set { _innerHtml = value; }
        }

        public string TagName { get; private set; }

        public void AddCssClass(string value)
        {
            string currentValue;

            if (Attributes.TryGetValue("class", out currentValue))
            {
                Attributes["class"] = value + " " + currentValue;
            }
            else
            {
                Attributes["class"] = value;
            }
        }

        public static string CreateSanitizedId(string originalId, [NotNull] string invalidCharReplacement)
        {
            if (string.IsNullOrEmpty(originalId))
            {
                return string.Empty;
            }

            var firstChar = originalId[0];

            var sb = new StringBuilder(originalId.Length);
            sb.Append(firstChar);

            for (var i = 1; i < originalId.Length; i++)
            {
                var thisChar = originalId[i];
                if (!char.IsWhiteSpace(thisChar))
                {
                    sb.Append(thisChar);
                }
                else
                {
                    sb.Append(invalidCharReplacement);
                }
            }

            return sb.ToString();
        }

        public void GenerateId(string name, [NotNull] string idAttributeDotReplacement)
        {
            if (!Attributes.ContainsKey("id"))
            {
                var sanitizedId = CreateSanitizedId(name, idAttributeDotReplacement);
                if (!string.IsNullOrEmpty(sanitizedId))
                {
                    Attributes["id"] = sanitizedId;
                }
            }
        }

        private void AppendAttributes(StringBuilder sb)
        {
            foreach (var attribute in Attributes)
            {
                var key = attribute.Key;
                if (string.Equals(key, "id", StringComparison.Ordinal) && string.IsNullOrEmpty(attribute.Value))
                {
                    continue;
                }

                var value = WebUtility.HtmlEncode(attribute.Value);
                sb.Append(' ')
                    .Append(key)
                    .Append("=\"")
                    .Append(value)
                    .Append('"');
            }
        }

        public void MergeAttribute(string key, string value)
        {
            MergeAttribute(key, value, replaceExisting: false);
        }

        public void MergeAttribute(string key, string value, bool replaceExisting)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(Resources.FormatArgumentNullOrEmpty("key"));
            }

            if (replaceExisting || !Attributes.ContainsKey(key))
            {
                Attributes[key] = value;
            }
        }

        public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes)
        {
            MergeAttributes(attributes, replaceExisting: false);
        }

        public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting)
        {
            if (attributes != null)
            {
                foreach (var entry in attributes)
                {
                    var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                    var value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);
                    MergeAttribute(key, value, replaceExisting);
                }
            }
        }

        public void SetInnerText(string innerText)
        {
            InnerHtml = WebUtility.HtmlEncode(innerText);
        }

        public HtmlString ToHtmlString(TagRenderMode renderMode)
        {
            return new HtmlString(ToString(renderMode));
        }

        public override string ToString()
        {
            return ToString(TagRenderMode.Normal);
        }

        public string ToString(TagRenderMode renderMode)
        {
            var sb = new StringBuilder();
            switch (renderMode)
            {
                case TagRenderMode.StartTag:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>');
                    break;
                case TagRenderMode.EndTag:
                    sb.Append("</")
                        .Append(TagName)
                        .Append('>');
                    break;
                case TagRenderMode.SelfClosing:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append(" />");
                    break;
                default:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>')
                        .Append(InnerHtml)
                        .Append("</")
                        .Append(TagName)
                        .Append('>');
                    break;
            }

            return sb.ToString();
        }
    }
}
