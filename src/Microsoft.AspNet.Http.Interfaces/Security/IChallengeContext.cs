// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Http.Interfaces.Authentication
{
    public interface IChallengeContext
    {
        IEnumerable<string> AuthenticationSchemes {get;}
        IDictionary<string,string> Properties {get;}

        void Accept(string authenticationType, IDictionary<string,object> description);
    }
}