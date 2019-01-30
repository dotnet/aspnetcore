// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.BlazorExtension
{
    public class AboutDialogInfoAttribute : RegistrationAttribute
    {
        private readonly string _detailsId;
        private readonly string _name;
        private readonly string _nameId;
        private readonly string _packageGuid;

        // nameId and detailsId are resource IDs, they should start with #
        public AboutDialogInfoAttribute(string packageGuid, string name, string nameId, string detailsId)
        {
            _packageGuid = packageGuid;
            _name = name;
            _nameId = nameId;
            _detailsId = detailsId;
        }

        private string GetKeyName()
        {
            return "InstalledProducts\\" + _name;
        }

        public override void Register(RegistrationContext context)
        {
            var version = typeof(AboutDialogInfoAttribute).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            using (var key = context.CreateKey(GetKeyName()))
            {
                key.SetValue(null, _nameId);
                key.SetValue("Package", Guid.Parse(_packageGuid).ToString("B"));
                key.SetValue("ProductDetails", _detailsId);
                key.SetValue("UseInterface", false);
                key.SetValue("UseVSProductID", false);

                if (version != null)
                {
                    key.SetValue("PID", version);
                }
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(GetKeyName());
        }
    }
}
