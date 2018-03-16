@Library('dotnet-ci') _

simpleNode('OSX10.12','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        sh './build.sh --ci'
    }
}
