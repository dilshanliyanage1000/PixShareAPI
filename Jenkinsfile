pipeline {
    agent any

    environment {
        DOCKER_CREDENTIALS_ID = 'my-docker-hub-credentials'
        SONAR_HOST_URL = 'http://localhost:9000/'
        DOCKER_REPO_NAME = 'dilshanliyanage1000/pixshareapi'
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
        stage('Deliver to Dockerhub') {
            steps {
                script {
                    // Log in to Docker Hub
                    withCredentials([usernamePassword(credentialsId: 'my-docker-hub-credentials', usernameVariable: 'DOCKER_USERNAME', passwordVariable: 'DOCKER_PASSWORD')]) {
                        bat "docker login -u %DOCKER_USERNAME% -p %DOCKER_PASSWORD%"
                    }
                    // Build the Docker image
                    bat "docker build -t pixshareapi:latest -f ./PixshareAPI/Dockerfile ."

                    // Tag the Docker image for the repository
                    bat "docker tag pixshareapi:latest %DOCKER_REPO_NAME%:dev"

                    // Push the Docker image to Docker Hub
                    bat "docker push %DOCKER_REPO_NAME%:dev"
                }
            }
        }
        stage('Deploy to DEV Env') {
            steps {
                script {
                    // Pull the image from Docker Hub to the local machine
                    bat "docker pull %DOCKER_REPO_NAME%:dev"

                    // Stop and remove existing container if it exists
                    // bat "docker ps -q -f name=pixshare_container >nul 2>&1 && (docker stop pixshare_container && docker rm pixshare_container) || echo No existing container to stop and remove"
                    
                    // Run the Docker container on the local machine.
                    bat 'docker run -itd -p 3002:8080 $DOCKER_REPO_NAME:dev'

                }
            }
        }
        stage('Deploy to QAT Env') {
            steps {
                echo 'Mocked up QAT Env running successfully'
            }
        }
        stage('Deploy to Staging Env') {
            steps {
                echo 'Mocked up Staging Env running successfully'
            }
        }
    }
}
