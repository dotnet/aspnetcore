using System;
using Microsoft.Framework.Logging;

namespace DeploymentHelpers
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

            if (deploymentParameters.RuntimeFlavor == RuntimeFlavor.mono)
            {
                return new MonoDeployer(deploymentParameters, logger);
            }

            switch (deploymentParameters.ServerType)
            {
                case ServerType.IISExpress:
                    return new IISExpressDeployer(deploymentParameters, logger);
#if DNX451
                case ServerType.IIS:
                case ServerType.IISNativeModule:
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