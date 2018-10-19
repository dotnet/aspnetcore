import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')

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
}
