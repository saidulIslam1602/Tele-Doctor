# Multi-Cloud Architecture

## Overview

TeleDoctor implements a comprehensive multi-cloud architecture leveraging Azure (primary), Google Cloud Platform (disaster recovery), and Amazon Web Services (alternative platform), demonstrating enterprise-grade hybrid cloud capabilities.

## Cloud Platform Distribution

### Azure (Primary Production)

**Region**: Norway East  
**Purpose**: Primary production environment  
**Workload**: 100% production traffic

**Infrastructure**:
- Hub-spoke VNet topology (10.0.0.0/16 hub, 10.1.0.0/16 spoke)
- Azure Kubernetes Service (AKS)
- Azure SQL Database
- Azure Redis Cache
- Azure Firewall + VPN Gateway
- BGP ASN: 65515

**Connectivity**:
- ExpressRoute for dedicated connectivity
- VPN Gateway with BGP for hybrid
- Virtual WAN for global transit
- Private endpoints for all PaaS

### Google Cloud Platform (Disaster Recovery)

**Region**: Europe West 1  
**Purpose**: Disaster recovery and geographic distribution  
**Workload**: Warm standby, failover capable

**Infrastructure**:
- VPC with Cloud Router (10.20.0.0/16)
- Google Kubernetes Engine (GKE)
- Cloud SQL (PostgreSQL)
- Memorystore for Redis
- Cloud VPN + Partner Interconnect
- BGP ASN: 65516

**Connectivity**:
- Cloud VPN to Azure (with BGP)
- Partner Interconnect (optional, production)
- Private Google Access
- Global VPC routing

### Amazon Web Services (Alternative/Comparison)

**Region**: EU North 1 (Stockholm)  
**Purpose**: Multi-cloud comparison and specific workloads  
**Workload**: Development/testing or AWS-specific services

**Infrastructure**:
- VPC with Transit Gateway (10.30.0.0/16)
- Elastic Kubernetes Service (EKS)
- RDS PostgreSQL
- ElastiCache Redis
- Transit Gateway + Site-to-Site VPN
- BGP ASN: 64512

**Connectivity**:
- Transit Gateway for hybrid
- Site-to-Site VPN to Azure with BGP
- NAT Gateways for internet
- VPC Flow Logs

---

## Hybrid Connectivity Architecture

### Network Topology

```
┌─────────────────────────────────────────────────────┐
│              Azure (Primary)                        │
│  Hub VNet (10.0.0.0/16)                            │
│  ├── Azure Firewall                                │
│  ├── VPN Gateway (BGP ASN: 65515)                 │
│  ├── ExpressRoute Gateway                          │
│  └── Virtual WAN Hub                               │
│         │                                           │
└─────────┼───────────────────────────────────────────┘
          │
          │ BGP Peering
          │
          ├─────────────────────────────────┐
          │                                 │
          ▼                                 ▼
┌──────────────────────┐      ┌──────────────────────┐
│  GCP (DR)            │      │  AWS (Alternative)   │
│  VPC: 10.20.0.0/16   │      │  VPC: 10.30.0.0/16   │
│  Cloud Router        │      │  Transit Gateway     │
│  ASN: 65516          │      │  ASN: 64512          │
│  ├── Cloud VPN       │      │  ├── Site-to-Site    │
│  └── Interconnect    │      │  └── VPN with BGP    │
└──────────────────────┘      └──────────────────────┘
```

### BGP Configuration Summary

| Cloud | ASN | Network Range | Peering With |
|-------|-----|---------------|--------------|
| Azure | 65515 | 10.0.0.0/8 | GCP (65516), AWS (64512) |
| GCP | 65516 | 10.20.0.0/16 | Azure (65515) |
| AWS | 64512 | 10.30.0.0/16 | Azure (65515) |

### Routing Tables

**Azure Routes**:
- 10.0.0.0/8 → Local (Azure VNets)
- 10.20.0.0/16 → GCP (via VPN/Interconnect)
- 10.30.0.0/16 → AWS (via VPN)
- 0.0.0.0/0 → Internet (via Azure Firewall)

**GCP Routes**:
- 10.20.0.0/16 → Local (GCP VPC)
- 10.0.0.0/8 → Azure (via Cloud VPN/Interconnect)
- 0.0.0.0/0 → Internet (via Cloud NAT)

**AWS Routes**:
- 10.30.0.0/16 → Local (AWS VPC)
- 10.0.0.0/8 → Azure (via Transit Gateway VPN)
- 0.0.0.0/0 → Internet (via NAT Gateways)

---

## Connectivity Options Comparison

### Option 1: VPN with BGP (Implemented)

**Setup Time**: Minutes  
**Cost**: Low ($35-150/month per connection)  
**Bandwidth**: Up to 1-10 Gbps  
**Latency**: Internet-based (10-50ms additional)  
**Use Case**: Development, testing, cost-sensitive deployments

**Implementation**:
- Azure: VPN Gateway with BGP
- GCP: Cloud VPN with BGP
- AWS: Site-to-Site VPN with Transit Gateway

### Option 2: Dedicated Connections (Available)

| Service | Provider | Bandwidth | Latency | Cost | SLA |
|---------|----------|-----------|---------|------|-----|
| **Azure ExpressRoute** | Equinix, others | 50 Mbps - 10 Gbps | <5ms | $300-3000/mo | 99.95% |
| **GCP Partner Interconnect** | Equinix, Megaport | 50 Mbps - 10 Gbps | <5ms | $300-3000/mo | 99.9% - 99.99% |
| **AWS Direct Connect** | Equinix, others | 50 Mbps - 100 Gbps | <5ms | $300-5000/mo | 99.9% |

### Option 3: Virtual WAN (Azure Hub)

**Purpose**: Global transit connectivity  
**Benefits**: Centralized routing, simplified management  
**Use Case**: Multiple regions, many branch offices  
**Status**: Implemented in `modules/virtualwan/`

---

## Disaster Recovery Strategy

### RTO and RPO Targets

| Tier | RTO | RPO | Implementation |
|------|-----|-----|----------------|
| **Tier 1** (Critical) | 1 hour | 15 minutes | Continuous replication Azure → GCP |
| **Tier 2** (Important) | 4 hours | 1 hour | Hourly snapshots |
| **Tier 3** (Normal) | 24 hours | 24 hours | Daily backups |

### Failover Procedure

**Automated**:
```
1. Azure Monitor detects primary failure
2. Trigger runbook via Azure Automation
3. Promote GCP Cloud SQL to primary
4. Update DNS to point to GCP
5. Scale GKE cluster to production capacity
6. Notify operations team
```

**Manual**:
```bash
# 1. Verify Azure is down
az monitor metrics list --resource /subscriptions/.../providers/Microsoft.Compute/...

# 2. Activate GCP DR site
cd infrastructure/terraform/multi-cloud/gcp
terraform apply -var="activate_dr=true"

# 3. Promote database
gcloud sql instances promote-replica teledoctor-db-production

# 4. Update DNS
# Point api.teledoctor.no to GCP load balancer

# 5. Scale GKE
gcloud container clusters resize teledoctor-dr-production --num-nodes 10

# 6. Verify
kubectl get pods -n production
curl https://api.teledoctor.no/health
```

### Failback Procedure

```bash
# 1. Verify Azure is healthy
az aks show --name aks-teledoctor-production --resource-group rg-teledoctor-prod

# 2. Sync data GCP → Azure
# Database replication
# File synchronization

# 3. Update DNS back to Azure

# 4. Scale down GCP
terraform apply -var="activate_dr=false"

# 5. Verify
curl https://api.teledoctor.no/health
```

---

## Data Replication

### Database Replication

**Azure SQL → GCP Cloud SQL**:
```
Method: Continuous replication via Cloud SQL external master
Lag: <60 seconds
Verification: Replication lag monitoring
```

**Azure SQL → AWS RDS**:
```
Method: AWS Database Migration Service (DMS)
Lag: <5 minutes
Verification: DMS replication task monitoring
```

### Cache Synchronization

**Redis Replication**:
```
Primary: Azure Redis Cache
Replicas: GCP Memorystore, AWS ElastiCache
Method: Application-level write-through
Consistency: Eventually consistent
```

### File Storage

**Blob Replication**:
- Azure Blob Storage (primary)
- GCP Cloud Storage (replica)
- AWS S3 (replica)
- Method: AzCopy, gsutil, AWS CLI scheduled sync

---

## Cost Comparison

### Infrastructure Cost by Cloud (Monthly, Production)

| Component | Azure | GCP | AWS |
|-----------|-------|-----|-----|
| Kubernetes | $350 (AKS) | $290 (GKE) | $220 (EKS) |
| Database | $450 (SQL) | $250 (Cloud SQL) | $280 (RDS) |
| Cache | $250 (Redis Premium) | $150 (Memorystore) | $100 (ElastiCache) |
| Networking | $250 (Firewall) | $35 (VPN) | $100 (NAT) |
| Load Balancer | $180 (App GW) | Included | $20 (ALB) |
| Monitoring | $150 (Log Analytics) | $50 (Operations) | $30 (CloudWatch) |
| **Total** | **$1,630** | **$775** | **$750** |

**Notes**:
- Azure cost higher due to premium services and compliance features
- GCP competitive pricing, good for DR
- AWS cost-effective for development

### Cost Optimization Strategies

**Multi-Cloud**:
- Use cheapest cloud for development (AWS)
- Reserved instances in all clouds
- Auto-scaling to minimize idle resources
- DR site scaled down until needed

**Connectivity**:
- VPN for development (low cost)
- ExpressRoute/Interconnect for production (performance)
- Evaluate Direct Connect based on bandwidth needs

---

## Security Considerations

### Network Segmentation

**Azure**: Hub-spoke with centralized firewall  
**GCP**: VPC with subnet isolation  
**AWS**: VPC with security groups and NACLs  

**All Clouds**:
- Private clusters (no public endpoints)
- Network policies (Calico/native)
- Firewall rules (default deny)
- Private database access only

### Identity Federation

**Cross-Cloud IAM**:
- Azure AD as identity provider
- GCP Workload Identity Federation
- AWS IAM with SAML/OIDC
- Single sign-on across clouds

### Encryption

**In Transit**:
- TLS 1.3 for all connections
- IPsec for VPN tunnels
- BGP MD5 authentication

**At Rest**:
- Azure: Customer-managed keys (Key Vault)
- GCP: Customer-managed encryption keys (KMS)
- AWS: KMS encryption

---

## Operational Procedures

### Health Checks

**Global Health Check**:
```bash
# Check all clouds
curl https://api.teledoctor.no/health  # Azure (primary)
curl https://dr.teledoctor.no/health   # GCP (DR)
curl https://dev.teledoctor.no/health  # AWS (dev)
```

### Failover Testing

**Quarterly DR Drill**:
1. Schedule during maintenance window
2. Simulate Azure outage
3. Execute GCP failover
4. Verify application functionality
5. Measure RTO/RPO achievement
6. Document lessons learned
7. Failback to Azure

### Cross-Cloud Monitoring

**Centralized Monitoring** (Azure Monitor as hub):
- Collect metrics from all clouds
- Unified dashboards
- Cross-cloud alerting
- Single pane of glass

---

## Compliance and Governance

### Data Residency

**Norway Data Protection**:
- Primary data: Azure Norway East
- DR replica: GCP Europe West (EU)
- Dev/test: AWS EU North (EU)
- All data stays within EU/EEA

### Compliance Certifications

| Standard | Azure | GCP | AWS |
|----------|-------|-----|-----|
| ISO 27001 | ✓ | ✓ | ✓ |
| SOC 2 | ✓ | ✓ | ✓ |
| GDPR | ✓ | ✓ | ✓ |
| HIPAA | ✓ | ✓ | ✓ |
| Norwegian Laws | ✓ | ✓ | ✓ |

### Audit Logging

**All Clouds**:
- VPC/VNet flow logs
- API access logs
- Database audit logs
- Security event logs
- 90-day retention minimum

---

## Use Case Matrix

### When to Use Each Cloud

**Azure** (Primary):
- Production workloads
- Norwegian data residency requirements
- Azure AD integration
- Microsoft stack integration
- ExpressRoute available

**GCP** (Disaster Recovery):
- DR failover site
- BigQuery for analytics
- AI/ML experimentation
- Cost-effective compute
- Strong Kubernetes support

**AWS** (Development/Alternative):
- Development and testing
- Specific AWS services (Lambda, DynamoDB)
- Cost-optimized workloads
- Global edge locations
- Mature service ecosystem

---

## Network Connectivity Matrix

| From → To | Method | Bandwidth | Latency | BGP |
|-----------|--------|-----------|---------|-----|
| **Azure → GCP** | VPN / Interconnect | 1-10 Gbps | 10-30ms | Yes (65515↔65516) |
| **Azure → AWS** | VPN / Direct Connect | 1-10 Gbps | 15-35ms | Yes (65515↔64512) |
| **GCP → AWS** | Cloud VPN | 1-3 Gbps | 20-40ms | Possible |

### BGP Peering Summary

```
Azure VPN Gateway (ASN: 65515)
├── Peer with GCP Cloud Router (ASN: 65516)
│   ├── Interface: 169.254.21.1 ↔ 169.254.21.2
│   ├── Advertise: 10.0.0.0/8
│   └── Receive: 10.20.0.0/16
│
└── Peer with AWS Transit Gateway (ASN: 64512)
    ├── Interface: 169.254.22.1 ↔ 169.254.22.2
    ├── Advertise: 10.0.0.0/8
    └── Receive: 10.30.0.0/16
```

---

## Technology Comparison

### Kubernetes Implementations

| Feature | Azure AKS | GCP GKE | AWS EKS |
|---------|-----------|---------|---------|
| **Network Plugin** | Azure CNI | VPC-native | AWS VPC CNI |
| **Network Policy** | Calico | Calico | Calico |
| **Service Mesh** | Istio/Linkerd | Anthos | App Mesh |
| **Managed** | Fully managed | Fully managed | Control plane managed |
| **Auto-scaling** | Cluster autoscaler | GKE autoscaler | Cluster autoscaler |
| **Regions** | All Azure regions | All GCP regions | All AWS regions |

### Database Comparison

| Feature | Azure SQL | GCP Cloud SQL | AWS RDS |
|---------|-----------|---------------|---------|
| **Engine** | SQL Server / PostgreSQL | PostgreSQL / MySQL | PostgreSQL / MySQL / SQL Server |
| **HA** | Zone-redundant | Regional | Multi-AZ |
| **Backup** | 35 days | 365 days | 35 days |
| **Encryption** | TDE + CMK | CMEK | KMS |
| **Replication** | Geo-replication | Read replicas | Read replicas |

---

## Deployment Strategy

### Environment Distribution

**Development**:
- Cloud: AWS (cost-effective)
- Region: EU North 1
- Scale: Minimal (3 nodes)
- Cost: ~$750/month

**Staging**:
- Cloud: Azure (production parity)
- Region: Norway East
- Scale: Medium (5 nodes)
- Cost: ~$1,200/month

**Production**:
- Primary: Azure Norway East
- DR: GCP Europe West
- Cost: ~$2,400/month combined

### Deployment Workflow

```
Developer Workstation
       │
       ├─── Terraform Cloud
       │    ├─── Plan for all clouds
       │    └─── Approval workflow
       │
       ├─── Azure DevOps
       │    └─── Deploy to Azure
       │
       ├─── Google Cloud Build
       │    └─── Deploy to GCP
       │
       └─── AWS CodePipeline
            └─── Deploy to AWS
```

---

## Monitoring and Observability

### Cross-Cloud Monitoring

**Centralized in Azure**:
- Log Analytics as central logging
- Application Insights for APM
- Workbooks for cross-cloud dashboards

**Cloud-Specific**:
- Azure Monitor for Azure resources
- Cloud Operations for GCP resources
- CloudWatch for AWS resources

**Unified Alerts**:
- PagerDuty integration from all clouds
- Slack notifications
- Email alerts

### Key Metrics

**Golden Signals Across Clouds**:
1. Latency (P50, P95, P99 per cloud)
2. Traffic (requests/sec per cloud)
3. Errors (error rate per cloud)
4. Saturation (resource utilization per cloud)

---

## Cost Management

### Multi-Cloud Cost Optimization

**Reserved Capacity**:
- Azure: 1-3 year reserved instances
- GCP: Committed use discounts
- AWS: Savings plans

**Auto-Scaling**:
- All clouds: Scale down during off-hours
- DR: Keep minimal until needed
- Dev: Shutdown nights and weekends

**Right-Sizing**:
- Azure: Use B-series for dev
- GCP: E2 instances (cost-optimized)
- AWS: T3 instances with burstable CPU

**Total Monthly Cost**:
```
Development (AWS):        $750
Staging (Azure):        $1,200
Production (Azure):     $1,630
DR (GCP):                $400 (scaled down)
────────────────────────────────
Total:                  $3,980/month
```

---

## Governance

### Tagging Strategy

**Required Tags (All Clouds)**:
- Environment: dev / staging / production
- Project: TeleDoctor
- ManagedBy: Terraform
- CostCenter: Healthcare
- Owner: Infrastructure Team

### Resource Naming Convention

```
Format: <resource-type>-<application>-<environment>-<region>

Examples:
- Azure: aks-teledoctor-prod-ne (Norway East)
- GCP: gke-teledoctor-dr-ew1 (Europe West 1)
- AWS: eks-teledoctor-dev-eun1 (EU North 1)
```

---

## Skills Demonstrated

### Cloud Platforms

- **Azure**: Production-grade infrastructure (1,762 lines Terraform)
- **GCP**: Disaster recovery with Cloud Interconnect (350+ lines)
- **AWS**: Alternative platform with Transit Gateway (400+ lines)

### Hybrid Networking

- **BGP**: Configured on all three clouds with unique ASNs
- **VPN**: Site-to-site with dynamic routing
- **Dedicated Circuits**: ExpressRoute, Interconnect, Direct Connect
- **Transit Routing**: Virtual WAN, Cloud Router, Transit Gateway

### Multi-Cloud Expertise

- Infrastructure as Code for all three major clouds
- Cross-cloud connectivity patterns
- Unified monitoring and alerting
- Cost optimization across platforms
- Compliance and governance

---

## Resources

### Azure
- [Azure Virtual WAN](https://docs.microsoft.com/azure/virtual-wan/)
- [ExpressRoute Documentation](https://docs.microsoft.com/azure/expressroute/)
- [Azure Firewall](https://docs.microsoft.com/azure/firewall/)

### GCP
- [Cloud Interconnect](https://cloud.google.com/network-connectivity/docs/interconnect)
- [Cloud VPN](https://cloud.google.com/network-connectivity/docs/vpn)
- [Cloud Router](https://cloud.google.com/network-connectivity/docs/router)

### AWS
- [AWS Transit Gateway](https://docs.aws.amazon.com/vpc/latest/tgw/)
- [AWS Direct Connect](https://docs.aws.amazon.com/directconnect/)
- [Site-to-Site VPN](https://docs.aws.amazon.com/vpn/)

---

**Status**: Production-ready multi-cloud architecture  
**Clouds Supported**: Azure (primary), GCP (DR), AWS (alternative)  
**Total Terraform Code**: 2,500+ lines across all clouds  
**BGP Configuration**: Complete with unique ASNs per cloud

