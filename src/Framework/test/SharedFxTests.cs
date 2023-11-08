// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore;

public class SharedFxTests
{
    private readonly string _expectedTfm;
    private readonly string _expectedRid;
    private readonly string _sharedFxRoot;
    private readonly ITestOutputHelper _output;

    public SharedFxTests(ITestOutputHelper output)
    {
        _output = output;
        _expectedTfm = TestData.GetDefaultNetCoreTargetFramework();
        _expectedRid = TestData.GetSharedFxRuntimeIdentifier();
        _sharedFxRoot = Path.Combine(
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            "shared",
            "Microsoft.AspNetCore.App",
            TestData.GetSharedFxVersion());
    }

    [Fact]
    public void SharedFrameworkContainsListedAssemblies()
    {
        var actualAssemblies = Directory.GetFiles(_sharedFxRoot, "*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet();

        _output.WriteLine("==== actual assemblies ====");
        _output.WriteLine(string.Join('\n', actualAssemblies.OrderBy(i => i)));
        _output.WriteLine("==== expected assemblies ====");
        _output.WriteLine(string.Join('\n', TestData.ListedSharedFxAssemblies.OrderBy(i => i)));

        var missing = TestData.ListedSharedFxAssemblies.Except(actualAssemblies);
        var unexpected = actualAssemblies.Except(TestData.ListedSharedFxAssemblies);

        _output.WriteLine("==== missing assemblies from the framework ====");
        _output.WriteLine(string.Join('\n', missing));
        _output.WriteLine("==== unexpected assemblies in the framework ====");
        _output.WriteLine(string.Join('\n', unexpected));

        Assert.Empty(missing);
        Assert.Empty(unexpected);
    }

    [Fact]
    public void SharedFrameworkContainsExpectedFiles()
    {
        var actualAssemblies = Directory.GetFiles(_sharedFxRoot, "*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet();
        var expectedAssemblies = TestData.GetSharedFxDependencies()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        _output.WriteLine("==== actual assemblies ====");
        _output.WriteLine(string.Join('\n', actualAssemblies.OrderBy(i => i)));
        _output.WriteLine("==== expected assemblies ====");
        _output.WriteLine(string.Join('\n', expectedAssemblies.OrderBy(i => i)));

        var missing = expectedAssemblies.Except(actualAssemblies);
        var unexpected = actualAssemblies.Except(expectedAssemblies);

        _output.WriteLine("==== missing assemblies from the framework ====");
        _output.WriteLine(string.Join('\n', missing));
        _output.WriteLine("==== unexpected assemblies in the framework ====");
        _output.WriteLine(string.Join('\n', unexpected));

        Assert.Empty(missing);
        Assert.Empty(unexpected);
    }

    [Fact]
    public void SharedFrameworkContainsValidRuntimeConfigFile()
    {
        var runtimeConfigFilePath = Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.runtimeconfig.json");

        AssertEx.FileExists(runtimeConfigFilePath);
        AssertEx.FileDoesNotExists(Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.runtimeconfig.dev.json"));

        var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

        Assert.Equal("Microsoft.NETCore.App", (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
        Assert.Equal(_expectedTfm, (string)runtimeConfig["runtimeOptions"]["tfm"]);
        Assert.Equal("LatestPatch", (string)runtimeConfig["runtimeOptions"]["rollForward"]);

        Assert.Equal(TestData.GetMicrosoftNETCoreAppPackageVersion(), (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
    }

    [Fact]
    public void SharedFrameworkContainsValidDepsJson()
    {
        var depsFilePath = Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.deps.json");

        var target = $".NETCoreApp,Version=v{_expectedTfm.Substring(3)}/{_expectedRid}";
        var ridPackageId = $"Microsoft.AspNetCore.App.Runtime.{_expectedRid}";
        var libraryId = $"{ridPackageId}/{TestData.GetSharedFxVersion()}";

        AssertEx.FileExists(depsFilePath);

        var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

        Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
        Assert.NotNull(depsFile["compilationOptions"]);
        Assert.Empty(depsFile["compilationOptions"]);
        Assert.All(depsFile["libraries"], item =>
        {
            var prop = Assert.IsType<JProperty>(item);
            var lib = Assert.IsType<JObject>(prop.Value);
            Assert.Equal("package", lib["type"].Value<string>());
            Assert.Empty(lib["sha512"].Value<string>());
        });

        Assert.NotNull(depsFile["libraries"][libraryId]);
        Assert.Single(depsFile["libraries"].Values());

        var targetLibraries = depsFile["targets"][target];
        Assert.Single(targetLibraries.Values());
        var runtimeLibrary = targetLibraries[libraryId];
        Assert.Null(runtimeLibrary["dependencies"]);
        Assert.All(runtimeLibrary["runtime"], item =>
        {
            var obj = Assert.IsType<JProperty>(item);
            var assemblyVersion = obj.Value["assemblyVersion"].Value<string>();
            Assert.NotEmpty(assemblyVersion);
            Assert.True(Version.TryParse(assemblyVersion, out _), $"{assemblyVersion} should deserialize to System.Version");
            var fileVersion = obj.Value["fileVersion"].Value<string>();
            Assert.NotEmpty(fileVersion);
            Assert.True(Version.TryParse(fileVersion, out _), $"{fileVersion} should deserialize to System.Version");
        });

        if (_expectedRid.StartsWith("win", StringComparison.Ordinal) && !_expectedRid.Contains("arm"))
        {
            Assert.All(runtimeLibrary["native"], item =>
            {
                var obj = Assert.IsType<JProperty>(item);
                var fileVersion = obj.Value["fileVersion"].Value<string>();
                Assert.NotEmpty(fileVersion);
                Assert.True(Version.TryParse(fileVersion, out _), $"{fileVersion} should deserialize to System.Version");
            });
        }
        else
        {
            Assert.Null(runtimeLibrary["native"]);
        }
    }

    [Fact]
    public void SharedFrameworkAssembliesHaveExpectedAssemblyVersions()
    {
        // Assemblies from this repo and dotnet/runtime don't always have identical assembly versions.
        var repoAssemblies = TestData.GetSharedFrameworkBinariesFromRepo()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var versionStringWithoutPrereleaseTag = TestData.GetMicrosoftNETCoreAppPackageVersion().Split('-', 2)[0];
        var version = Version.Parse(versionStringWithoutPrereleaseTag);
        var aspnetcoreVersionString = TestData.GetSharedFxVersion().Split('-', 2)[0];
        var aspnetcoreVersion = Version.Parse(aspnetcoreVersionString);

        var dlls = Directory.GetFiles(_sharedFxRoot, "*.dll", SearchOption.AllDirectories);
        Assert.NotEmpty(dlls);

        Assert.All(dlls, path =>
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, "aspnetcorev2_inprocess", StringComparison.Ordinal))
            {
                // Skip our native assembly.
                return;
            }

            var expectedVersion = repoAssemblies.Contains(name) ? aspnetcoreVersion : version;

            using var fileStream = File.OpenRead(path);
            using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
            var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
            var assemblyDefinition = reader.GetAssemblyDefinition();

            // Assembly versions should all match Major.Minor.0.0
            if (repoAssemblies.Contains(name))
            {
                // We always align major.minor in assemblies and packages.
                Assert.Equal(expectedVersion.Major, assemblyDefinition.Version.Major);
            }
            else
            {
                // ... but dotnet/runtime has a window between package version and (then) assembly version updates.
                Assert.True(expectedVersion.Major == assemblyDefinition.Version.Major ||
                    expectedVersion.Major - 1 == assemblyDefinition.Version.Major,
                    $"Unexpected Major assembly version '{assemblyDefinition.Version.Major}' is neither " +
                        $"{expectedVersion.Major - 1}' nor '{expectedVersion.Major}'.");
            }

            Assert.Equal(expectedVersion.Minor, assemblyDefinition.Version.Minor);
            Assert.Equal(0, assemblyDefinition.Version.Build);
            Assert.Equal(0, assemblyDefinition.Version.Revision);
        });
    }

    // ASP.NET Core shared Fx assemblies should reference only ASP.NET Core assemblies with Revsion == 0.
    [Fact]
    public void SharedFrameworkAssemblyReferencesHaveExpectedAssemblyVersions()
    {
        // Only test managed assemblies from dotnet/aspnetcore.
        var repoAssemblies = TestData.GetSharedFrameworkBinariesFromRepo()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        IEnumerable<string> dlls = Directory.GetFiles(_sharedFxRoot, "*.dll", SearchOption.AllDirectories);
        Assert.NotEmpty(dlls);

        Assert.All(dlls, path =>
        {
            // Unlike dotnet/aspnetcore, dotnet/runtime varies the assembly version while in servicing.
            // dotnet/aspnetcore assemblies build against RTM targeting pack from dotnet/runtime.
            if (!repoAssemblies.Contains(Path.GetFileNameWithoutExtension(path)))
            {
                return;
            }

            using var fileStream = File.OpenRead(path);
            using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
            var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);

            Assert.All(reader.AssemblyReferences, handle =>
            {
                var reference = reader.GetAssemblyReference(handle);
                Assert.Equal(0, reference.Version.Build);
                Assert.Equal(0, reference.Version.Revision);
            });
        });
    }

    [Fact]
    public void ItContainsVersionFile()
    {
        var versionFile = Path.Combine(_sharedFxRoot, ".version");
        AssertEx.FileExists(versionFile);
        var lines = File.ReadAllLines(versionFile);
        Assert.Equal(2, lines.Length);
        Assert.Equal(TestData.GetRepositoryCommit(), lines[0]);
        Assert.Equal(TestData.GetSharedFxVersion(), lines[1]);
    }

    [Fact]
    public void RuntimeListContainsCorrectEntries()
    {
        var expectedAssemblies = TestData.GetSharedFxDependencies()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var runtimeListPath = "RuntimeList.xml";
        AssertEx.FileExists(runtimeListPath);

        var runtimeListDoc = XDocument.Load(runtimeListPath);
        var runtimeListEntries = runtimeListDoc.Root.Descendants();

        _output.WriteLine("==== file contents ====");
        _output.WriteLine(string.Join('\n', runtimeListEntries.Select(i => i.Attribute("Path").Value).OrderBy(i => i)));
        _output.WriteLine("==== expected assemblies ====");
        _output.WriteLine(string.Join('\n', expectedAssemblies.OrderBy(i => i)));

        var actualAssemblies = runtimeListEntries
           .Select(i =>
           {
               var filePath = i.Attribute("Path").Value;
               var fileParts = filePath.Split('/');
               var fileName = fileParts[fileParts.Length - 1];
               return fileName.EndsWith(".dll", StringComparison.Ordinal)
                   ? fileName.Substring(0, fileName.Length - 4)
                   : fileName;
           })
           .ToHashSet();

        var missing = expectedAssemblies.Except(actualAssemblies);
        var unexpected = actualAssemblies.Except(expectedAssemblies);

        _output.WriteLine("==== missing assemblies from the runtime list ====");
        _output.WriteLine(string.Join('\n', missing));
        _output.WriteLine("==== unexpected assemblies in the runtime list ====");
        _output.WriteLine(string.Join('\n', unexpected));

        Assert.Empty(missing);
        Assert.Empty(unexpected);

        Assert.All(runtimeListEntries, i =>
        {
            var assemblyType = i.Attribute("Type").Value;
            var assemblyPath = i.Attribute("Path").Value;
            var fileVersion = i.Attribute("FileVersion").Value;

            if (assemblyType.Equals("Managed"))
            {
                var assemblyVersion = i.Attribute("AssemblyVersion").Value;
                Assert.True(Version.TryParse(assemblyVersion, out _), $"{assemblyPath} has assembly version {assemblyVersion}. Assembly version must be convertable to System.Version");
            }

            Assert.True(Version.TryParse(fileVersion, out _), $"{assemblyPath} has file version {fileVersion}. File version must be convertable to System.Version");
        });
    }

    [Fact]
    public void RuntimeListContainsCorrectPaths()
    {
        var runtimeListPath = "RuntimeList.xml";
        AssertEx.FileExists(runtimeListPath);

        var runtimeListDoc = XDocument.Load(runtimeListPath);
        var runtimeListEntries = runtimeListDoc.Root.Descendants();

        var packageFolder = SkipOnHelixAttribute.OnHelix() ?
            Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") :
            TestData.GetPackagesFolder();
        var sharedFxPath = Directory.GetFiles(packageFolder, "Microsoft.AspNetCore.App.Runtime.*-*." + TestData.GetSharedFxVersion() + ".nupkg").FirstOrDefault();
        Assert.NotNull(sharedFxPath);

        ZipArchive archive = ZipFile.OpenRead(sharedFxPath);

        var actualPaths = archive.Entries
            .Where(i => i.FullName.EndsWith(".dll", StringComparison.Ordinal))
            .Select(i => i.FullName).ToHashSet();

        var expectedPaths = runtimeListEntries.Select(i => i.Attribute("Path").Value).ToHashSet();

        _output.WriteLine("==== package contents ====");
        _output.WriteLine(string.Join('\n', actualPaths.OrderBy(i => i)));
        _output.WriteLine("==== expected assemblies ====");
        _output.WriteLine(string.Join('\n', expectedPaths.OrderBy(i => i)));

        var missing = expectedPaths.Except(actualPaths);
        var unexpected = actualPaths.Except(expectedPaths);

        _output.WriteLine("==== missing assemblies from the runtime list ====");
        _output.WriteLine(string.Join('\n', missing));
        _output.WriteLine("==== unexpected assemblies in the runtime list ====");
        _output.WriteLine(string.Join('\n', unexpected));

        Assert.Empty(missing);
        Assert.Empty(unexpected);
    }
}
