// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using System;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupBase
    {
        public StartupBase()
        {
        }

		public void ConfigureBaseClassServices(IServiceCollection services)
		{
			services.AddOptions();
			services.Configure<FakeOptions>(o =>
			{
				o.Configured = true;
				o.Environment = "BaseClass";
			});
		}

	}
}