# Cloud9 Storage Management Guide

This guide covers how to manage storage in AWS Cloud9 environments, particularly for .NET development which requires significant disk space.

## 📊 Storage Requirements

**.NET Development Needs:**
- Base .NET 8 SDK: ~500MB
- NuGet packages: ~200-500MB  
- Build artifacts (bin/obj): ~100-300MB per project
- Docker images: ~500MB-1GB
- Development tools: ~200MB
- **Total recommended: 15-20GB minimum**

**Default Cloud9 Storage:**
- Standard instances: ~10GB total
- Available after OS: ~3-6GB
- ❌ **Insufficient for .NET development**

## 🔧 Solution Options

### Option 1: Resize Existing Root Volume (Recommended)
**Pros:** Simple, no data migration needed  
**Cons:** Limited by instance type maximums

### Option 2: Add Secondary EBS Volume
**Pros:** Can add large amounts of storage  
**Cons:** Requires data migration, more complex setup

---

## 🚀 Method 1: Resize Existing Root Volume (Easiest)

### Step 1: Resize Volume in AWS Console

1. **Find your Cloud9 instance:**
   - Go to **AWS Console → EC2 → Instances**
   - Find your Cloud9 instance (name usually contains "aws-cloud9-")
   - Note the **Instance ID** (i-xxxxxxxxx)

2. **Find the root volume:**
   - Go to **AWS Console → EC2 → Volumes**
   - Look for volume attached to your instance ID
   - It will be mounted as `/dev/sda1` or similar
   - Current size will show (usually 10GB)

3. **Resize the volume:**
   - Select the volume → **Actions → Modify Volume**
   - Change size from 10GB to **30GB** (or desired size)
   - Click **Modify**
   - Wait for status to change to "in-use - completed"

### Step 2: Extend Filesystem in Cloud9

```bash
# 1. Check current disk usage
df -h

# 2. Identify your filesystem type
lsblk -f

# 3. Check available space on the device
lsblk
```

### Step 3: Extend the Partition and Filesystem

**For newer instances (NVMe - most common):**
```bash
# Extend partition to use full disk
sudo growpart /dev/nvme0n1 1

# Extend filesystem (choose based on filesystem type):

# If filesystem is ext4:
sudo resize2fs /dev/nvme0n1p1

# If filesystem is XFS:
sudo xfs_growfs /

# Verify the new size
df -h
```

**For older instances (traditional naming):**
```bash
# Extend partition
sudo growpart /dev/xvda 1

# Extend filesystem:
# If ext4:
sudo resize2fs /dev/xvda1

# If XFS:
sudo xfs_growfs /

# Verify
df -h
```

### Step 4: Verify Success

```bash
# Check disk usage - should show increased space
df -h

# Check available space for your user
du -sh ~
```

You should now see ~25GB available space (from 30GB volume minus OS overhead).

---

## 🔗 Method 2: Add Secondary EBS Volume (Advanced)

### Step 1: Create New EBS Volume

1. **Go to AWS Console → EC2 → Volumes**
2. **Create Volume:**
   - Size: 20GB or more
   - Volume Type: gp3 (best performance/cost)
   - **Important:** Same Availability Zone as your Cloud9 instance
3. **Note the Volume ID** (vol-xxxxxxxxx)

### Step 2: Find Instance Information

1. **Get Instance ID:**
   - Go to **EC2 → Instances**
   - Find your Cloud9 instance
   - Note **Instance ID** (i-xxxxxxxxx)

### Step 3: Attach and Configure Volume

```bash
# Method A: Use our simplified script
sudo ./scripts/setup-cloud9-ebs.sh
# (Script will prompt for Volume ID and Instance ID)

# Method B: Manual commands
# Replace vol-xxx and i-xxx with your actual IDs
aws ec2 attach-volume --volume-id vol-xxxxxxxxx --instance-id i-xxxxxxxxx --device /dev/xvdf

# Wait for device to appear, then:
sudo mkfs.ext4 /dev/xvdf
sudo mkdir -p /mnt/ebs-storage
sudo mount /dev/xvdf /mnt/ebs-storage

# Add to /etc/fstab for persistence:
echo "/dev/xvdf /mnt/ebs-storage ext4 defaults,nofail 0 2" | sudo tee -a /etc/fstab

# Move development workspace:
sudo mkdir -p /mnt/ebs-storage/workspace
sudo chown ec2-user:ec2-user /mnt/ebs-storage/workspace
ln -s /mnt/ebs-storage/workspace ~/workspace-extended
```

---

## 🛠️ Troubleshooting

### "Bad magic number" Error
```bash
# Check filesystem type first:
lsblk -f

# Use correct resize command:
# For XFS: sudo xfs_growfs /
# For ext4: sudo resize2fs /dev/nvme0n1p1
```

### Device Not Found
```bash
# Check what devices exist:
lsblk

# Common device names:
# - /dev/nvme0n1p1 (newer instances)
# - /dev/xvda1 (older instances)
# - /dev/xvdf (additional volumes)
```

### Partition Doesn't Extend
```bash
# Reboot instance and try again:
sudo reboot

# Or force kernel to re-read partition table:
sudo partprobe /dev/nvme0n1
```

### Check Disk Space
```bash
# Overall disk usage:
df -h

# Directory usage:
du -sh /home/ec2-user/*

# Find large files:
sudo find / -size +100M -type f 2>/dev/null
```

---

## 📋 Quick Reference Commands

### Check Current Storage
```bash
df -h                    # Show disk usage
lsblk                    # Show block devices
lsblk -f                 # Show filesystems
du -sh ~                 # Your home directory size
```

### Extend Root Volume (after AWS resize)
```bash
# For NVMe instances (most common):
sudo growpart /dev/nvme0n1 1
sudo resize2fs /dev/nvme0n1p1    # ext4
sudo xfs_growfs /                # XFS

# For traditional instances:
sudo growpart /dev/xvda 1
sudo resize2fs /dev/xvda1        # ext4
sudo xfs_growfs /                # XFS
```

### Clean Up Space
```bash
# Clean .NET build artifacts:
find ~/source -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find ~/source -name "obj" -type d -exec rm -rf {} + 2>/dev/null

# Clean package caches:
dotnet nuget locals all --clear

# Clean Docker (if using):
docker system prune -a
```

---

## 💡 Best Practices

1. **Start with 30GB root volume** - Usually sufficient for .NET development
2. **Monitor disk usage** - Use `df -h` regularly
3. **Clean build artifacts** - Run `make clean` periodically
4. **Use .gitignore properly** - Exclude bin/, obj/, node_modules/
5. **Regular cleanup** - Clear NuGet caches and Docker images

## 🎯 Recommended Setup for .NET Development

```bash
# 1. Resize root volume to 30GB (via AWS Console)
# 2. Extend filesystem:
sudo growpart /dev/nvme0n1 1
sudo resize2fs /dev/nvme0n1p1    # or sudo xfs_growfs /

# 3. Verify space:
df -h

# 4. Setup .NET development:
./scripts/setup-cloud9-dotnet.sh

# 5. Start developing:
make setup-dev
make run-watch
```

This gives you ~25GB of usable space, which is plenty for multiple .NET projects, Docker containers, and development tools.