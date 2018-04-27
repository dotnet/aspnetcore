import org.dotnet.ci.pipelines.Pipeline

def windowsPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/windows.groovy')
def linuxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/linux.groovy')
def osxPipeline = Pipeline.createPipeline(this, 'build/buildpipeline/osx.groovy')

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

    linuxPipeline.triggerPipelineOnEveryGithubPR("Ubuntu 16.04 ${configuration} Build", params)
    linuxPipeline.triggerPipelineOnGithubPush(params)

    osxPipeline.triggerPipelineOnEveryGithubPR("OSX 10.12 ${configuration} Build", params)
    osxPipeline.triggerPipelineOnGithubPush(params)

}
