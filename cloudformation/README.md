# AWS Deployment Guide

Deploy the Clean Architecture .NET 8 API to AWS ECS Fargate. Choose your platform below.

> **📝 Note:** All commands in this guide should be run from the **project root directory**, not from the `cloudformation/` directory.

## 🍎 For Mac Silicon (M1/M2/M3)

### Prerequisites
- AWS CLI configured (`aws configure`)
- Docker Desktop running

### Step 1: Deploy Infrastructure (5 minutes)
```bash
export AWS_REGION=us-east-1

aws cloudformation deploy --template-file cloudformation/ecr.yaml --stack-name clean-arch-ecr --region $AWS_REGION
aws cloudformation deploy --template-file cloudformation/vpc-alb.yaml --stack-name clean-arch-vpc-alb --region $AWS_REGION
```

### Step 2: Build and Push (Mac Silicon way)
```bash
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish
docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-dotnet .

# Bulletproof ECR push (handles any shell environment issues)
cat > /tmp/ecr_push.sh << 'EOF'
#!/bin/bash
set -e

# Get ECR URI and build it manually to avoid corruption
RAW_URI=$(aws cloudformation describe-stacks --stack-name clean-arch-ecr --region us-east-1 --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryURI`].OutputValue' --output text)
ACCOUNT_ID=$(echo "$RAW_URI" | cut -d'.' -f1)
REGION="us-east-1"
REPO_NAME="clean-dotnet-dev-cleanarch-api"
CLEAN_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPO_NAME}"

echo "Using ECR URI: $CLEAN_URI"

# Login, tag, and push
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin "$CLEAN_URI"
docker tag clean-dotnet:latest "$CLEAN_URI:latest"
docker push "$CLEAN_URI:latest"
EOF

# Run the bulletproof script
bash /tmp/ecr_push.sh
rm /tmp/ecr_push.sh
```

### Step 3: Deploy ECS (8 minutes)
```bash
aws cloudformation deploy \
  --template-file cloudformation/ecs.yaml \
  --stack-name clean-arch-ecs \
  --capabilities CAPABILITY_IAM \
  --region $AWS_REGION \
  --parameter-overrides \
    ImageURI=$(aws cloudformation describe-stacks --stack-name clean-arch-ecr --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryURI`].OutputValue' --output text):latest

# Get your URL
echo "Your API: http://$(aws cloudformation describe-stacks --stack-name clean-arch-vpc-alb --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerDNS`].OutputValue' --output text)/"
```

---

## ☁️ Cloud9/AMD64 Deployment

### Prerequisites
- AWS CLI configured (automatic in Cloud9)
- Docker running

### Step 1: Deploy Infrastructure (5 minutes)
```bash
export AWS_REGION=us-east-1

aws cloudformation deploy --template-file cloudformation/ecr.yaml --stack-name clean-arch-ecr --region $AWS_REGION
aws cloudformation deploy --template-file cloudformation/vpc-alb.yaml --stack-name clean-arch-vpc-alb --region $AWS_REGION
```

### Step 2: Build and Push (AMD64 way - much simpler!)
```bash
docker build --platform linux/amd64 -t clean-dotnet .

# Bulletproof ECR push (handles any shell environment issues)
cat > /tmp/ecr_push.sh << 'EOF'
#!/bin/bash
set -e

# Get ECR URI and build it manually to avoid corruption
RAW_URI=$(aws cloudformation describe-stacks --stack-name clean-arch-ecr --region us-east-1 --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryURI`].OutputValue' --output text)
ACCOUNT_ID=$(echo "$RAW_URI" | cut -d'.' -f1)
REGION="us-east-1"
REPO_NAME="clean-dotnet-dev-cleanarch-api"
CLEAN_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPO_NAME}"

echo "Using ECR URI: $CLEAN_URI"

# Login, tag, and push
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin "$CLEAN_URI"
docker tag clean-dotnet:latest "$CLEAN_URI:latest"
docker push "$CLEAN_URI:latest"
EOF

# Run the bulletproof script
bash /tmp/ecr_push.sh
rm /tmp/ecr_push.sh
```

### Step 3: Deploy ECS (8 minutes)
```bash
aws cloudformation deploy \
  --template-file cloudformation/ecs.yaml \
  --stack-name clean-arch-ecs \
  --capabilities CAPABILITY_IAM \
  --region $AWS_REGION \
  --parameter-overrides \
    ImageURI=$(aws cloudformation describe-stacks --stack-name clean-arch-ecr --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryURI`].OutputValue' --output text):latest

# Get your URL
echo "Your API: http://$(aws cloudformation describe-stacks --stack-name clean-arch-vpc-alb --region $AWS_REGION --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerDNS`].OutputValue' --output text)/"
```

---

## 🧹 Clean Up

When you're done testing:

```bash
export AWS_REGION=us-east-1

aws cloudformation delete-stack --stack-name clean-arch-ecs --region $AWS_REGION
aws cloudformation delete-stack --stack-name clean-arch-vpc-alb --region $AWS_REGION  
aws cloudformation delete-stack --stack-name clean-arch-ecr --region $AWS_REGION
```

---

## 🔧 If Something Goes Wrong

1. **Check stack status:**
   ```bash
   aws cloudformation describe-stacks --stack-name STACK-NAME --region us-east-1
   ```

2. **Check application logs:**
   ```bash
   aws logs tail /ecs/dev-cleanarch --region us-east-1 --follow
   ```

3. **Test Docker locally first:**
   ```bash
   docker run --rm -p 8080:8080 clean-dotnet
   curl http://localhost:8080/health
   ```

**Start over:** Delete all stacks and run the steps again.

---

## Key Differences

| Platform | Docker Build | Why |
|----------|--------------|-----|
| **🍎 Mac Silicon** | `dotnet publish` + `Dockerfile.prebuild` | Avoids Rosetta emulation issues |
| **☁️ Cloud9/AMD64** | Standard `Dockerfile` | Native AMD64, no cross-compilation needed |

Both approaches deploy to the same ECS infrastructure and work identically once deployed!

# This duplicate section was removed - use the Cloud9/AMD64 deployment section above

---

## 🧹 Clean Up (Delete Everything)

When you're done testing, delete all resources to avoid charges:

```bash
export AWS_REGION=us-east-1

# Delete in reverse order
aws cloudformation delete-stack --stack-name clean-arch-ecs --region $AWS_REGION
aws cloudformation delete-stack --stack-name clean-arch-vpc-alb --region $AWS_REGION  
aws cloudformation delete-stack --stack-name clean-arch-ecr --region $AWS_REGION
```

---

## 🔧 Troubleshooting

**If something fails:**

1. **Check stack status:**
   ```bash
   aws cloudformation describe-stacks --stack-name STACK-NAME --region us-east-1
   ```

2. **Check application logs:**
   ```bash
   aws logs tail /ecs/dev-cleanarch --region us-east-1 --follow
   ```

3. **Test Docker image locally:**
   ```bash
   docker run --rm -p 8080:8080 clean-dotnet
   curl http://localhost:8080/health
   ```

**Start over:** Delete all stacks and run the steps again.

---

## That's it! 🎉

Three simple steps to deploy a production-ready .NET 8 API on AWS ECS Fargate with load balancer, auto-scaling, and monitoring.