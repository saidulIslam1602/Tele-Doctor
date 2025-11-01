# AWS Infrastructure

## Overview

This directory contains Terraform code for deploying TeleDoctor infrastructure on Amazon Web Services (AWS) as an alternative cloud platform and multi-cloud comparison.

## Architecture

### Network Design

```
AWS VPC (10.30.0.0/16)
├── Private Subnets (3 AZs)
│   ├── 10.30.0.0/24 (eu-north-1a)
│   ├── 10.30.1.0/24 (eu-north-1b)
│   └── 10.30.2.0/24 (eu-north-1c)
├── Public Subnets (3 AZs)
│   ├── 10.30.100.0/24 (eu-north-1a)
│   ├── 10.30.101.0/24 (eu-north-1b)
│   └── 10.30.102.0/24 (eu-north-1c)
└── NAT Gateways (3 AZs for HA)

Connectivity to Azure:
├── Transit Gateway (BGP ASN: 64512)
└── Site-to-Site VPN with BGP
```

### Components

**Compute**:
- EKS cluster (Kubernetes 1.28)
- Multi-AZ node group (3-10 nodes)
- t3.large instances

**Data**:
- RDS PostgreSQL 15.4 (Multi-AZ)
- ElastiCache Redis (HA with replication)
- Automated backups (30-day retention)

**Networking**:
- VPC with public and private subnets
- Transit Gateway for hybrid connectivity
- Site-to-Site VPN with BGP to Azure
- NAT Gateways for internet access

**Security**:
- Private EKS cluster
- Security groups (least privilege)
- KMS encryption at rest
- VPC Flow Logs
- CloudWatch logging

## BGP Configuration

### Transit Gateway

**ASN**: 64512 (AWS side)  
**Purpose**: Central routing hub for multi-VPC and hybrid  
**Routing**: BGP with Azure (ASN: 65515)

### VPN Connection

**Type**: IPsec with BGP  
**Redundancy**: Dual tunnels  
**Routing**: Dynamic via BGP  
**Encryption**: AES-256

## Comparison with Azure and GCP

| Feature | Azure | GCP | AWS |
|---------|-------|-----|-----|
| **Network** | VNet | VPC | VPC |
| **Kubernetes** | AKS | GKE | EKS |
| **Database** | Azure SQL | Cloud SQL | RDS |
| **Cache** | Azure Redis | Memorystore | ElastiCache |
| **Hybrid** | ExpressRoute + VPN | Interconnect + VPN | Direct Connect + VPN |
| **Transit** | Virtual WAN | Cloud Router | Transit Gateway |
| **BGP ASN** | 65515 | 65516 | 64512 |
| **Firewall** | Azure Firewall | VPC Firewall | Security Groups |

## Deployment

### Prerequisites

```bash
# Install AWS CLI
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# Configure AWS credentials
aws configure
```

### Deploy Infrastructure

```bash
cd infrastructure/terraform/multi-cloud/aws

# Initialize
terraform init

# Plan
terraform plan \
  -var="environment=production" \
  -var="kms_key_arn=arn:aws:kms:..."

# Apply
terraform apply \
  -var="environment=production"
```

### Connect to EKS

```bash
# Get cluster credentials
aws eks update-kubeconfig \
  --region eu-north-1 \
  --name teledoctor-eks

# Verify connection
kubectl get nodes
```

## Transit Gateway Setup

### Connecting to Azure

1. **Create Transit Gateway** (AWS side)
```bash
terraform apply -var="enable_transit_gateway=true"
```

2. **Create Customer Gateway** (represents Azure)
```bash
terraform apply \
  -var="enable_site_to_site_vpn=true" \
  -var="azure_vpn_gateway_ip=X.X.X.X"
```

3. **Configure Azure side**
```bash
# In Azure, create Local Network Gateway pointing to AWS
az network local-gateway create \
  --name lng-aws \
  --gateway-ip-address AWS_TRANSIT_GATEWAY_IP \
  --local-address-prefixes 10.30.0.0/16
```

4. **Establish BGP peering**
- AWS announces: 10.30.0.0/16
- Azure announces: 10.0.0.0/8
- BGP session over IPsec tunnels

## Multi-AZ High Availability

### Design

**3 Availability Zones**:
- eu-north-1a (primary)
- eu-north-1b (secondary)
- eu-north-1c (tertiary)

**Distribution**:
- EKS nodes spread across all AZs
- RDS Multi-AZ with automatic failover
- ElastiCache with read replicas in each AZ
- NAT Gateways in each AZ

**SLA**: 99.99% (Four nines with Multi-AZ)

## Security

### Network Security

- Private subnets for all workloads
- Security groups (default deny)
- NACLs for subnet-level filtering
- VPC Flow Logs enabled

### Data Security

- KMS encryption for RDS and ElastiCache
- Encryption in transit (TLS/SSL)
- Private endpoints only
- No public access to databases

### Compliance

- VPC Flow Logs for audit
- CloudWatch Logs retention (90 days)
- Encrypted storage
- IAM roles with least privilege

## Monitoring

**CloudWatch**:
- EKS cluster logs
- RDS performance insights
- ElastiCache metrics
- VPC Flow Logs

**Metrics**:
- CPU utilization
- Memory usage
- Network throughput
- Database connections
- Cache hit ratio

## Cost Estimate

**Monthly Cost (Production)**:
- EKS (3 x t3.large): $220
- RDS Multi-AZ (db.t3.large): $280
- ElastiCache (2 x cache.t3.medium): $100
- NAT Gateways (3 x): $100
- Transit Gateway: $35
- Data transfer: $50-100
- **Total**: ~$835/month

## Use Cases

### When to Use AWS

1. **Multi-cloud strategy**: Avoid vendor lock-in
2. **Specific AWS services**: Lambda, DynamoDB, etc.
3. **Geographic requirements**: AWS-specific regions
4. **Cost optimization**: Spot instances, reserved capacity
5. **Compliance**: AWS-specific certifications

### Integration with Azure

**Workload Distribution**:
- Azure: Primary production (Norway East)
- GCP: Disaster recovery (Europe West)
- AWS: Development/testing or specific workloads

**Data Replication**:
- Azure SQL → GCP Cloud SQL (continuous replication)
- Azure Redis → AWS ElastiCache (sync as needed)
- Cross-cloud backup strategy

## Troubleshooting

### EKS Connection Issues

```bash
# Update kubeconfig
aws eks update-kubeconfig \
  --region eu-north-1 \
  --name teledoctor-eks

# Check cluster status
aws eks describe-cluster --name teledoctor-eks
```

### VPN to Azure

```bash
# Check VPN status
aws ec2 describe-vpn-connections

# View BGP routes
aws ec2 get-transit-gateway-route-table-propagations \
  --transit-gateway-route-table-id tgw-rtb-xxx
```

### Database Connectivity

```bash
# Test RDS connection
psql -h $(terraform output -raw rds_endpoint) \
     -U admin \
     -d teledoctor

# Check security groups
aws ec2 describe-security-groups \
  --filters "Name=group-name,Values=teledoctor-rds-sg*"
```

---

**Status**: Production-ready AWS infrastructure  
**Purpose**: Multi-cloud comparison and alternative deployment  
**Kubernetes**: EKS with Multi-AZ for high availability  
**Hybrid Connectivity**: Transit Gateway with BGP to Azure

