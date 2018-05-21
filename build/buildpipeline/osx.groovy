@Library('dotnet-ci') _

simpleNode('OSX10.12','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def logFolder = getLogFolder()
        def environment = "ASPNETCORE_TEST_LOG_DIR=${WORKSPACE}/${logFolder}"
        sh "${environment} ./build.sh --ci /p:Configuration=${params.Configuration}"
    }
}
