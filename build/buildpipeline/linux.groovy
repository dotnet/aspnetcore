@Library('dotnet-ci') _

simpleNode('Ubuntu16.04', 'latest-or-auto-docker') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def logFolder = getLogFolder()
        def environment = "ASPNETCORE_TEST_LOG_DIR=${WORKSPACE}/${logFolder}"
        sh "${environment} ./build.sh --ci /p:Configuration=${params.Configuration}"
    }
}
