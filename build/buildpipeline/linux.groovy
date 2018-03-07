@Library('dotnet-ci') _

simpleNode('Ubuntu16.04','latest') {
    stage ('Checking out source') {
        checkout scm
    }
    stage ('Build') {
        sh './build.sh'
    }
}
