// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModels;

namespace ApplicationModelWebSite
{
    public class ControllerLisenceConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            controller.Properties["lisence"] = "Copyright (c) Microsoft Open Technologies, Inc. All rights reserved." +
                " Licensed under the Apache License, Version 2.0. See License.txt " +
                "in the project root for license information.";
        }
    }
}