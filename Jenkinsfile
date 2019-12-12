#!groovy

def DEPLOYMENT_TARGET = "UNKNOWN"

pipeline {
	agent any
	environment {
        PROJECT_NAME = 'CmsFe'
        SLACK_CHANNEL = 'N/A'
		SOURCE_DIR='.\\DeploymentScripts'
		DOCKER_REGISTRY_CREDENTIALS = 'dpr.od.rferl.org'
		DOCKER_REGISTRY = 'dpr.od.rferl.org:9999'		
    }
    stages {	
		stage("Build") {
			steps {
				echo 'stage Build'
				/*node('linux') {
					checkout scm
					sh ("chmod +x ./Cake/build.sh")
					sh ("./Cake/build.sh --script ./Cake/build.cake --bootstrap")
					sh ('./Cake/build.sh --script ./Cake/build.cake --target="PublishBinaries"')
				}*/
				powershell ("./Cake/build.sh --script ./Cake/build.cake --bootstrap")
			}
		}
		stage('Publish Test Results') {
			steps {
				node('linux') {
					mstest testResultsFile: 'dist/Results/TestResults.xml'
					cobertura autoUpdateHealth: false, autoUpdateStability: false, coberturaReportFile: 'dist/Results/CodeCoverage.xml', conditionalCoverageTargets: '60, 0, 0', failUnhealthy: false, failUnstable: false, 
						lineCoverageTargets: '60, 0, 0', maxNumberOfBuilds: 0, methodCoverageTargets: '60, 0, 0', onlyStable: false, sourceEncoding: 'ASCII', zoomCoverageChart: false
				}
			}
		}
		stage("Push Docker Images") {
			when {
				not {
					branch 'feature/*'
				}
				not {
					branch 'bugfix/*'
				}
			}
			steps {
				node('linux') {
					withCredentials([usernamePassword(credentialsId: "${DOCKER_REGISTRY_CREDENTIALS}", usernameVariable: "DOCKER_REGISTRY_USERNAME", passwordVariable: "DOCKER_REGISTRY_PASSWORD")]) {
						sh ("chmod +x ./Cake/build.sh")
						sh ("./Cake/build.sh --script ./Cake/DockerBuild.cake")
					}
				}			    
			}
		}
		stage ('Deploy to Testing'){
			when {
				not {
					buildingTag()
				}
				branch 'release/*'
			}
			steps {
				script {
					DEPLOYMENT_TARGET = "QA"
					def SWARM_MANAGER = "QAdockerhg01"
					def COMPOSE_FILES = "docker-compose.Release.yml, docker-compose.QA.yml"
					
					withCredentials([usernamePassword(credentialsId: "${DOCKER_REGISTRY_CREDENTIALS}", usernameVariable: "DOCKER_REGISTRY_USERNAME", passwordVariable: "DOCKER_REGISTRY_PASSWORD")]) {
						powershell ('./Cake/build.ps1 -Script ./Cake/DockerBuild.cake --bootstrap')
						powershell ("./Cake/build.ps1 -Script ./Cake/DockerBuild.cake -target 'Deploy' -ScriptArgs '-environment=${DEPLOYMENT_TARGET}'")
						sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\DeployStack.ps1 $SOURCE_DIR $SWARM_MANAGER $COMPOSE_FILES $PROJECT_NAME $DEPLOYMENT_TARGET $DOCKER_REGISTRY_USERNAME $DOCKER_REGISTRY_PASSWORD $DOCKER_REGISTRY")
						println sOut
					}

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\ClearFiles.ps1 $SWARM_MANAGER") 
					println sOut

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\StackStatus.ps1 $SWARM_MANAGER $PROJECT_NAME") 
					println sOut
				}
			}
		}
		stage ('Confirmation for stage deployment') {
			when {
				not {
					buildingTag()
				}
				branch 'release/*'
			}
			steps {
				input('Deploy to Staging environment?')
			}
		}
		stage ('Deploy to Staging'){
			when {
				not {
					buildingTag()
				}
				anyOf {
					branch 'release/*'; branch 'hotfix/*'
				}
			}
			steps {
				script {					
					DEPLOYMENT_TARGET = "STAGE"
					def SWARM_MANAGER = "STAGEdockerhg01"
					def COMPOSE_FILES = "docker-compose.Release.yml, docker-compose.STAGE.yml"

					withCredentials([usernamePassword(credentialsId: "${DOCKER_REGISTRY_CREDENTIALS}", usernameVariable: "DOCKER_REGISTRY_USERNAME", passwordVariable: "DOCKER_REGISTRY_PASSWORD")]) {
						powershell ('./Cake/build.ps1 -Script ./Cake/DockerBuild.cake --bootstrap')
						powershell ("./Cake/build.ps1 -Script ./Cake/DockerBuild.cake -target 'Deploy' -ScriptArgs '-environment=${DEPLOYMENT_TARGET}'")
						sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\DeployStack.ps1 $SOURCE_DIR $SWARM_MANAGER $COMPOSE_FILES $PROJECT_NAME $DEPLOYMENT_TARGET $DOCKER_REGISTRY_USERNAME $DOCKER_REGISTRY_PASSWORD $DOCKER_REGISTRY")
						println sOut
					}

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\ClearFiles.ps1 $SWARM_MANAGER") 
					println sOut

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\StackStatus.ps1 $SWARM_MANAGER $PROJECT_NAME") 
					println sOut
				}
			}
		}
		stage ('Deploy to Production'){
			when {
				buildingTag()
			}
			steps {
				script {
					DEPLOYMENT_TARGET = "PROD"
					def SWARM_MANAGER = "PRODdockerhg01"
					def COMPOSE_FILES = "docker-compose.Release.yml, docker-compose.PROD.yml"
					
					withCredentials([usernamePassword(credentialsId: "${DOCKER_REGISTRY_CREDENTIALS}", usernameVariable: "DOCKER_REGISTRY_USERNAME", passwordVariable: "DOCKER_REGISTRY_PASSWORD")]) {
						powershell ('./Cake/build.ps1 -Script ./Cake/DockerBuild.cake --bootstrap')
						powershell ("./Cake/build.ps1 -Script ./Cake/DockerBuild.cake -target 'Deploy' -ScriptArgs '-environment=${DEPLOYMENT_TARGET}'")
						sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\DeployStack.ps1 $SOURCE_DIR $SWARM_MANAGER $COMPOSE_FILES $PROJECT_NAME $DEPLOYMENT_TARGET $DOCKER_REGISTRY_USERNAME $DOCKER_REGISTRY_PASSWORD $DOCKER_REGISTRY")
						println sOut
					}

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\ClearFiles.ps1 $SWARM_MANAGER") 
					println sOut

					sOut=powershell(returnStdout: true, script: "$SOURCE_DIR\\StackStatus.ps1 $SWARM_MANAGER $PROJECT_NAME") 
					println sOut
				}
			}
		}
    }
}