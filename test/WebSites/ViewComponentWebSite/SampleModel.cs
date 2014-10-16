// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace ViewComponentWebSite
{
    public class SampleModel
    {
        public string Prop1 { get; set; }

        public string Prop2 { get; set; }

        public Task<string> GetValueAsync()
        {
            return Task.FromResult(Prop1 + " " + Prop2);
        }
    }
}