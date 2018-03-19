import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def linuxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/linux.groovy')
def osxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/osx.groovy')
String configuration = 'Release'
def parameters = [
    'Configuration': configuration
]

windowsPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} x64 Build", parameters)
windowsPipeline.triggerPipelineOnGithubPush(parameters)

linuxPipeline.triggerPipelineOnEveryGithubPR("Ubuntu 16.04 ${configuration} Build", parameters)
linuxPipeline.triggerPipelineOnGithubPush(parameters)

osxPipeline.triggerPipelineOnEveryGithubPR("OSX 10.12 ${configuration} Build", parameters)
osxPipeline.triggerPipelineOnGithubPush(parameters)
