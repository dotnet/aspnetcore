@Library('dotnet-ci') _

simpleNode('OSX10.12','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        environment {
            DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
            DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
        }

        sh './build.sh'
    }
}
