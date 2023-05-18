using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;

#nullable enable

var builder = WebApplication.CreateSlimBuilder(args);
_ = builder.Build();

if (GetType("System.Security.Cryptography", "System.Security.Cryptography.X509Certificates.X509Certificate") is not null) {
    return 1;
}

return 100; // Success

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Justification = "Returning null when the type is unreferenced is desirable")]
static Type? GetType(string assemblyName, string typeName) {
    var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == assemblyName);
    return assembly?.GetType(typeName);
}
