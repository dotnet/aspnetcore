using System;
using System.Collections.Generic;
using Microsoft.Build.OOB.ESRP;

namespace MicroBuild.Plugins.TeamCity.Signing
{
    /// <summary>
    /// Map MicroBuild cert names to their operations. Should match names in <see href="https://microsoft.sharepoint.com/:o:/r/teams/DD_CoEng/_layouts/15/WopiFrame.aspx?sourcedoc={67a1015a-0742-49d3-8556-de2a101d97d8}&action=edit&wd=target%28MicroBuild%2Eone%7CA0381102-446A-4329-B353-B97A11F92C8B%2FWhat%20is%20MicroBuild%3F%7CA1405334-6C1C-46CE-8239-CD3881027CFF%2F%29%20onenote%3Ahttps%3A%2F%2Fmicrosoft%2Esharepoint%2Ecom%2Fteams%2FDD_CoEng%2FDocuments%2FOneNote%2FMicroBuild%20Documentation%2FMicroBuild%2Eone#What%20is%20MicroBuild&section-id=%7BA0381102-446A-4329-B353-B97A11F92C8B%7D&page-id=%7BA1405334-6C1C-46CE-8239-CD3881027CFF%7D&end"/>
    /// </summary>
    internal class CertificateNameMapping
    {
        public static Operations[] GetOperations(string friendlyName)
        {
            if (!_mapping.TryGetValue(friendlyName, out var retVal))
            {
                throw new KeyNotFoundException($"Certificate name '{friendlyName}' is not recognized.");
            }
            return retVal;
        }

        private static readonly Dictionary<string, Operations[]> _mapping = new Dictionary<string, Operations[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft"] = OperationsJson.MicrosoftAuthentiCodeSha1Sha2,
            ["Microsoft400"] = OperationsJson.MicrosoftAuthentiCodeSha2,
            ["Microsoft402"] = OperationsJson.MicrosoftAuthentiCodeSha2HashSha1,
            ["Vsix"] = OperationsJson.MicrosoftOpc,
            ["VsixSHA2"] = OperationsJson.MicrosoftOpc2,
            ["3PartyDual"] = OperationsJson.Microsoft3rdPartyAppComponentDual,
            ["3PartySHA2"] = OperationsJson.Microsoft3rdPartyAppComponent,
            ["NuGet"] = OperationsJson.MicrosoftNuGet,
            ["MicrosoftJAR"] = OperationsJson.MicrosoftJava,
        };
    }
}
