# Manual AWS Deployment Guide

This guide provides step-by-step instructions for manually deploying the Clean Architecture .NET 8 application to AWS ECS Fargate.

## Prerequisites

- AWS CLI configured with appropriate permissions
- Docker installed and running
- .NET 8 SDK installed

## Container Build Guide

### 🖥️ Local Development Testing

For **local testing** on your development machine (before AWS deployment):

#### 🍎 **macOS Apple Silicon (M1/M2/M3)**:
```bash
# Build natively for local testing (ARM64)
docker build -t clean-architecture-dotnet .

# Run locally
docker run --rm -p 8080:8080 clean-architecture-dotnet

# Test health endpoint
curl http://localhost:8080/health
```

#### 💻 **macOS Intel / Linux / Windows**:
```bash
# Build for your native platform (fastest)
docker build -t clean-architecture-dotnet .

# Run locally
docker run --rm -p 8080:8080 clean-architecture-dotnet

# Test health endpoint
curl http://localhost:8080/health
```

#### ☁️ **AWS Cloud9**:
```bash
# Build natively (AMD64)
docker build -t clean-architecture-dotnet .

# Run locally
docker run --rm -p 8080:8080 clean-architecture-dotnet

# Test health endpoint (use Cloud9 preview)
curl http://localhost:8080/health
```

---

### ☁️ AWS ECS Deployment

For **AWS ECS Fargate deployment**, containers must be built for **linux/amd64** platform:

#### Intel/AMD Systems:
```bash
# Build for AWS (linux/amd64)
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```

#### Apple Silicon (M1/M2) Mac:

**Option 1: Docker Desktop with containerd (Recommended)**
```bash
# Enable in Docker Desktop: Settings > Features in development > Use containerd
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```

**Option 2: Use buildx with QEMU emulation**
```bash
docker buildx create --use --name multiarch
docker buildx build --platform linux/amd64 -t clean-architecture-dotnet .
```

**Option 3: GitHub Actions/CodeBuild**
Use CI/CD to build on AMD64 runners, then deploy to AWS.

---

### Platform Summary

| Platform | Local Testing | AWS ECS Deployment |
|----------|---------------|--------------------|
| **🍎 macOS Apple Silicon (M1/M2/M3)** | `docker build -t ...` | **Prebuild required**: `dotnet publish` → `docker build -f Dockerfile.prebuild` |
| **💻 macOS Intel** | `docker build -t ...` | `docker build --platform linux/amd64 -t ...` |
| **🐧 Linux Intel/AMD** | `docker build -t ...` | `docker build --platform linux/amd64 -t ...` |
| **🫟 Windows Intel/AMD** | `docker build -t ...` | `docker build --platform linux/amd64 -t ...` |
| **☁️ AWS Cloud9** | `docker build -t ...` | `docker build --platform linux/amd64 -t ...` |

### Platform Compatibility Notes

**Apple Silicon Users**: If experiencing Rosetta emulation issues when building linux/amd64:

## Manual Deployment Steps

### Step 1: Set Environment Variables

```bash
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet
export AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
```

### Step 2: Deploy Infrastructure

```bash
# Deploy ECR repository
aws cloudformation deploy \
  --template-file ecr.yaml \
  --stack-name clean-arch-ecr \
  --region $AWS_REGION

# Deploy VPC and ALB
aws cloudformation deploy \
  --template-file vpc-alb.yaml \
  --stack-name clean-arch-vpc-alb \
  --region $AWS_REGION

# Deploy ECS Fargate service
aws cloudformation deploy \
  --template-file ecs-fargate.yaml \
  --stack-name clean-arch-ecs \
  --capabilities CAPABILITY_IAM \
  --region $AWS_REGION \
  --parameter-overrides \
    ImageUri=${AWS_ACCOUNT}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPO}:latest
```

### Step 3: Build and Push Docker Image

**⚠️ Platform-Specific Commands for AWS ECS Fargate:**

#### 🍎 **macOS Apple Silicon (M1/M2/M3)** - PREBUILD APPROACH REQUIRED:
```bash
# ⚠️ Standard docker build fails due to Rosetta emulation issues
# Use prebuild approach instead:

# Step 1: Build .NET natively on your Mac
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish

# Step 2: Build AMD64 container using pre-built binaries
docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-architecture-dotnet .
```
**Why this works**: Builds .NET natively on Apple Silicon, then packages binaries in AMD64 container (avoids cross-compilation).

#### 💻 **macOS Intel**:
```bash
# Build for ECS Fargate (native AMD64 platform)
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```
**Note**: Works normally since Intel Macs are AMD64 systems.

#### 🐧 **Linux (Intel/AMD)**:
```bash
# Build for ECS Fargate (native AMD64 platform)
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```
**Note**: Works normally on AMD64 Linux systems.

#### 🫟 **Windows (Intel/AMD)**:
```bash
# Build for ECS Fargate (native AMD64 platform)
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```
**Note**: Works normally with Docker Desktop on AMD64 Windows.

#### ☁️ **AWS Cloud9 (Amazon Linux)**:
```bash  
# Build for ECS Fargate (native AMD64 platform)
docker build --platform linux/amd64 -t clean-architecture-dotnet .
```
**Note**: Cloud9 runs on AMD64, so this builds natively without emulation.

---

#### **Complete Push Process (All Platforms)**:

```bash
# Get ECR repository URI
ECR_URI=$(aws ecr describe-repositories \
  --repository-names $ECR_REPO \
  --region $AWS_REGION \
  --query 'repositories[0].repositoryUri' \
  --output text)

echo "ECR URI: $ECR_URI"

# Login to ECR
aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin $ECR_URI

# Tag and push image
docker tag clean-architecture-dotnet:latest $ECR_URI:latest
docker push $ECR_URI:latest

echo "✅ Image pushed successfully to $ECR_URI:latest"
```

#### **Alternative for Apple Silicon (if build fails)**:

```bash
# Use buildx with direct push (slower but reliable)
docker buildx create --use --name multiarch --driver docker-container

ECR_URI=$(aws ecr describe-repositories \
  --repository-names $ECR_REPO \
  --region $AWS_REGION \
  --query 'repositories[0].repositoryUri' \
  --output text)

aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin $ECR_URI

docker buildx build \
  --platform linux/amd64 \
  --tag $ECR_URI:latest \
  --push .
```

### Step 4: Update ECS Service

```bash
# Force new deployment to pick up the latest image
CLUSTER_NAME=$(aws cloudformation describe-stacks \
  --stack-name clean-arch-ecs \
  --region $AWS_REGION \
  --query 'Stacks[0].Outputs[?OutputKey==`ClusterName`].OutputValue' \
  --output text)

SERVICE_NAME=$(aws cloudformation describe-stacks \
  --stack-name clean-arch-ecs \
  --region $AWS_REGION \
  --query 'Stacks[0].Outputs[?OutputKey==`ServiceName`].OutputValue' \
  --output text)

aws ecs update-service \
  --cluster $CLUSTER_NAME \
  --service $SERVICE_NAME \
  --force-new-deployment \
  --region $AWS_REGION
  
```

### Step 5: Get Application URL

```bash
# Get the Load Balancer DNS name
ALB_DNS=$(aws cloudformation describe-stacks \
  --stack-name clean-arch-vpc-alb \
  --region $AWS_REGION \
  --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerDNS`].OutputValue' \
  --output text)

echo "Application URL: http://$ALB_DNS"
echo "Health Check: http://$ALB_DNS/health"
echo "Swagger UI: http://$ALB_DNS/ (root serves Swagger UI)"
```


## Troubleshooting

### Container Keeps Stopping

1. **Check ECS Service Events:**
   ```bash
   aws ecs describe-services \
     --cluster $CLUSTER_NAME \
     --services $SERVICE_NAME \
     --region $AWS_REGION
   ```

2. **Check CloudWatch Logs:**
   ```bash
   aws logs describe-log-groups \
     --log-group-name-prefix /ecs/clean-arch \
     --region $AWS_REGION
   ```

3. **Check Task Definition:**
   ```bash
   aws ecs describe-task-definition \
     --task-definition clean-arch-task \
     --region $AWS_REGION
   ```

### Common Issues

1. **Platform Mismatch:** Ensure Docker image is built for linux/amd64
2. **Health Check Failures:** Verify `/health` endpoint responds with 200
3. **Port Configuration:** Ensure container exposes port 8080
4. **Environment Variables:** Check ASPNETCORE_URLS is set correctly

### Health Check Commands

```bash
# Test health endpoint locally
curl -f http://localhost:8080/health

# Test in container
docker run --rm -p 8080:8080 clean-architecture-dotnet &
sleep 10
curl -f http://localhost:8080/health
```

## Cleanup

```bash
# Delete all stacks (in reverse order)
aws cloudformation delete-stack --stack-name clean-arch-ecs --region $AWS_REGION
aws cloudformation delete-stack --stack-name clean-arch-vpc-alb --region $AWS_REGION
aws cloudformation delete-stack --stack-name clean-arch-ecr --region $AWS_REGION

# Wait for deletion to complete
aws cloudformation wait stack-delete-complete --stack-name clean-arch-ecs --region $AWS_REGION
aws cloudformation wait stack-delete-complete --stack-name clean-arch-vpc-alb --region $AWS_REGION
aws cloudformation wait stack-delete-complete --stack-name clean-arch-ecr --region $AWS_REGION
```

## Using Make Commands (When Working)

If the platform issues are resolved, you can use the simplified Make commands:

```bash
# Set environment variables
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet

# Deploy infrastructure
make deploy-cloudformation

# Build and deploy application
make deploy
```

## Alternative: GitHub Actions Deployment

For Apple Silicon users, consider using the GitHub Actions workflow in `.github/workflows/` which builds on AMD64 runners and deploys automatically.