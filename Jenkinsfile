pipeline {
    agent { docker { image 'node:12.2.0-alpine' } }
    stages {
        stage('build') {
            steps {
                sh 'npm build'
            }
        }
    }
}