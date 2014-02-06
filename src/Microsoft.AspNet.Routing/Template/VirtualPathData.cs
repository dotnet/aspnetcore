// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
namespace Microsoft.AspNet.Routing.Template
{
    public class VirtualPathData : IVirtualPathData
    {
        private string _virtualPath;

        public VirtualPathData(IRoute route, string virtualPath)
        {
            if (route == null)
            {
                throw new ArgumentNullException("route");
            }

            if (virtualPath == null)
            {
                throw new ArgumentNullException("virtualPath");
            }

            Route = route;
            VirtualPath = virtualPath;
        }

        public IRoute Route { get; private set; }

        public string VirtualPath
        {
            get { return _virtualPath; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _virtualPath = value;
            }
        }
    }
}
