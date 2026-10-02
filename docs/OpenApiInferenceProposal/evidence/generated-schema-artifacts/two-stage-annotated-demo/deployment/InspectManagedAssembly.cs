#:property PublishAot=false

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet InspectManagedAssembly.cs -- <assembly>");
    return 2;
}

using var stream = File.OpenRead(args[0]);
using var peReader = new PEReader(stream);

if (!peReader.HasMetadata)
{
    Console.Error.WriteLine("not a managed assembly");
    return 3;
}

var reader = peReader.GetMetadataReader();

Console.WriteLine("Assembly references:");
foreach (var handle in reader.AssemblyReferences)
{
    var reference = reader.GetAssemblyReference(handle);
    Console.WriteLine($"  {reader.GetString(reference.Name)}");
}

Console.WriteLine("Forbidden metadata matches:");
var matches = new SortedSet<string>(StringComparer.Ordinal);
foreach (var handle in reader.TypeReferences)
{
    var reference = reader.GetTypeReference(handle);
    AddIfForbidden($"{reader.GetString(reference.Namespace)}.{reader.GetString(reference.Name)}");
}

foreach (var handle in reader.TypeDefinitions)
{
    var definition = reader.GetTypeDefinition(handle);
    AddIfForbidden($"{reader.GetString(definition.Namespace)}.{reader.GetString(definition.Name)}");
}

foreach (var handle in reader.MemberReferences)
{
    AddIfForbidden(reader.GetString(reader.GetMemberReference(handle).Name));
}

foreach (var handle in reader.MethodDefinitions)
{
    AddIfForbidden(reader.GetString(reader.GetMethodDefinition(handle).Name));
}

if (matches.Count == 0)
{
    Console.WriteLine("  none");
}
else
{
    foreach (var match in matches)
    {
        Console.WriteLine($"  {match}");
    }
}

return 0;

void AddIfForbidden(string value)
{
    if (value.Contains("JsonSchemaBuilder", StringComparison.Ordinal)
        || value.Contains("GeneratedJsonSchemas", StringComparison.Ordinal)
        || value.Contains("JsonSchema.Net", StringComparison.Ordinal))
    {
        matches.Add(value);
    }
}
