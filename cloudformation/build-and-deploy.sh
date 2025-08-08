#!/bin/bash

# Clean Architecture .NET - AWS ECS Fargate Build and Deploy Script
# 
# ⚠️  WARNING: This script is EXCLUSIVELY for AWS ECS Fargate deployment!
# For local development, use: make run or dotnet run --project src/WebApi
#
# Usage: ./build-and-deploy.sh [environment] [region]
# Example: ./build-and-deploy.sh prod us-west-2

set -e

# Default values
ENVIRONMENT=${1:-dev}
REGION=${2:-us-east-1}
APP_NAME="cleanarch"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

echo_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

echo_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

echo_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check and handle stack states
handle_stack_state() {
    local stack_name=$1
    local region=$2
    
    # Check if stack exists and get its state
    local stack_status=$(aws cloudformation describe-stacks --stack-name "$stack_name" --region "$region" --query 'Stacks[0].StackStatus' --output text 2>/dev/null)
    
    if [ $? -ne 0 ]; then
        # Stack doesn't exist, can proceed with creation
        return 0
    fi
    
    case $stack_status in
        "ROLLBACK_COMPLETE"|"CREATE_FAILED"|"UPDATE_ROLLBACK_COMPLETE")
            echo_warning "Stack $stack_name is in $stack_status state. Deleting and recreating..."
            aws cloudformation delete-stack --stack-name "$stack_name" --region "$region"
            aws cloudformation wait stack-delete-complete --stack-name "$stack_name" --region "$region"
            echo_info "Stack $stack_name deleted successfully"
            ;;
        "DELETE_IN_PROGRESS")
            echo_info "Stack $stack_name is being deleted. Waiting..."
            aws cloudformation wait stack-delete-complete --stack-name "$stack_name" --region "$region"
            ;;
        "CREATE_IN_PROGRESS"|"UPDATE_IN_PROGRESS"|"ROLLBACK_IN_PROGRESS")
            echo_info "Stack $stack_name is in progress ($stack_status). Waiting for completion..."
            aws cloudformation wait stack-create-complete --stack-name "$stack_name" --region "$region" 2>/dev/null || \
            aws cloudformation wait stack-update-complete --stack-name "$stack_name" --region "$region" 2>/dev/null
            ;;
    esac
}

# Check AWS CLI
if ! command -v aws &> /dev/null; then
    echo_error "AWS CLI not found. Please install AWS CLI."
    exit 1
fi

# Check Docker
if ! command -v docker &> /dev/null; then
    echo_error "Docker not found. Please install Docker."
    exit 1
fi

# Check .NET CLI
if ! command -v dotnet &> /dev/null; then
    echo_error ".NET CLI not found. Please install .NET 8 SDK."
    exit 1
fi

# Get AWS Account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
if [ $? -ne 0 ]; then
    echo_error "Failed to get AWS Account ID. Check your AWS credentials."
    exit 1
fi

echo_info "Starting deployment for environment: $ENVIRONMENT in region: $REGION"
echo_info "AWS Account ID: $ACCOUNT_ID"

# Step 1: Deploy ECR repository
echo_info "Step 1: Deploying ECR repository..."
aws cloudformation deploy \
    --template-file ecr.yaml \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecr" \
    --parameter-overrides \
        EnvironmentName=$ENVIRONMENT \
        ApplicationName=$APP_NAME \
    --region $REGION \
    --tags \
        Environment=$ENVIRONMENT \
        Project="clean-dotnet" \
        Component="ecr"

if [ $? -eq 0 ]; then
    echo_success "ECR repository deployed successfully"
else
    echo_error "Failed to deploy ECR repository"
    exit 1
fi

# Get ECR repository URI
ECR_URI=$(aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecr" \
    --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryURI`].OutputValue' \
    --output text \
    --region $REGION)

echo_info "ECR Repository URI: $ECR_URI"

# Step 2: Build and push Docker image
echo_info "Step 2: Building and pushing Docker image..."

# Login to ECR
echo_info "Logging into ECR..."
ECR_REGISTRY=$(echo $ECR_URI | cut -d'/' -f1)
aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin $ECR_REGISTRY

# Build Docker image
echo_info "Building Docker image..."
cd ..

# Pre-flight check: Test .NET build locally before Docker build
# This catches compilation errors early and saves time vs waiting for Docker build to fail
echo_info "Running pre-flight check - testing .NET build locally..."
export PATH="$HOME/.dotnet:$PATH"
dotnet restore
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo_error "Pre-flight check failed - .NET application has build errors"
    exit 1
fi
echo_success "Pre-flight check passed"

# Build Docker image for AWS Fargate (AMD64)
echo_info "Building .NET app locally to avoid Rosetta emulation issues..."
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish --no-restore

echo_info "Building AMD64 Docker image using pre-built binaries..."
docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-dotnet-$APP_NAME .
docker tag clean-dotnet-$APP_NAME:latest "${ECR_URI}:latest"
docker tag clean-dotnet-$APP_NAME:latest "${ECR_URI}:$(date +%Y%m%d-%H%M%S)"

# Push Docker image
echo_info "Pushing Docker image..."
docker push "${ECR_URI}:latest"
docker push "${ECR_URI}:$(date +%Y%m%d-%H%M%S)"

echo_success "Docker image built and pushed successfully"

# Return to cloudformation directory
cd cloudformation

# Step 3: Deploy VPC and ALB
echo_info "Step 3: Deploying VPC and ALB..."
aws cloudformation deploy \
    --template-file vpc-alb.yaml \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-vpc-alb" \
    --parameter-overrides \
        EnvironmentName=$ENVIRONMENT \
        ApplicationName=$APP_NAME \
    --region $REGION \
    --tags \
        Environment=$ENVIRONMENT \
        Project="clean-dotnet" \
        Component="vpc"

if [ $? -eq 0 ]; then
    echo_success "VPC and ALB deployed successfully"
else
    echo_error "Failed to deploy VPC and ALB"
    exit 1
fi

# Step 4: Deploy ECS Fargate service
echo_info "Step 4: Deploying ECS Fargate service..."

# Handle any problematic stack states first
handle_stack_state "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecs" "$REGION"

aws cloudformation deploy \
    --template-file ecs-fargate.yaml \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecs" \
    --parameter-overrides \
        EnvironmentName=$ENVIRONMENT \
        ApplicationName=$APP_NAME \
        ImageURI=$ECR_URI:latest \
        DesiredCount=1 \
    --capabilities CAPABILITY_NAMED_IAM \
    --region $REGION \
    --tags \
        Environment=$ENVIRONMENT \
        Project="clean-dotnet" \
        Component="ecs"

if [ $? -eq 0 ]; then
    echo_success "ECS Fargate service deployed successfully"
else
    echo_error "Failed to deploy ECS Fargate service"
    exit 1
fi

# Step 5: Force new deployment to ensure latest image is used
echo_info "Step 5: Forcing new deployment to use latest container image..."
aws ecs update-service \
    --cluster "$ENVIRONMENT-$APP_NAME-cluster" \
    --service "$ENVIRONMENT-$APP_NAME-service" \
    --force-new-deployment \
    --region $REGION > /dev/null

if [ $? -eq 0 ]; then
    echo_success "Forced deployment initiated successfully"
    echo_info "Waiting for deployment to complete (this may take 3-5 minutes)..."

    # Wait for deployment to complete
    aws ecs wait services-stable \
        --cluster "$ENVIRONMENT-$APP_NAME-cluster" \
        --services "$ENVIRONMENT-$APP_NAME-service" \
        --region $REGION

    if [ $? -eq 0 ]; then
        echo_success "Deployment completed and services are stable"
    else
        echo_warning "Deployment timeout - services may still be starting"
    fi
else
    echo_warning "Failed to force new deployment, but initial deployment was successful"
fi

# Get application URLs
echo_info "Getting application URLs..."
API_URL=$(aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`APIURL`].OutputValue' \
    --output text \
    --region $REGION)

SWAGGER_URL=$(aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`SwaggerURL`].OutputValue' \
    --output text \
    --region $REGION)

HEALTH_URL=$(aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-$ENVIRONMENT-$APP_NAME-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`HealthCheckURL`].OutputValue' \
    --output text \
    --region $REGION)

echo_success "Deployment completed successfully!"
echo ""
echo "=== Application URLs ==="
echo_info "API Base URL: $API_URL"
echo_info "Swagger UI: $SWAGGER_URL"
echo_info "Health Check: $HEALTH_URL"
echo ""
echo "=== Available API Endpoints ==="
echo_info "POST $API_URL/api/v1/customers - Create customer"
echo_info "POST $API_URL/api/v1/orders - Create order"
echo_info "GET  $API_URL/api/v1/orders/customer/{customerId} - Get customer orders"
echo_info "GET  $API_URL/health - Health check"
echo ""
echo_warning "Note: It may take a few minutes for the services to be healthy and accessible."
echo_info "You can check the ECS service status in the AWS Console or monitor the health endpoint."
echo ""
echo_info "To test the API:"
echo_info "curl -X POST '$API_URL/api/v1/customers' -H 'Content-Type: application/json' -d '{\"name\":\"John Doe\",\"email\":\"john.doe@example.com\"}'"