// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Indicates the status of a class during controller discovery. 
    /// All values except 0 represent a reason why a type is not a controller.
    /// </summary>
    [Flags]
    public enum ControllerStatus
    {
        IsController = 0,
        IsNotAClass = 1,
        IsNotPublicOrTopLevel = 2,
        IsAbstract = 4,
        ContainsGenericParameters = 8,
        // The name of the controller class is "Controller"
        NameIsController = 16,
        DoesNotEndWithControllerAndIsNotAssignable = 32
    }
}