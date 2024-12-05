pipeline {
    agent any

    environment {
        DOCKER_CREDENTIALS_ID = 'my-docker-hub-credentials'
        SONAR_HOST_URL = 'http://localhost:9000/'
    }

    stages {
        stage('Checkout') {
            steps {
                // Checkout the code from the specified Git repository and branch
                checkout scmGit(branches: [[name: '*/main']], extensions: [], userRemoteConfigs: [[url: 'https://github.com/dilshanliyanage1000/PixshareAPI.git']])
            }
        }
        stage('SonarQube Analysis') {
            steps {
                script {
                    def scannerHome = tool name: 'SonarScanner for MSBuild'
                    withSonarQubeEnv() {
                        bat "dotnet \"${scannerHome}\\SonarScanner.MSBuild.dll\" begin /k:\"PixshareAPI\""
                        bat "dotnet build"
                        bat "dotnet \"${scannerHome}\\SonarScanner.MSBuild.dll\" end"
                    }
                }
            }
        }
        stage('Build .NET Core Project') {
            steps {
                // Restores the NuGet packages for the .NET Core project
                bat 'dotnet restore'

                // Builds the project in Release configuration
                bat 'dotnet build --configuration Release'

                // Publishes the application to a specified output directory
                bat 'dotnet publish --configuration Release --output ./publish'
            }
        }
        stage('Run Tests') {
            steps {
                // Create tests here
                echo 'Mock up Tests here'
            }
        }
    }
}