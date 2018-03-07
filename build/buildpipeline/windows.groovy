@Library('dotnet-ci') _

// 'node' indicates to Jenkins that the enclosed block runs on a node that matches
// the label 'windows-with-vs'
simpleNode('Windows_NT','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        environment {
            DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
            DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
        }

        bat '.\\run.cmd default-build'
    }
}
