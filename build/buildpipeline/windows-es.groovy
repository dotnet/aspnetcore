@Library('dotnet-ci') _

// 'simpleNode' indicates to Jenkins that the enclosed block runs on a node that matches
// the label 'Windows.10.Amd64.ClientRS4.ES.Open'
simpleNode('Windows.10.Amd64.ClientRS4.ES.Open') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def logFolder = getLogFolder()
        def environment = "set ASPNETCORE_TEST_LOG_DIR=${WORKSPACE}\\${logFolder}"
        bat "${environment}&.\\build.cmd -ci /p:SkipJavaClient=true"
    }
}
