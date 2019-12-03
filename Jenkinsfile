pipeline {
    agent { docker { image 'node:12.2.0-alpine' } }
    stages {
        stage('build') {
            steps {
                sh 'npm config set unsafe-perm true'
				sh 'npm install'
				sh 'npm run-script build'
            }
        }
    }
}