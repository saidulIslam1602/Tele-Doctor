# TeleDoctor Infrastructure as Code

## 📋 Overview

This directory contains the complete Infrastructure as Code (IaC) implementation for TeleDoctor Modern, demonstrating production-grade infrastructure engineering practices aligned with enterprise requirements.

## 🏗️ Architecture Highlights

- **Hub-Spoke Network Topology** with Azure Firewall and VPN Gateway
- **Zero Trust Network Access (ZTNA)** implementation
- **Multi-region deployment** with disaster recovery
- **Kubernetes orchestration** with advanced networking (Calico)
- **Comprehensive monitoring** and observability
- **Automated deployment** with CI/CD pipelines
- **Configuration management** with Ansible

## 📁 Directory Structure

```
infrastructure/
├── terraform/                    # Terraform infrastructure code
│   ├── main.tf                  # Main infrastructure definition
│   ├── variables.tf             # Input variables
│   ├── outputs.tf               # Output values
│   ├── modules/                 # Reusable Terraform modules
│   │   ├── networking/          # VNet, NSG, Firewall, VPN
│   │   ├── aks/                 # Azure Kubernetes Service
│   │   ├── monitoring/          # Log Analytics, App Insights
│   │   ├── sql/                 # Azure SQL Database
│   │   ├── redis/               # Redis Cache
│   │   ├── keyvault/            # Azure Key Vault
│   │   └── acr/                 # Container Registry
│   └── environments/            # Environment-specific configs
│       ├── production/
│       ├── staging/
│       └── dev/
├── ansible/                     # Ansible configuration management
│   ├── playbooks/              # Ansible playbooks
│   │   └── configure-aks.yml   # AKS cluster configuration
│   ├── inventory/              # Inventory files
│   │   └── hosts.yml          # Host definitions
│   └── manifests/              # Kubernetes manifests
├── NETWORK_ARCHITECTURE.md     # Network architecture documentation
├── SRE_PRACTICES.md            # SRE practices and runbooks
└── README.md                   # This file
```

## 🚀 Quick Start

### Prerequisites

```bash
# Required tools
- Terraform >= 1.6.0
- Azure CLI >= 2.50.0
- kubectl >= 1.28.0
- Ansible >= 2.15.0
- Helm >= 3.12.0

# Install on Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y azure-cli terraform ansible

# Install kubectl
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# Install Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

### Initial Setup

```bash
# 1. Login to Azure
az login

# 2. Set subscription
az account set --subscription "your-subscription-id"

# 3. Create backend storage for Terraform state
az group create --name rg-teledoctor-tfstate --location norwayeast

az storage account create \
  --name teledoctortfstatedev \
  --resource-group rg-teledoctor-tfstate \
  --location norwayeast \
  --sku Standard_LRS

az storage container create \
  --name tfstate \
  --account-name teledoctortfstatedev

# 4. Configure Terraform
cd terraform
cp environments/dev/terraform.tfvars.example environments/dev/terraform.tfvars

# Edit terraform.tfvars with your values
vim environments/dev/terraform.tfvars
```

### Deploy Infrastructure

```bash
# Initialize Terraform
terraform init \
  -backend-config="resource_group_name=rg-teledoctor-tfstate" \
  -backend-config="storage_account_name=teledoctortfstatedev" \
  -backend-config="container_name=tfstate" \
  -backend-config="key=dev.terraform.tfstate"

# Review plan
terraform plan -var-file="environments/dev/terraform.tfvars"

# Apply infrastructure
terraform apply -var-file="environments/dev/terraform.tfvars"

# Get outputs
terraform output
```

### Configure AKS with Ansible

```bash
cd ../ansible

# Get AKS credentials
az aks get-credentials \
  --resource-group rg-teledoctor-dev-ne \
  --name aks-teledoctor-dev

# Run Ansible playbook
ansible-playbook \
  -i inventory/hosts.yml \
  playbooks/configure-aks.yml \
  -e "environment=dev"

# Verify deployment
kubectl get nodes
kubectl get pods --all-namespaces
```

## 🔧 Infrastructure Components

### 1. Networking Module

**Features:**
- Hub-spoke VNet topology (10.0.0.0/16 hub, 10.1.0.0/16 production spoke)
- Azure Firewall with application and network rules
- VPN Gateway with BGP support (ASN: 65515)
- Network Security Groups on all subnets
- Private DNS zones for Azure PaaS services
- VNet peering with gateway transit

**Configuration:**
```hcl
module "networking" {
  source = "./modules/networking"
  
  hub_vnet_address_space = ["10.0.0.0/16"]
  enable_firewall        = true
  enable_vpn_gateway     = true
  enable_bastion         = true
}
```

### 2. AKS Module

**Features:**
- Azure CNI networking with Calico network policies
- Multi-zone node pools (zones 1, 2, 3)
- Auto-scaling (3-10 nodes)
- Application Gateway Ingress Controller
- Azure Policy integration
- Container Insights monitoring

**Configuration:**
```hcl
module "aks" {
  source = "./modules/aks"
  
  network_plugin = "azure"
  network_policy = "calico"
  
  system_node_pool = {
    vm_size = "Standard_D4s_v3"
    min_count = 3
    max_count = 10
  }
}
```

### 3. Monitoring Module

**Features:**
- Log Analytics workspace (90-day retention)
- Application Insights for APM
- Action groups for alerting
- Metric alerts (CPU, memory, disk)
- Container Insights solution

**Alerts:**
- CPU > 80% for 15 minutes
- Memory > 85% for 15 minutes
- Disk > 90% for 15 minutes

## 📊 Network Architecture

### Subnet Allocation

| VNet | Subnet | CIDR | Purpose |
|------|--------|------|---------|
| Hub | AzureFirewallSubnet | 10.0.1.0/24 | Azure Firewall |
| Hub | GatewaySubnet | 10.0.2.0/24 | VPN Gateway |
| Hub | AzureBastionSubnet | 10.0.3.0/24 | Azure Bastion |
| Hub | Management | 10.0.4.0/24 | Jump boxes |
| Spoke | AKS | 10.1.1.0/24 | Kubernetes nodes |
| Spoke | Data | 10.1.2.0/24 | SQL, Redis |
| Spoke | AppGW | 10.1.3.0/24 | Application Gateway |
| Spoke | PrivateLink | 10.1.4.0/24 | Private endpoints |

### Service Networking

| Component | CIDR | Purpose |
|-----------|------|---------|
| Kubernetes Services | 172.16.0.0/16 | ClusterIP services |
| DNS Service | 172.16.0.10 | CoreDNS |
| Docker Bridge | 172.17.0.1/16 | Container networking |

## 🔒 Security Implementation

### Zero Trust Principles

✅ **Verify Explicitly**: Azure AD authentication for all access  
✅ **Least Privilege**: RBAC at resource and Kubernetes levels  
✅ **Assume Breach**: Network segmentation and micro-segmentation  

### Network Security

- **Private Endpoints**: All PaaS services use private endpoints
- **No Public IPs**: Application workloads isolated from internet
- **NSG Rules**: Deny-all by default, explicit allow rules
- **Firewall Rules**: Application-aware filtering
- **Network Policies**: Calico policies for pod-to-pod communication

### Secrets Management

- Azure Key Vault for all secrets
- Managed identities for authentication
- External Secrets Operator in Kubernetes
- No hardcoded credentials

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

**Stages:**
1. **Validate**: Format check, init, validate
2. **Security Scan**: Checkov, tfsec, Trivy
3. **Plan**: Generate and review execution plan
4. **Cost Estimation**: Infracost analysis
5. **Apply**: Deploy to environment (manual approval for production)

**Security Scanning:**
- Checkov for Terraform best practices
- tfsec for security misconfigurations
- Trivy for vulnerability scanning

**Features:**
- Automated PR comments with plan output
- Cost estimation on PRs
- Slack notifications
- Artifact retention (30 days)

### Deployment Process

```yaml
Development: Automatic on push to main
Staging: Automatic on merge to main
Production: Manual approval required (2 approvers)
```

## 📈 Monitoring & Observability

### Metrics Collection

**Prometheus Stack:**
- Prometheus for metrics collection
- Grafana for visualization
- AlertManager for alerting
- ServiceMonitors for auto-discovery

**Azure Monitor:**
- Application Insights for APM
- Container Insights for cluster monitoring
- Log Analytics for centralized logging
- Network Watcher for network diagnostics

### Dashboards

Pre-configured Grafana dashboards:
- API performance metrics
- Infrastructure health
- Cost analysis
- Security alerts

## 🔧 Operational Runbooks

### Common Tasks

#### Scale AKS Cluster
```bash
az aks nodepool scale \
  --resource-group rg-teledoctor-prod-ne \
  --cluster-name aks-teledoctor-prod \
  --name application \
  --node-count 10
```

#### Update Firewall Rules
```bash
az network firewall application-rule create \
  --collection-name teledoctor-app-rules \
  --firewall-name afw-teledoctor-prod \
  --name allow-new-service \
  --protocols Https=443 \
  --target-fqdns api.newservice.com \
  --source-addresses 10.1.1.0/24
```

#### Database Failover
```bash
az sql failover-group set-primary \
  --name teledoctor-fog \
  --resource-group rg-teledoctor-prod-ne \
  --server sql-teledoctor-secondary
```

#### Restart Application
```bash
kubectl rollout restart deployment/teledoctor-api -n production
kubectl rollout status deployment/teledoctor-api -n production
```

## 📖 Documentation

- **[NETWORK_ARCHITECTURE.md](NETWORK_ARCHITECTURE.md)**: Detailed network design
- **[SRE_PRACTICES.md](SRE_PRACTICES.md)**: SRE practices and incident management
- **Module READMEs**: Each Terraform module has its own documentation

## 🧪 Testing

### Infrastructure Testing

```bash
# Terraform validation
terraform fmt -check -recursive
terraform validate

# Security scanning
checkov -d terraform/
tfsec terraform/

# Plan review
terraform plan -var-file=environments/dev/terraform.tfvars
```

### Ansible Testing

```bash
# Syntax check
ansible-playbook playbooks/configure-aks.yml --syntax-check

# Dry run
ansible-playbook playbooks/configure-aks.yml --check

# Run with verbose output
ansible-playbook playbooks/configure-aks.yml -vvv
```

## 💰 Cost Optimization

### Estimated Monthly Costs (Production)

| Component | Configuration | Est. Cost (USD) |
|-----------|--------------|-----------------|
| AKS | 3 x D4s_v3 nodes | $350 |
| Azure Firewall | Standard | $250 |
| Application Gateway | Standard v2 | $180 |
| Azure SQL | GP Gen5 4 vCore | $450 |
| Redis Premium | P1 | $250 |
| Log Analytics | 50 GB/day | $150 |
| **Total** | | **~$1,630/month** |

### Cost Optimization Strategies

✅ Reserved Instances for predictable workloads  
✅ Auto-scaling to minimize idle resources  
✅ Spot instances for dev/test environments  
✅ Log retention policies  
✅ Resource tagging for cost allocation  

## 🚨 Troubleshooting

### Common Issues

**Terraform State Lock**
```bash
# Break state lock (use carefully)
terraform force-unlock <lock-id>
```

**AKS Connection Issues**
```bash
# Refresh credentials
az aks get-credentials \
  --resource-group rg-teledoctor-prod-ne \
  --name aks-teledoctor-prod \
  --overwrite-existing
```

**Network Connectivity**
```bash
# Test from pod
kubectl run -it --rm debug --image=alpine --restart=Never -- sh
# Inside pod: ping, wget, nslookup
```

## 📞 Support

**Infrastructure Team**: infrastructure@teledoctor.no  
**On-Call**: PagerDuty or +47 XXX XX XXX  
**Documentation**: See SRE_PRACTICES.md for escalation procedures

## 🤝 Contributing

1. Create feature branch from `develop`
2. Make infrastructure changes
3. Run `terraform fmt` and validation
4. Submit PR with plan output
5. Await security scan results
6. Get approvals (2 required for production)

## 📝 License

MIT License - See LICENSE file for details

---

**Maintained by**: Infrastructure Team  
**Last Updated**: 2024  
**Version**: 1.0

