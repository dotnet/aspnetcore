using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Build.OOB.ESRP
{
    public class OperationsJson
    {
        private readonly Dictionary<string, Operations[]> _operationsJson =  new Dictionary<string, Operations[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "MicrosoftSN", MicrosoftSN },
            { "MicrosoftSharedLibrariesSN", MicrosoftSharedLibrariesSN },
            { "MicrosoftAuthentiCodeSha2", MicrosoftAuthentiCodeSha2 },
            { "MicrosoftNuGet", MicrosoftNuGet },
            { "MicrosoftOpc2", MicrosoftOpc2 },
            { "Microsoft3rdPartyAppComponent", Microsoft3rdPartyAppComponent },
            { "MicrosoftJava", MicrosoftJava },
        };

        public const string DefaultOpusName = "Microsoft";
        public const string DefaultOpusInfo = "https://www.microsoft.com/";

        public Operations[] this[string key]
        {
            get
            {
                _operationsJson.TryGetValue(key, out var value);

                return value ?? throw new InvalidOperationException($"Could not find operations for keycode {key}");
            }
        }

        public static OperationsJson Instance { get; } = new OperationsJson();

        private OperationsJson()
        {
        }

        public bool ContainsKey(string key)
        {
            return _operationsJson.ContainsKey(key);
        }

        // Classic ID: 25, 67
        public static Operations[] MicrosoftSN => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_235845_SN,
                OperationCode = ESRP.OperationCode.StrongNameSign
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_235845_SN,
                OperationCode = ESRP.OperationCode.StrongNameVerify,
            }
        };

        // Classic ID: 72
        public static Operations[] MicrosoftSharedLibrariesSN => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_233863_SN,
                OperationCode = OperationCode.StrongNameSign
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_233863_SN,
                OperationCode = OperationCode.StrongNameVerify
            }
        };

        // Classic ID: 400
        public static Operations[] MicrosoftAuthentiCodeSha2 => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_230012,
                OperationCode = OperationCode.SigntoolSign,
                Parameters = new JObject
                {
                    { "OpusName", DefaultOpusName },
                    { "OpusInfo", DefaultOpusInfo },
                    { "FileDigest", @"/fd ""SHA256""" },
                    { "PageHash", "/NPH" },
                    { "TimeStamp", @"/tr ""http://rfc3161.gtm.corp.microsoft.com/TSS/HttpTspServer"" /td sha256" }
                },
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_230012,
                OperationCode = OperationCode.SigntoolVerify
            }
        };

        // Classic ID: N/A
        public static Operations[] MicrosoftNuGet => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_401405,
                OperationCode = OperationCode.NuGetSign
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_401405,
                OperationCode = OperationCode.NuGetVerify
            }
        };

        // Classic ID: 100040160
        public static Operations[] MicrosoftOpc2 => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_233016,
                OperationCode = OperationCode.OpcSign,
                Parameters = new JObject
                {
                    { "FileDigest", "/fd SHA256" }
                }
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_233016,
                OperationCode = OperationCode.OpcVerify
            }
        };

        // Classic ID: 135020002
        public static Operations[] Microsoft3rdPartyAppComponent => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_231522,
                OperationCode = OperationCode.SigntoolSign,
                Parameters = new JObject
                {
                    { "OpusName", DefaultOpusName },
                    { "OpusInfo", DefaultOpusInfo },
                    { "Append", "/as" },
                    { "FileDigest", @"/fd ""SHA256""" },
                    { "PageHash", "/NPH" },
                    { "TimeStamp", @"/tr ""http://rfc3161.gtm.corp.microsoft.com/TSS/HttpTspServer""" }
                }
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_231522,
                OperationCode = OperationCode.SigntoolVerify,
            }
        };

        public static Operations[] MicrosoftJava => new Operations[]
        {
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_232612_Java,
                OperationCode = OperationCode.JavaSign,
                Parameters = new JObject
                {
                    { "SigAlg", "SHA256withRSA" },
                    { "Timestamp", "-tsa http://sha256timestamp.ws.symantec.com/sha256/timestamp" },
                }
            },
            new Operations
            {
                KeyCode = ESRP.KeyCode.CP_232612_Java,
                OperationCode = OperationCode.JavaVerify,
            }
        };
    }
}
