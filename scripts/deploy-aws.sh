#!/bin/bash

# AWS Deployment Script for Clean Architecture .NET 8 Application
# This script deploys the application to AWS ECS Fargate using CloudFormation

set -e

echo "============================================"
echo "Clean Architecture .NET 8 AWS Deployment"
echo "============================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${PURPLE}[STEP]${NC} $1"
}

# Configuration
PROJECT_NAME="clean-architecture-dotnet"
REGION=${AWS_REGION:-"us-east-1"}
ECR_REPO_NAME=${ECR_REPO:-"${PROJECT_NAME}"}
STACK_PREFIX="clean-arch"

# Check prerequisites
check_prerequisites() {
    print_header "Checking prerequisites..."
    
    # Check AWS CLI
    if ! command -v aws &> /dev/null; then
        print_error "AWS CLI is not installed. Please install it first."
        print_status "Install with: curl \"https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip\" -o \"awscliv2.zip\" && unzip awscliv2.zip && sudo ./aws/install"
        exit 1
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed. Please install .NET 8 first."
        print_status "Run: ./scripts/setup-dotnet-environment.sh"
        exit 1
    fi
    
    # Verify .NET 8
    DOTNET_VERSION=$(dotnet --version)
    if [[ ! "$DOTNET_VERSION" == 8.* ]]; then
        print_warning "Current .NET version is $DOTNET_VERSION. This deployment script is optimized for .NET 8."
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
    
    # Check AWS credentials
    if ! aws sts get-caller-identity &> /dev/null; then
        print_error "AWS credentials not configured or insufficient permissions."
        print_status "Configure with: aws configure"
        exit 1
    fi
    
    # Check if running from project root
    if [[ ! -f "CleanArchitecture.sln" ]]; then
        print_error "This script must be run from the project root directory (where CleanArchitecture.sln is located)."
        exit 1
    fi
    
    print_success "All prerequisites check passed"
    
    # Display configuration
    print_status "Deployment Configuration:"
    echo "  Project Name: $PROJECT_NAME"
    echo "  AWS Region: $REGION"
    echo "  ECR Repository: $ECR_REPO_NAME"
    echo "  AWS Account: $(aws sts get-caller-identity --query Account --output text)"
    echo ""
}

# Build and test the application
build_application() {
    print_header "Building and testing the application..."
    
    # Clean and restore
    print_status "Cleaning and restoring packages..."
    dotnet clean
    dotnet restore
    
    # Build
    print_status "Building solution..."
    if ! dotnet build --configuration Release --no-restore; then
        print_error "Build failed. Please fix build errors before deploying."
        exit 1
    fi
    
    # Run tests
    print_status "Running tests..."
    if ! dotnet test --configuration Release --no-build --logger "console;verbosity=normal"; then
        print_error "Tests failed. Please fix test failures before deploying."
        exit 1
    fi
    
    print_success "Application built and tested successfully"
}

# Deploy CloudFormation infrastructure
deploy_infrastructure() {
    print_header "Deploying CloudFormation infrastructure..."
    
    # Deploy ECR repository
    print_status "Deploying ECR repository..."
    aws cloudformation deploy \
        --template-file cloudformation/ecr.yaml \
        --stack-name "${STACK_PREFIX}-ecr" \
        --region "$REGION" \
        --parameter-overrides \
            RepositoryName="$ECR_REPO_NAME" \
        --tags \
            Project="$PROJECT_NAME" \
            Environment="Production" \
        --no-fail-on-empty-changeset
    
    if [[ $? -eq 0 ]]; then
        print_success "ECR repository deployed successfully"
    else
        print_error "ECR deployment failed"
        exit 1
    fi
    
    # Get ECR repository URI
    ECR_URI=$(aws cloudformation describe-stacks \
        --stack-name "${STACK_PREFIX}-ecr" \
        --region "$REGION" \
        --query 'Stacks[0].Outputs[?OutputKey==`RepositoryURI`].OutputValue' \
        --output text)
    
    if [[ -z "$ECR_URI" ]]; then
        print_error "Failed to get ECR repository URI"
        exit 1
    fi
    
    print_status "ECR Repository URI: $ECR_URI"
    
    # Deploy VPC and ALB
    print_status "Deploying VPC and Application Load Balancer..."
    aws cloudformation deploy \
        --template-file cloudformation/vpc-alb.yaml \
        --stack-name "${STACK_PREFIX}-vpc-alb" \
        --region "$REGION" \
        --parameter-overrides \
            ProjectName="$PROJECT_NAME" \
        --tags \
            Project="$PROJECT_NAME" \
            Environment="Production" \
        --no-fail-on-empty-changeset
    
    if [[ $? -eq 0 ]]; then
        print_success "VPC and ALB deployed successfully"
    else
        print_error "VPC/ALB deployment failed"
        exit 1
    fi
    
    print_success "Infrastructure deployment completed"
}

# Build and push Docker image
build_and_push_image() {
    print_header "Building and pushing Docker image..."
    
    # Login to ECR
    print_status "Logging in to Amazon ECR..."
    aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$ECR_URI"
    
    # Build Docker image
    print_status "Building Docker image for linux/amd64 platform..."
    docker build --platform linux/amd64 -t "$PROJECT_NAME" .
    
    if [[ $? -ne 0 ]]; then
        print_error "Docker build failed"
        exit 1
    fi
    
    # Tag image for ECR
    print_status "Tagging image for ECR..."
    docker tag "$PROJECT_NAME:latest" "$ECR_URI:latest"
    docker tag "$PROJECT_NAME:latest" "$ECR_URI:$(date +%Y%m%d-%H%M%S)"
    
    # Push image to ECR
    print_status "Pushing image to ECR..."
    docker push "$ECR_URI:latest"
    docker push "$ECR_URI:$(date +%Y%m%d-%H%M%S)"
    
    if [[ $? -eq 0 ]]; then
        print_success "Docker image pushed successfully"
        print_status "Image URI: $ECR_URI:latest"
    else
        print_error "Docker push failed"
        exit 1
    fi
}

# Deploy ECS service
deploy_ecs_service() {
    print_header "Deploying ECS Fargate service..."
    
    # Deploy ECS service
    print_status "Deploying ECS Fargate service..."
    aws cloudformation deploy \
        --template-file cloudformation/ecs-fargate.yaml \
        --stack-name "${STACK_PREFIX}-ecs" \
        --region "$REGION" \
        --parameter-overrides \
            ProjectName="$PROJECT_NAME" \
            ImageURI="$ECR_URI:latest" \
            VPCStackName="${STACK_PREFIX}-vpc-alb" \
        --capabilities CAPABILITY_IAM \
        --tags \
            Project="$PROJECT_NAME" \
            Environment="Production" \
        --no-fail-on-empty-changeset
    
    if [[ $? -eq 0 ]]; then
        print_success "ECS service deployed successfully"
    else
        print_error "ECS service deployment failed"
        exit 1
    fi
    
    # Get ALB DNS name
    ALB_DNS=$(aws cloudformation describe-stacks \
        --stack-name "${STACK_PREFIX}-vpc-alb" \
        --region "$REGION" \
        --query 'Stacks[0].Outputs[?OutputKey==`LoadBalancerDNS`].OutputValue' \
        --output text)
    
    if [[ -n "$ALB_DNS" ]]; then
        print_success "Application Load Balancer DNS: $ALB_DNS"
        print_status "Your API will be available at: http://$ALB_DNS"
        print_status "Swagger UI will be available at: http://$ALB_DNS/swagger"
        print_status "Health check: http://$ALB_DNS/health"
    fi
}

# Wait for service to be healthy
wait_for_service() {
    print_header "Waiting for service to become healthy..."
    
    if [[ -z "$ALB_DNS" ]]; then
        print_warning "ALB DNS not available, skipping health check"
        return
    fi
    
    print_status "Waiting for ECS service to start (this may take a few minutes)..."
    
    # Wait for ECS service to be stable
    aws ecs wait services-stable \
        --cluster "${PROJECT_NAME}-cluster" \
        --services "${PROJECT_NAME}-service" \
        --region "$REGION"
    
    print_success "ECS service is stable"
    
    # Check health endpoint
    print_status "Checking application health..."
    
    for i in {1..30}; do
        if curl -f -s "http://$ALB_DNS/health" > /dev/null; then
            print_success "Application is healthy!"
            break
        fi
        
        if [[ $i -eq 30 ]]; then
            print_warning "Health check timeout. The service may still be starting up."
            print_status "Check the service status in AWS Console or try accessing the endpoints manually."
            break
        fi
        
        print_status "Waiting for application to be ready... (attempt $i/30)"
        sleep 10
    done
}

# Display deployment summary
show_deployment_summary() {
    echo ""
    echo "============================================"
    print_success "🎉 Deployment completed successfully!"
    echo "============================================"
    echo ""
    echo -e "${BLUE}Deployment Summary:${NC}"
    echo ""
    echo "Project: $PROJECT_NAME"
    echo "Region: $REGION"
    echo "ECR Repository: $ECR_URI"
    
    if [[ -n "$ALB_DNS" ]]; then
        echo ""
        echo -e "${BLUE}Application Endpoints:${NC}"
        echo "  API Base URL: http://$ALB_DNS"
        echo "  Swagger UI: http://$ALB_DNS/swagger"
        echo "  Health Check: http://$ALB_DNS/health"
        echo "  Customers API: http://$ALB_DNS/api/v1/customers"
        echo "  Orders API: http://$ALB_DNS/api/v1/orders"
    fi
    
    echo ""
    echo -e "${BLUE}AWS Resources Created:${NC}"
    echo "  CloudFormation Stacks:"
    echo "    • ${STACK_PREFIX}-ecr (ECR Repository)"
    echo "    • ${STACK_PREFIX}-vpc-alb (VPC and Load Balancer)"
    echo "    • ${STACK_PREFIX}-ecs (ECS Fargate Service)"
    echo ""
    echo -e "${BLUE}Management Commands:${NC}"
    echo "  View logs:"
    echo "    aws logs describe-log-groups --region $REGION"
    echo "  Update service:"
    echo "    ./scripts/deploy-aws.sh"
    echo "  Delete deployment:"
    echo "    aws cloudformation delete-stack --stack-name ${STACK_PREFIX}-ecs --region $REGION"
    echo "    aws cloudformation delete-stack --stack-name ${STACK_PREFIX}-vpc-alb --region $REGION"
    echo "    aws cloudformation delete-stack --stack-name ${STACK_PREFIX}-ecr --region $REGION"
    echo ""
    print_success "🚀 Your Clean Architecture .NET 8 API is now running on AWS!"
}

# Cleanup function for failed deployments
cleanup_on_failure() {
    print_warning "Cleaning up resources due to deployment failure..."
    
    # This function could be enhanced to clean up partial deployments
    # For now, it just provides guidance
    echo ""
    echo "To clean up any partially created resources:"
    echo "1. Check CloudFormation stacks in AWS Console"
    echo "2. Delete stacks in reverse order: ECS -> VPC/ALB -> ECR"
    echo "3. Check ECR repository for any pushed images"
    echo ""
}

# Main execution
main() {
    print_status "Starting AWS deployment for Clean Architecture .NET 8..."
    
    # Set trap for cleanup on failure
    trap cleanup_on_failure ERR
    
    check_prerequisites
    build_application
    deploy_infrastructure
    build_and_push_image
    deploy_ecs_service
    wait_for_service
    show_deployment_summary
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --region)
            REGION="$2"
            shift 2
            ;;
        --project-name)
            PROJECT_NAME="$2"
            ECR_REPO_NAME="$PROJECT_NAME"
            shift 2
            ;;
        --ecr-repo)
            ECR_REPO_NAME="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --region REGION        AWS region (default: us-east-1)"
            echo "  --project-name NAME    Project name (default: clean-architecture-dotnet)"
            echo "  --ecr-repo NAME        ECR repository name (default: same as project name)"
            echo "  --help                 Show this help message"
            echo ""
            echo "Environment variables:"
            echo "  AWS_REGION             AWS region (overrides --region)"
            echo "  ECR_REPO               ECR repository name (overrides --ecr-repo)"
            echo ""
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Run main function
main "$@"