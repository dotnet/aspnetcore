@Library('dotnet-ci') _

simpleNode('Ubuntu16.04', 'latest-or-auto-docker') {
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
