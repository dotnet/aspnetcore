import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def windowsESPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows-es.groovy')
String configuration = 'Release'
def parameters = [
    'Configuration': configuration
]

windowsPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} x64 Build", parameters)
windowsPipeline.triggerPipelineOnGithubPush(parameters)

windowsESPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} Spanish Language x64 Build", parameters)
windowsESPipeline.triggerPipelineOnGithubPush(parameters)
