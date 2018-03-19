@Library('dotnet-ci') _

simpleNode('Ubuntu16.04', 'latest-or-auto-docker') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def environment = 'export SIGNALR_TESTS_VERBOSE=1'
        sh "${environment} && ./build.sh --ci"
    }
}
