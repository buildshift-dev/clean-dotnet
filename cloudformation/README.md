# Clean Architecture .NET - AWS ECS Deployment

This directory contains CloudFormation templates and deployment scripts **exclusively for deploying to AWS ECS Fargate**.

**⚠️ Not for Local Development**: For local development, see the main [README.md](../README.md).

## Architecture Overview

The deployment consists of:
- **ECR Repository**: Stores the Docker container images
- **VPC with ALB**: Network infrastructure with Application Load Balancer
- **ECS Fargate**: Container orchestration running .NET 8 API

## Files

### CloudFormation Templates
- `ecr.yaml` - ECR repository for container images
- `vpc-alb.yaml` - VPC, subnets, NAT gateway, ALB, and security groups
- `ecs-fargate.yaml` - ECS cluster, service, task definition, and ALB listeners

### AWS ECS Deployment Scripts
- `build-and-deploy.sh` - **Complete AWS ECS Fargate deployment script**
- `cleanup.sh` - **Cleanup script to remove all AWS resources**

**⚠️ Important**: These scripts are **exclusively for AWS ECS deployment**. For local development, see the main README.md.

## Platform-Specific Deployment Guide

Choose your deployment approach based on your development platform:

### 🍎 **macOS Apple Silicon (M1/M2/M3)**
**⚠️ Docker cross-compilation issues require special approach**

```bash
# Set environment variables
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet
export AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)

# 1. Deploy infrastructure (works normally)
make deploy-cloudformation

# 2. Build using prebuild approach (avoids cross-compilation)
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish
docker build --platform linux/amd64 -f Dockerfile.prebuild -t $ECR_REPO .

# 3. Push to ECR
ECR_URI=${AWS_ACCOUNT}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPO}
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_URI
docker tag $ECR_REPO:latest $ECR_URI:latest
docker push $ECR_URI:latest

# 4. Deploy ECS service
aws cloudformation deploy \
    --template-file cloudformation/ecs-fargate.yaml \
    --stack-name clean-arch-ecs \
    --capabilities CAPABILITY_IAM \
    --region $AWS_REGION \
    --parameter-overrides ImageUri=$ECR_URI:latest
```

### 💻 **macOS Intel / Linux Intel/AMD / Windows Intel/AMD**
**✅ Standard automated deployment works**

```bash
# Set environment variables
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet

# One-command deployment
make deploy
```

### ☁️ **AWS Cloud9 (Amazon Linux)**
**✅ Standard automated deployment works (native AMD64)**

```bash
# Set environment variables
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet

# One-command deployment
make deploy

# OR use script directly
cloudformation/build-and-deploy.sh
```

### 🔧 **Alternative: Manual Step-by-Step (Any Platform)**
**📖 See [manual-deployment.md](manual-deployment.md) for detailed instructions**


## Legacy Script Deployment (Intel/AMD Only)

**⚠️ These scripts only work on Intel/AMD systems. Apple Silicon users must use the manual approach above.**

### Deploy to AWS ECS Fargate
```bash
# Make scripts executable
chmod +x cloudformation/build-and-deploy.sh cloudformation/cleanup.sh

# Deploy to AWS ECS in dev environment (us-east-1)
cloudformation/build-and-deploy.sh

# Deploy to AWS ECS in specific environment and region
cloudformation/build-and-deploy.sh prod us-west-2
```

---

## Redeploy After Code Changes

### 🍎 **macOS Apple Silicon (M1/M2/M3)**:
```bash
# 1. Build and push using prebuild approach
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish
docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-architecture-dotnet .

# 2. Get ECR URI and push
ECR_URI=$(aws ecr describe-repositories --repository-names $ECR_REPO --region $AWS_REGION --query 'repositories[0].repositoryUri' --output text)
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_URI
docker tag clean-architecture-dotnet:latest $ECR_URI:latest
docker push $ECR_URI:latest

# 3. Force ECS service restart
CLUSTER_NAME=$(aws cloudformation describe-stacks --stack-name clean-arch-ecs --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ClusterName`].OutputValue' --output text)
SERVICE_NAME=$(aws cloudformation describe-stacks --stack-name clean-arch-ecs --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ServiceName`].OutputValue' --output text)
aws ecs update-service --cluster $CLUSTER_NAME --service $SERVICE_NAME --force-new-deployment --region $AWS_REGION
```

### 💻 **macOS Intel / Linux / Windows**:
```bash
# Use automated script
cloudformation/build-and-deploy.sh
```

### ☁️ **AWS Cloud9**:
```bash
# Use automated script (works natively on AMD64)
cloudformation/build-and-deploy.sh
```


## Access the Application

After deployment, get the URLs:

```bash
# API Base URL
aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-dev-cleanarch-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`APIURL`].OutputValue' \
    --output text

# Swagger UI URL
aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-dev-cleanarch-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`SwaggerURL`].OutputValue' \
    --output text

# Health Check URL
aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-dev-cleanarch-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`HealthCheckURL`].OutputValue' \
    --output text
```

## API Endpoints

The ALB routes all traffic to the .NET API running on port 8080:
- `/` → Swagger UI documentation (root serves Swagger)
- `/api/v1/customers` → Customer management endpoints
- `/api/v1/orders` → Order management endpoints
- `/health` → Health check endpoint

### Available Endpoints
- `POST /api/v1/customers` - Create a new customer
- `POST /api/v1/orders` - Create a new order
- `GET /api/v1/orders/customer/{customerId}` - Get orders for a customer
- `GET /health` - Health check

## Cleanup

To remove all resources:
```bash
cloudformation/cleanup.sh

# Or for specific environment/region
cloudformation/cleanup.sh prod us-west-2
```

## Cost Considerations

This deployment includes:
- **NAT Gateway**: ~$45/month (required for private subnet internet access)
- **Application Load Balancer**: ~$22/month
- **ECS Fargate**: ~$15/month for 1 task (0.5 vCPU, 1GB RAM)
- **CloudWatch Logs**: Minimal cost
- **ECR**: $0.10/GB/month for stored images

**Total estimated cost**: ~$82/month for dev environment

## Troubleshooting

### Common Issues

1. **Stack in ROLLBACK_COMPLETE state**
   ```bash
   # Delete the failed stack and redeploy
   aws cloudformation delete-stack --stack-name clean-dotnet-dev-cleanarch-ecs
   aws cloudformation wait stack-delete-complete --stack-name clean-dotnet-dev-cleanarch-ecs
   # Then redeploy using build-and-deploy.sh or manual steps
   ```

2. **Missing CloudFormation exports**
   - Ensure VPC stack deployed successfully: `aws cloudformation describe-stacks --stack-name clean-dotnet-dev-cleanarch-vpc-alb`
   - Check exports are available: `aws cloudformation list-exports --query 'Exports[?contains(Name, \`dev-cleanarch\`)].Name'`
   - Deploy stacks in correct order: ECR → VPC → ECS

3. **ECS Service fails to start**
   - Check CloudWatch logs: `/ecs/dev-cleanarch`
   - Verify security groups allow ALB → ECS communication on port 8080
   - Check task definition resource allocation

4. **Container architecture mismatch (Apple Silicon Macs)**
   ```
   Error: exec /usr/bin/dotnet: exec format error
   assertion failed [true_path_length_self >= 0]: true_path_length_self underflowed!
   (ThreadContextFcntl.cpp:178 is_rosetta_process)
   ```
   - This occurs due to Rosetta emulation issues when cross-compiling .NET on Apple Silicon
   - **Solution**: Use the prebuild approach:
     ```bash
     # Build .NET natively first, then package in AMD64 container
     dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish
     docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-architecture-dotnet .
     ```

5. **ALB health checks fail**
   - Ensure .NET app binds to `0.0.0.0:8080` not `127.0.0.1`
   - Check health check path: `/health`
   - Verify container port matches ALB target group port (8080)

3. **New code not visible after deployment**
   - ECS doesn't automatically restart when pushing new images with same tag
   - Force new deployment: `aws ecs update-service --cluster dev-cleanarch-cluster --service dev-cleanarch-service --force-new-deployment`
   - Clear browser cache (Ctrl+F5 / Cmd+Shift+R)
   - Wait 3-5 minutes for deployment to complete

4. **Docker build fails**
   - Ensure you're in the project root directory
   - Check Dockerfile paths and .NET SDK installation
   - Verify all project files are present

5. **.NET build fails**
   - Ensure .NET 8 SDK is installed: `dotnet --version`
   - Add .NET to PATH: `export PATH="$HOME/.dotnet:$PATH"`
   - Run `dotnet restore` and `dotnet build` manually

### Useful Commands

```bash
# Check ECS service status
aws ecs describe-services \
    --cluster dev-cleanarch-cluster \
    --services dev-cleanarch-service

# View ECS service logs
aws logs tail /ecs/dev-cleanarch --follow

# Force deployment with latest container image
aws ecs update-service \
    --cluster dev-cleanarch-cluster \
    --service dev-cleanarch-service \
    --force-new-deployment

# Check deployment progress
aws ecs describe-services \
    --cluster dev-cleanarch-cluster \
    --services dev-cleanarch-service \
    --query 'services[0].deployments[?status==`PRIMARY`].{Status:rolloutState,Updated:updatedAt}' \
    --output table

# Test API endpoints
API_URL="http://your-alb-dns-name"
curl -X POST "$API_URL/api/v1/customers" \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john.doe@example.com"}'
```

## Security Notes

- ECS tasks run in private subnets with no direct internet access
- ALB is internet-facing but access is controlled by security groups
- All communication between ALB and ECS is within the VPC on port 8080
- ECR images are scanned for vulnerabilities
- Task execution role has minimal required permissions

## Customization

### Environment Variables
Add environment variables to the task definition in `ecs-fargate.yaml`:
```yaml
Environment:
  - Name: ASPNETCORE_ENVIRONMENT
    Value: !Ref EnvironmentName
  - Name: DATABASE_URL
    Value: "your-database-url"
```

### Scaling
Modify auto-scaling parameters in `ecs-fargate.yaml`:
- `MinCapacity`: Minimum number of tasks
- `MaxCapacity`: Maximum number of tasks
- CPU/Memory thresholds for scaling triggers

### Custom Domain
To use a custom domain:
1. Add SSL certificate ARN to ALB listener
2. Create Route53 record pointing to ALB
3. Update security groups if needed

## Differences from Python Version

This .NET deployment differs from the Python version in several ways:
- **Single container**: Only runs .NET API (no separate Streamlit app)
- **Port 8080**: .NET app runs on port 8080 instead of 8000/8501
- **Swagger UI**: API documentation served at root URL (`/`) instead of `/swagger`
- **Health endpoint**: Uses `/health` instead of application-specific checks
- **Platform targeting**: Built specifically for linux/amd64 platform

## Testing the Deployment

Once deployed, test the API:

```bash
# Get the API URL
API_URL=$(aws cloudformation describe-stacks \
    --stack-name "clean-dotnet-dev-cleanarch-ecs" \
    --query 'Stacks[0].Outputs[?OutputKey==`APIURL`].OutputValue' \
    --output text)

# Test health endpoint
curl "$API_URL/health"

# Create a customer
curl -X POST "$API_URL/api/v1/customers" \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john.doe@example.com"}' | jq

# Create an order (use the customer ID from above)
curl -X POST "$API_URL/api/v1/orders" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"CUSTOMER_ID_HERE","totalAmount":99.99,"currency":"USD"}' | jq
```