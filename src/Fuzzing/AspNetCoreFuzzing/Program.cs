// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SharpFuzz;

namespace AspNetCoreFuzzing;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IFuzzer[] fuzzers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Contains(typeof(IFuzzer)))
            .Select(t => (IFuzzer)Activator.CreateInstance(t)!)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void PrintUsage()
        {
            Console.Error.WriteLine(
                $"""
                Usage:
                    AspNetCoreFuzzing <Fuzzer name> [input file/directory]
                    AspNetCoreFuzzing prepare-onefuzz <output directory>

                Fuzzers available: {string.Join(", ", fuzzers.Select(t => t.Name))}
                """);
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        if (args[0].Equals("prepare-onefuzz", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return;
            }

            string publishDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Environment.CurrentDirectory;

            await PrepareOneFuzzDeploymentAsync(fuzzers, publishDirectory, args[1]).ConfigureAwait(false);
            return;
        }

        IFuzzer? fuzzer = fuzzers
            .Where(f => f.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (fuzzer is null)
        {
            Console.Error.WriteLine($"Fuzzer '{args[0]}' not found. Available: {string.Join(", ", fuzzers.Select(t => t.Name))}");
            return;
        }

        if (args.ElementAtOrDefault(1) == "--get-instrumented-assemblies")
        {
            foreach (string assembly in GetInstrumentationTargets(fuzzer))
            {
                Console.WriteLine(assembly);
            }
            return;
        }

        RunFuzzer(fuzzer, inputFiles: args.Length > 1 ? args[1] : null);
    }

    private static unsafe void RunFuzzer(IFuzzer fuzzer, string? inputFiles)
    {
        if (!string.IsNullOrEmpty(inputFiles))
        {
            string[] files = Directory.Exists(inputFiles)
                ? Directory.GetFiles(inputFiles)
                : [inputFiles];

            foreach (string inputFile in files)
            {
                fuzzer.FuzzTarget(File.ReadAllBytes(inputFile));
            }

            return;
        }

        Fuzzer.LibFuzzer.Run(bytes =>
        {
            // Some fuzzers assume that the input is at least 2-byte aligned.
            ArgumentOutOfRangeException.ThrowIfNotEqual((nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)) % 8, 0U);

            fuzzer.FuzzTarget(bytes);
        });
    }

    private static async Task PrepareOneFuzzDeploymentAsync(IFuzzer[] fuzzers, string publishDirectory, string outputDirectory)
    {
        string[] dictionaries = Directory.Exists(Path.Combine(publishDirectory, "Dictionaries"))
            ? Directory.GetFiles(Path.Combine(publishDirectory, "Dictionaries"))
                .Select(f => Path.GetFileName(f)!)
                .ToArray()
            : [];

        if (dictionaries.FirstOrDefault(dict => !fuzzers.Any(f => f.Dictionary == dict)) is { } unusedDictionary)
        {
            throw new Exception($"Dictionary '{unusedDictionary}' is not referenced by any fuzzer.");
        }

        string[] corpora = Directory.Exists(Path.Combine(publishDirectory, "Corpora"))
            ? Directory.GetDirectories(Path.Combine(publishDirectory, "Corpora"))
                .Select(d => Path.GetFileName(d)!)
                .ToArray()
            : [];

        if (corpora.FirstOrDefault(corpus => !fuzzers.Any(f => f.Corpus == corpus)) is { } unusedCorpus)
        {
            throw new Exception($"Corpus '{unusedCorpus}' is not referenced by any fuzzer.");
        }

        Directory.CreateDirectory(outputDirectory);

        await DownloadArtifactAsync(
            Path.Combine(publishDirectory, "libfuzzer-dotnet.exe"),
            "https://github.com/Metalnem/libfuzzer-dotnet/releases/download/v2025.05.02.0904/libfuzzer-dotnet-windows.exe",
            "4da2a77d06229a43040f9841bc632a881389a0b8fdcc2d60c8d0b547ccbedee63e7b0a7eca8eeffdba1243d85bdcec3cfe763237650c2f46a1327f8ee401d9a2").ConfigureAwait(false);

        Console.WriteLine("Preparing fuzzers ...");

        ConcurrentQueue<string> exceptions = new();

        Parallel.ForEach(fuzzers, fuzzer =>
        {
            try
            {
                PrepareFuzzer(fuzzer);
            }
            catch (Exception ex)
            {
                exceptions.Enqueue($"Failed to prepare {fuzzer.Name}: {ex.Message}");
            }
        });

        if (!exceptions.IsEmpty)
        {
            Console.WriteLine(string.Join('\n', exceptions));
            throw new Exception($"Failed to prepare {exceptions.Count} fuzzers.");
        }

        void PrepareFuzzer(IFuzzer fuzzer)
        {
            string fuzzerDirectory = Path.Combine(outputDirectory, fuzzer.Name);
            Directory.CreateDirectory(fuzzerDirectory);

            // NOTE: The expected fuzzer directory structure is currently flat.
            // If we ever need to support subdirectories, OneFuzzConfig.json must also be updated to use PreservePathsJobDependencies.
            foreach (string file in Directory.GetFiles(publishDirectory))
            {
                File.Copy(file, Path.Combine(fuzzerDirectory, Path.GetFileName(file)), overwrite: true);
            }

            if (fuzzer.Dictionary is string dict)
            {
                if (!dictionaries.Contains(dict, StringComparer.Ordinal))
                {
                    throw new Exception($"Fuzzer '{fuzzer.Name}' is referencing a dictionary '{fuzzer.Dictionary}' that does not exist in the publish directory.");
                }

                File.Copy(Path.Combine(publishDirectory, "Dictionaries", dict), Path.Combine(fuzzerDirectory, "dictionary"), overwrite: true);
            }

            if (fuzzer.Corpus is string corpus)
            {
                if (!corpora.Contains(corpus, StringComparer.Ordinal))
                {
                    throw new Exception($"Fuzzer '{fuzzer.Name}' is referencing a corpus '{fuzzer.Corpus}' that does not exist in the publish directory.");
                }

                Directory.CreateDirectory(Path.Combine(fuzzerDirectory, "corpus"));
                foreach (string file in Directory.EnumerateFiles(Path.Combine(publishDirectory, "Corpora", corpus), "*", SearchOption.TopDirectoryOnly))
                {
                    File.Copy(file, Path.Combine(fuzzerDirectory, "corpus", Path.GetFileName(file)), overwrite: true);
                }
            }

            InstrumentAssemblies(fuzzer, fuzzerDirectory);

            File.WriteAllText(Path.Combine(fuzzerDirectory, "OneFuzzConfig.json"), GenerateOneFuzzConfigJson(fuzzer));
            File.WriteAllText(Path.Combine(fuzzerDirectory, "local-run.bat"), GenerateLocalRunHelperScript(fuzzer));
        }

        WorkaroundOneFuzzTaskNotAcceptingMultipleJobs(fuzzers);
    }

    private static IEnumerable<string> GetInstrumentationTargets(IFuzzer fuzzer)
    {
        if (fuzzer.TargetAssemblies.Length == 0)
        {
            throw new Exception($"Specify at least one target in {nameof(IFuzzer.TargetAssemblies)}.");
        }

        foreach (string assembly in fuzzer.TargetAssemblies)
        {
            string path = assembly;
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                path += ".dll";
            }

            yield return path;
        }
    }

    private static void InstrumentAssemblies(IFuzzer fuzzer, string fuzzerDirectory)
    {
        foreach (string assembly in GetInstrumentationTargets(fuzzer))
        {
            string path = Path.Combine(fuzzerDirectory, assembly);
            if (!File.Exists(path))
            {
                throw new Exception($"Assembly {path} not found. Make sure to run the tool from the publish directory.");
            }

            byte[] current = File.ReadAllBytes(path);
            string previousOriginal = $"{path}.original";
            string previousInstrumented = $"{path}.instrumented";

            if (File.Exists(previousOriginal) &&
                File.Exists(previousInstrumented) &&
                File.ReadAllBytes(previousOriginal).AsSpan().SequenceEqual(current))
            {
                // The assembly hasn't changed since the previous invocation of SharpFuzz.
                File.Copy(previousInstrumented, path, overwrite: true);
                continue;
            }

            File.Delete(previousOriginal);
            File.Delete(previousInstrumented);

            var startInfo = new ProcessStartInfo
            {
                FileName = "sharpfuzz",
                Arguments = path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://github.com/Metalnem/sharpfuzz/blob/9e44048d8821da942d00c2c125bb59d039d55673/src/SharpFuzz/Options.cs#L37-L41
                startInfo.EnvironmentVariables.Add("SHARPFUZZ_INSTRUMENT_MIXED_MODE_ASSEMBLIES", "1");
            }

            // The sharpfuzz global tool may target a different runtime version than the repo's local SDK.
            // Clear DOTNET_ROOT so it can find system-installed runtimes instead of only the repo's local SDK.
            startInfo.EnvironmentVariables.Remove("DOTNET_ROOT");
            startInfo.EnvironmentVariables.Remove("DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR");
            startInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP"] = "1";

            var processOutput = Process.RunAndCaptureText(startInfo);

            if (processOutput.ExitStatus.ExitCode != 0)
            {
                throw new Exception($"Failed to instrument {path}: {processOutput.StandardOutput}{Environment.NewLine}{processOutput.StandardError}");
            }

            File.WriteAllBytes(previousOriginal, current);
            File.Copy(path, previousInstrumented);
        }
    }

    private static async Task DownloadArtifactAsync(string path, string url, string hash)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Downloading {Path.GetFileName(path)}");

            using var client = new HttpClient();
            byte[] bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);

            if (!Convert.ToHexString(SHA512.HashData(bytes)).Equals(hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"{path} checksum mismatch");
            }

            File.WriteAllBytes(path, bytes);
        }
    }

    private static string GenerateOneFuzzConfigJson(IFuzzer fuzzer)
    {
        // {setup_dir} is replaced by OneFuzz with the path to the fuzzer directory.
        string? dictionaryArgument = fuzzer.Dictionary is not null
            ? "\"-dict={setup_dir}/dictionary\","
            : null;

        // Make it easier to distinguish between long-running CI jobs and short-lived test submissions.
        string nameSuffix = Environment.GetEnvironmentVariable("TF_BUILD") is null ? "-local" : "";

        return
            $$$"""
            {
              "ConfigVersion": 3,
              "Entries": [
                {
                  "JobNotificationEmail": "brecon@microsoft.com",
                  "Skip": false,
                  "Fuzzer": {
                    "$type": "libfuzzer",
                    "FuzzingHarnessExecutableName": "libfuzzer-dotnet.exe",
                    "FuzzingTargetBinaries": [
                      {{{string.Join(", ", GetInstrumentationTargets(fuzzer).Select(t => $"\"{t}\""))}}}
                    ],
                    "CheckFuzzerHelp": false
                  },
                  "FuzzerTimeoutInSeconds": 120,
                  "OneFuzzJobs": [
                    {
                      "ProjectName": "AspNetCoreFuzzing",
                      "TargetName": "{{{fuzzer.Name}}}{{{nameSuffix}}}",
                      "TargetOptions": [
                        "--target_path=AspNetCoreFuzzing.exe",
                        "--target_arg={{{fuzzer.Name}}}"
                      ],
                      "FuzzingTargetOptions": [
                        {{{dictionaryArgument}}}
                        "-timeout=60"
                      ]
                    }
                  ],
                  "JobDependencies": [
                    ".\\*"
                  ],
                  "AdoTemplate": {
                    "Org": "devdiv",
                    "Project": "DevDiv",
                    "AssignedTo": "brecon@microsoft.com",
                    "AreaPath": "DevDiv\\ASP.NET Core",
                    "IterationPath": "DevDiv",
                    "AdoFields": {
                        "System.Title": "[{{ job.project }} {{ job.name }}]: {{ report.crash_site }}",
                        "Custom.CustomField01": "{{ job.name }}-{{ report.minimized_stack_function_lines_sha256 }}"
                    },
                    "UniqueFields": [
                      "Custom.CustomField01"
                    ],
                    "OnDuplicate": {
                      "SetState": {
                        "Resolved": "Active",
                        "Closed": "Active"
                      }
                    }
                  }
                }
              ]
            }
            """;
    }

    private static string GenerateLocalRunHelperScript(IFuzzer fuzzer)
    {
        string script = $"%~dp0libfuzzer-dotnet.exe --target_path=%~dp0AspNetCoreFuzzing.exe --target_arg={fuzzer.Name}";

        if (fuzzer.Dictionary is not null)
        {
            script += " -dict=%~dp0dictionary";
        }

        // Pass any additional arguments to the fuzzer.
        script += " %*";

        // Multiple corpus directories can be passed to the fuzzer, new test
        // inputs are then added to the first one. We put the seed corpus after
        // additional args so that if user specifies additional corpus dirs, the
        // new inputs get added there instead.
        if (fuzzer.Corpus is not null)
        {
            script += " %~dp0corpus";
        }

        return script;
    }

    private static void WorkaroundOneFuzzTaskNotAcceptingMultipleJobs(IFuzzer[] fuzzers)
    {
        string yamlPath = Environment.CurrentDirectory;
        while (!File.Exists(Path.Combine(yamlPath, "AspNetCoreFuzzing.csproj")))
        {
            yamlPath = Path.GetDirectoryName(yamlPath) ?? throw new Exception("Couldn't find AspNetCoreFuzzing.csproj");
        }

        yamlPath = Path.Combine(yamlPath, "../../../.azure/pipelines/fuzzing/deploy-to-onefuzz.yml");

        string yaml = File.ReadAllText(yamlPath);

        // At the moment OneFuzz can't handle a single deployment where multiple jobs share similar assemblies/pdbs.
        // Generate a separate step for each fuzzer instead as a workaround.
        string tasks = string.Join("\n\n", fuzzers.Select(fuzzer =>
        {
            return
                $$"""
                  - task: onefuzz-task@0
                    inputs:
                      onefuzzOSes: 'Windows'
                    env:
                      onefuzzDropDirectory: $(fuzzerProject)/deployment/{{fuzzer.Name}}
                      SYSTEM_ACCESSTOKEN: $(System.AccessToken)
                    displayName: Send {{fuzzer.Name}} to OneFuzz
                """;
        }));

        const string StartMarker = "# ONEFUZZ_TASK_WORKAROUND_START";
        const string EndMarker = "# ONEFUZZ_TASK_WORKAROUND_END";

        int start = yaml.IndexOf(StartMarker, StringComparison.Ordinal) + StartMarker.Length;
        int end = yaml.IndexOf(EndMarker, start, StringComparison.Ordinal);

        yaml = string.Concat(yaml.AsSpan(0, start), $"\n{tasks}\n", yaml.AsSpan(end));
        yaml = yaml.ReplaceLineEndings("\r\n");

        File.WriteAllText(yamlPath, yaml);
    }
}
