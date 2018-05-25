@Library('dotnet-ci') _

// 'node' indicates to Jenkins that the enclosed block runs on a node that matches
// the label 'windows-with-vs'
simpleNode('Windows.10.Enterprise.RS3.ASPNET') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        def logFolder = getLogFolder()
        def environment = "\$env:ASPNETCORE_TEST_LOG_DIR='${WORKSPACE}\\${logFolder}'"
        bat "powershell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command \"&.\\tools\\update_schema.ps1;${environment};&.\\run.ps1 -CI default-build /p:Configuration=${params.Configuration}\""
    }
}
