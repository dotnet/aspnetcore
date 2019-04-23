using System;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// When a build partially succeeds, ignore the result.
    /// This is meant to be used after other handles, like <see cref="HandleTestFailures"/> which can read interesting data from a partially-successful build.
    /// </summary>
    class HandleSucceededWithIssuesFailures : HandleFailureBase
    {
        public override Task<bool> CanHandleFailure(ICIBuild build)
        {
            return Task.FromResult(build.Status == BuildStatus.PARTIALSUCCESS);
        }

        public override Task<IFailureHandlerResult> HandleFailure(ICIBuild build)
        {
            Reporter.Output($"Build {build.WebURL} is being ignored because is was a partial success");
            // Ignore it.
            return Task.FromResult<IFailureHandlerResult>(
                new FailureHandlerResult(build, applicableIssues: Array.Empty<ICIIssue>()));
        }
    }
}
