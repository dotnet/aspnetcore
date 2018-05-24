@Library('dotnet-ci') _

// 'node' indicates to Jenkins that the enclosed block runs on a node that matches
// the label 'windows-with-vs'
simpleNode('Windows.10.Amd64.ClientRS4.ES') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def logFolder = getLogFolder()
        def environment = "set ASPNETCORE_TEST_LOG_DIR=${WORKSPACE}\\${logFolder}"
        bat "${environment}&.\\run.cmd -CI default-build"
    }
}
