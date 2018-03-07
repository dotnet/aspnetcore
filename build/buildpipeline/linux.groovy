@Library('dotnet-ci') _

simpleNode('Ubuntu14.04','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        sh './build.sh'
    }
}
