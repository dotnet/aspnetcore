import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def linuxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/linux.groovy')
def osxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/osx.groovy')
String configuration = 'Release'
def parameters = [
    'Configuration': configuration,
    'DOTNET_CLI_TELEMETRY_OPTOUT': 'true',
    'DOTNET_SKIP_FIRST_TIME_EXPERIENCE': 'true'
]

def jobName = "${RepoName} ${BrancName}"

windowsPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} x64 Build", parameters, jobName)
windowsPipeline.triggerPipelineOnGithubPush(parameters, jobName)

linuxPipeline.triggerPipelineOnEveryGithubPR("Ubuntu 16.04 ${configuration} Build", parameters, jobName)
linuxPipeline.triggerPipelineOnGithubPush(parameters, jobName)

osxPipeline.triggerPipelineOnEveryGithubPR("OSX 10.12 ${configuration} Build", parameters, jobName)
osxPipeline.triggerPipelineOnGithubPush(parameters, jobName)
