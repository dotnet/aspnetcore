using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class AssertHelpers
    {
        internal static void IsAttachToDocumentMessage(string message, int componentId, string selector)
        {
            var payload = message.Split(":");
            Assert.Equal(3, payload.Length);
            Assert.Equal("AttachToDocument", payload[0]);
            Assert.Equal(componentId, int.Parse(payload[1], CultureInfo.InvariantCulture));
            Assert.Equal(selector, payload[2]);
        }

        internal static RenderBatch IsRenderBatch(string message)
        {
            var payload = message.Split(":");
            Assert.Equal(2, payload.Length);
            Assert.Equal("RenderBatch", payload[0]);

            // At least validate we can base64 decode it
            var _ = Convert.FromBase64String(payload[1]);
            // TODO: Produce the render batch if we want to grab info from it.
            return default;
        }
    }
}
