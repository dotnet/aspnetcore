// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public interface IRouter
    {
        Task RouteAsync(RouteContext context);

        string BindPath(BindPathContext context);
    }
}
