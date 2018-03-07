import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def linuxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/linux.groovy')
def osxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/osx.groovy')
String configuration = 'Release'

windowsPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} x64 Build", ['Configuration':configuration])
windowsPipeline.triggerPipelineOnGithubPush(['Configuration':configuration])

linuxPipeline.triggerPipelineOnEveryGithubPR("Ubuntu14.04 ${configuration} Build", ['Configuration':configuration])
linuxPipeline.triggerPipelineOnGithubPush(['Configuration':configuration])

osxPipeline.triggerPipelineOnEveryGithubPR("OSX10.12 ${configuration} Build", ['Configuration':configuration])
osxPipeline.triggerPipelineOnGithubPush(['Configuration':configuration])
