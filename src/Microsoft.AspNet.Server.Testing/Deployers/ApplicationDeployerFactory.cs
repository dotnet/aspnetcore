// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Factory to create an appropriate deployer based on <see cref="DeploymentParameters"/>.
    /// </summary>
    public class ApplicationDeployerFactory
    {
        /// <summary>
        /// Creates a deployer instance based on settings in <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <param name="deploymentParameters"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IApplicationDeployer Create(DeploymentParameters deploymentParameters, ILogger logger)
        {
            if (deploymentParameters == null)
            {
                throw new ArgumentNullException(nameof(deploymentParameters));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (deploymentParameters.RuntimeFlavor == RuntimeFlavor.Mono)
            {
                return new MonoDeployer(deploymentParameters, logger);
            }

            switch (deploymentParameters.ServerType)
            {
                case ServerType.IISExpress:
                    return new IISExpressDeployer(deploymentParameters, logger);
#if NET451
                case ServerType.IIS:
                    return new IISDeployer(deploymentParameters, logger);
#endif
                case ServerType.WebListener:
                case ServerType.Kestrel:
                    return new SelfHostDeployer(deploymentParameters, logger);
                default:
                    throw new NotSupportedException(
                        string.Format("Found no deployers suitable for server type '{0}' with the current runtime.", 
                        deploymentParameters.ServerType)
                        );
            }
        }
    }
}