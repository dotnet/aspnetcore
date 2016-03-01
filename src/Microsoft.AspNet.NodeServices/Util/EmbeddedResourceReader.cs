using System;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNet.NodeServices {
    public static class EmbeddedResourceReader {
        public static string Read(Type assemblyContainingType, string path) {
            var asm = assemblyContainingType.GetTypeInfo().Assembly;
            var embeddedResourceName = asm.GetName().Name + path.Replace("/", ".");

            using (var stream = asm.GetManifestResourceStream(embeddedResourceName))
            using (var sr = new StreamReader(stream)) {
                return sr.ReadToEnd();
            }
        }
    }
}
