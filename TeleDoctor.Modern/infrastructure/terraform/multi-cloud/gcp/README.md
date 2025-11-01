# Google Cloud Platform Infrastructure

## Overview

This directory contains Terraform code for deploying TeleDoctor infrastructure on Google Cloud Platform (GCP) as a disaster recovery site and multi-cloud demonstration.

## Architecture

### Network Design

```
GCP VPC (10.20.0.0/16)
├── GKE Subnet (10.20.0.0/24)
│   ├── Pods (10.21.0.0/16)
│   └── Services (10.22.0.0/16)
└── Data Subnet (10.20.1.0/24)
    ├── Cloud SQL (PostgreSQL)
    └── Memorystore (Redis)

Connectivity to Azure:
├── Cloud VPN with BGP (ASN: 65516)
└── Partner Interconnect (optional, production)
```

### Components

**Compute**:
- GKE cluster (regional, private)
- Auto-scaling node pool (3-10 nodes)
- Calico network policies

**Data**:
- Cloud SQL (PostgreSQL 15)
- Memorystore for Redis
- Automated backups

**Networking**:
- VPC with custom subnets
- Cloud Router with BGP
- Cloud NAT for outbound
- HA VPN to Azure
- Partner Interconnect (optional)

**Security**:
- Private GKE cluster
- Firewall rules (default deny)
- Workload Identity
- Private Google Access

## BGP Configuration

### Cloud Router

**ASN**: 65516 (different from Azure: 65515)  
**Routing Mode**: GLOBAL  
**Advertised Routes**: ALL_SUBNETS + custom ranges

### Peering with Azure

**Interface**: 169.254.21.2/30 (GCP)  
**Peer IP**: 169.254.21.1 (Azure)  
**Peer ASN**: 65515 (Azure VPN Gateway)  
**Route Priority**: 100

## Cloud Interconnect

### Partner Interconnect to Azure

**Type**: PARTNER (Equinix, Megaport, etc.)  
**Bandwidth**: 1 Gbps  
**Redundancy**: AVAILABILITY_DOMAIN_1  
**Use Case**: Low-latency, high-bandwidth Azure-GCP connectivity

**Comparison with VPN**:
| Feature | Cloud VPN | Partner Interconnect |
|---------|-----------|---------------------|
| Bandwidth | Up to 3 Gbps per tunnel | 50 Mbps - 10 Gbps |
| Latency | Internet-based | Dedicated, lower latency |
| SLA | 99.9% | 99.9% - 99.99% |
| Cost | Lower | Higher (dedicated circuit) |
| Setup Time | Minutes | Weeks (partner provisioning) |

## Deployment

### Prerequisites

```bash
# Install gcloud CLI
curl https://sdk.cloud.google.com | bash
exec -l $SHELL

# Authenticate
gcloud auth login
gcloud auth application-default login

# Set project
gcloud config set project YOUR_PROJECT_ID
```

### Deploy

```bash
cd infrastructure/terraform/multi-cloud/gcp

# Initialize Terraform
terraform init

# Plan
terraform plan -var="gcp_project_id=YOUR_PROJECT_ID"

# Apply
terraform apply -var="gcp_project_id=YOUR_PROJECT_ID"
```

### Connect to GKE

```bash
# Get credentials
gcloud container clusters get-credentials teledoctor-dr-production \
  --region europe-west1

# Verify connection
kubectl get nodes
```

## Multi-Cloud Connectivity

### Option 1: Cloud VPN (Implemented)

**Setup**:
1. Deploy GCP infrastructure (creates HA VPN Gateway)
2. Note GCP VPN Gateway IPs from Terraform output
3. Configure Azure VPN Gateway to connect to GCP
4. Exchange BGP information (ASN: 65515 ↔ 65516)
5. Verify BGP peering: `gcloud compute routers get-status teledoctor-router`

**Advantages**:
- Quick setup (minutes)
- Cost-effective
- IPsec encrypted
- BGP dynamic routing

### Option 2: Partner Interconnect (Optional)

**Setup**:
1. Enable in Terraform: `enable_cloud_interconnect = true`
2. Contact partner provider (Equinix, Megaport)
3. Provision circuit
4. Use pairing key from Terraform output
5. Complete partner provisioning

**Advantages**:
- Dedicated bandwidth
- Lower latency
- Higher SLA
- Not internet-dependent

## Disaster Recovery

### Failover Procedure

**Database**:
```bash
# Promote GCP Cloud SQL to primary
gcloud sql instances promote-replica teledoctor-db-production

# Update application connection strings
kubectl set env deployment/teledoctor-api \
  DATABASE_HOST=$(terraform output cloud_sql_private_ip)
```

**Traffic**:
```bash
# Update Azure Traffic Manager to route to GCP
az network traffic-manager endpoint update \
  --name gcp-endpoint \
  --profile-name teledoctor-tm \
  --resource-group rg-teledoctor-prod \
  --type externalEndpoints \
  --priority 1
```

**Kubernetes**:
```bash
# Deploy application to GKE
kubectl apply -f ../../kubernetes/production/

# Verify deployment
kubectl get pods -n production
```

## Cost Estimate

**Monthly Cost (Production)**:
- GKE (3 x e2-standard-4): $290
- Cloud SQL (4 vCPU, 16GB): $250
- Memorystore Redis (5GB): $150
- VPN Gateway: $35
- Cloud NAT: $30
- Egress traffic: $50-100
- **Total**: ~$805/month

**Partner Interconnect Additional**: $300-500/month (1 Gbps)

## Security

### Network Security

- Private GKE cluster (no public endpoint)
- Firewall rules (default deny)
- Private Google Access enabled
- SSL required for Cloud SQL

### Identity and Access

- Workload Identity for GKE pods
- Service account with least privilege
- IAM policies enforced
- Audit logging enabled

## Monitoring

**Cloud Operations Suite**:
- Cloud Logging (Stackdriver)
- Cloud Monitoring
- Uptime checks
- Alert policies

**Metrics Collected**:
- GKE cluster health
- Node CPU/memory
- Pod status
- Database performance
- Redis cache hit ratio

## Comparison with Azure

| Feature | Azure Implementation | GCP Implementation |
|---------|---------------------|-------------------|
| Kubernetes | AKS with Azure CNI | GKE with VPC-native |
| Database | Azure SQL | Cloud SQL (PostgreSQL) |
| Cache | Azure Redis | Memorystore |
| Network | Hub-spoke VNet | VPC with Cloud Router |
| Connectivity | VPN Gateway + ExpressRoute | Cloud VPN + Interconnect |
| BGP ASN | 65515 | 65516 |
| Firewall | Azure Firewall | VPC Firewall Rules |
| Monitoring | Azure Monitor | Cloud Operations |

## Usage Notes

**Purpose**:
- Disaster recovery site for Azure primary
- Multi-cloud capability demonstration
- Geographic distribution option

**Best Practices**:
- Keep GCP as warm standby
- Regular DR drills
- Data replication strategy
- Cost optimization in DR

## Resources

- [GCP VPC Documentation](https://cloud.google.com/vpc/docs)
- [GKE Best Practices](https://cloud.google.com/kubernetes-engine/docs/best-practices)
- [Cloud VPN with BGP](https://cloud.google.com/network-connectivity/docs/vpn/concepts/overview)
- [Partner Interconnect](https://cloud.google.com/network-connectivity/docs/interconnect/concepts/partner-overview)

---

**Status**: Production-ready, demonstrates multi-cloud capability  
**Use Case**: Disaster recovery and multi-cloud architecture

