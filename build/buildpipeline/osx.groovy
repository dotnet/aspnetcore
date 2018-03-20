@Library('dotnet-ci') _

simpleNode('OSX10.12','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def environment = 'export SIGNALR_TESTS_VERBOSE=1'
        sh "${environment} && ./build.sh --ci"
    }
}
