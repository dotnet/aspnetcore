// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.CertificateGeneration.Task
{
    public class GenerateSSLCertificateTask : Build.Utilities.Task
    {
        public bool Force { get; set; }

        protected string Subject { get; set; } = "CN=localhost";

        public override bool Execute()
        {
            var subjectValue = Subject;
            var sansValue = new List<string> { "localhost" };
            var friendlyNameValue = "ASP.NET Core HTTPS development certificate";
            var notBeforeValue = DateTime.UtcNow;
            var expiresValue = DateTime.UtcNow.AddYears(1);
            var storeNameValue = StoreName.My;
            var storeLocationValue = StoreLocation.CurrentUser;

            var cert = CertificateManager.FindCertificate(subjectValue, storeNameValue, storeLocationValue);

            if (cert != null && !Force)
            {
                LogMessage($"A certificate with subject name '{Subject}' already exists. Skipping certificate generation.");
                return true;
            }

            var generated = CertificateManager.GenerateSSLCertificate(subjectValue, sansValue, friendlyNameValue, notBeforeValue, expiresValue, storeNameValue, storeLocationValue);
            LogMessage($"Generated certificate {generated.SubjectName.Name} - {generated.Thumbprint} - {generated.FriendlyName}");

            return true;
        }

        protected virtual void LogMessage(string message) => Log.LogMessage(MessageImportance.High, message);
    }
}
