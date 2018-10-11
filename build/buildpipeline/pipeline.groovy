import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def windowsAppverifPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows-appverif.groovy')

def configurations = [
    'Debug',
    'Release'
]

configurations.each { configuration ->

    def params = [
        'Configuration': configuration
    ]

    windowsPipeline.triggerPipelineOnEveryGithubPR("Windows ${configuration} x64 Build", params)
    windowsPipeline.triggerPipelineOnGithubPush(params)

    windowsAppverifPipeline.triggerPipelineOnEveryGithubPR("Windows AppVerifier ${configuration} x64 Build", params)
}
