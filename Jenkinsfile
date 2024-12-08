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
                echo "Checkout branch..."
                checkout scm
            }
        }
        stage('Code Analysis with SonarQube') {
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
                // Run unit tests with proper formatting and collect code coverage
                echo 'Running unit tests with code coverage...'
                bat 'dotnet test ./Tests/Tests.csproj --collect:"XPlat Code Coverage" --configuration Release --results-directory Tests/TestResults'

                // Publish the coverage report in Jenkins
                echo 'Publishing code coverage reports...'
                cobertura coberturaReportFile: '**/TestResults/**/coverage.cobertura.xml'
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
                    echo 'Pulling and deploying the image to DEV environment...'
                    bat """
                        docker pull %DOCKER_REPO_NAME%:dev || exit
                        docker ps -q -f name=pixshare_container && docker stop pixshare_container && docker rm pixshare_container
                        docker run -itd --name pixshare_container -p 3002:8080 %DOCKER_REPO_NAME%:dev
                    """
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
