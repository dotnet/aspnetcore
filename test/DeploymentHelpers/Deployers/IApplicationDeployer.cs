using System;

namespace DeploymentHelpers
{
    /// <summary>
    /// Common operations on an application deployer.
    /// </summary>
    public interface IApplicationDeployer : IDisposable
    {
        /// <summary>
        /// Deploys the application to the target with specified <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <returns></returns>
        DeploymentResult Deploy();
    }
}