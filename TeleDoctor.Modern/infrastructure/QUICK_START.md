# Infrastructure Quick Start Guide

## ⚡ Fast Track Deployment

### Prerequisites Check

```bash
# Verify all tools are installed
terraform --version    # Should be >= 1.6.0
az --version          # Azure CLI >= 2.50.0
kubectl version       # >= 1.28.0
ansible --version     # >= 2.15.0
helm version          # >= 3.12.0
```

### 1. Azure Setup (5 minutes)

```bash
# Login to Azure
az login

# Create resource group for Terraform state
az group create \
  --name rg-teledoctor-tfstate \
  --location norwayeast

# Create storage account
az storage account create \
  --name teledoctortfstatedev \
  --resource-group rg-teledoctor-tfstate \
  --sku Standard_LRS

# Create container
az storage container create \
  --name tfstate \
  --account-name teledoctortfstatedev
```

### 2. Configure Variables (2 minutes)

```bash
cd TeleDoctor.Modern/infrastructure/terraform

# Copy example config
cp environments/dev/terraform.tfvars.example environments/dev/terraform.tfvars

# Edit with your values
nano environments/dev/terraform.tfvars
```

**Minimum required changes:**
```hcl
sql_admin_username = "sqladmin"
sql_admin_password = "YourSecurePassword123!"
jwt_secret_key     = "your-jwt-secret-key-min-32-chars"
```

### 3. Deploy Infrastructure (10 minutes)

```bash
# Initialize Terraform
terraform init \
  -backend-config="resource_group_name=rg-teledoctor-tfstate" \
  -backend-config="storage_account_name=teledoctortfstatedev" \
  -backend-config="container_name=tfstate" \
  -backend-config="key=dev.terraform.tfstate"

# Review what will be created
terraform plan -var-file="environments/dev/terraform.tfvars"

# Deploy (takes ~10-15 minutes)
terraform apply -var-file="environments/dev/terraform.tfvars" -auto-approve
```

### 4. Configure Kubernetes (5 minutes)

```bash
# Get AKS credentials
az aks get-credentials \
  --resource-group rg-teledoctor-dev-ne \
  --name aks-teledoctor-dev \
  --overwrite-existing

# Verify connection
kubectl get nodes

# Deploy monitoring and security
cd ../ansible
ansible-playbook \
  -i inventory/hosts.yml \
  playbooks/configure-aks.yml \
  -e "environment=dev"
```

### 5. Verify Deployment (2 minutes)

```bash
# Check infrastructure
kubectl get nodes
kubectl get pods --all-namespaces
kubectl get svc --all-namespaces

# Access Grafana (monitoring)
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80

# Open browser: http://localhost:3000
# Username: admin
# Password: Check Ansible output or use: kubectl get secret -n monitoring prometheus-grafana -o jsonpath="{.data.admin-password}" | base64 --decode
```

## 🎯 What Gets Deployed

### Azure Resources

✅ **Networking**
- Hub VNet (10.0.0.0/16)
- Production Spoke VNet (10.1.0.0/16)
- Azure Firewall (zone-redundant)
- VPN Gateway with BGP
- 8 Subnets with NSGs

✅ **Compute**
- AKS Cluster (3 nodes, Standard_D4s_v3)
- Auto-scaling enabled (3-10 nodes)
- Multi-zone deployment

✅ **Data**
- Azure SQL Database (GP Gen5)
- Redis Cache (Premium P1)
- Geo-redundant backups

✅ **Security**
- Azure Key Vault
- Private endpoints for all services
- Network security groups
- Azure Bastion

✅ **Monitoring**
- Log Analytics Workspace
- Application Insights
- Prometheus + Grafana
- Alert rules configured

### Kubernetes Resources

✅ **Namespaces**
- production
- staging
- monitoring
- security
- ingress-nginx

✅ **Applications**
- Prometheus (metrics)
- Grafana (dashboards)
- AlertManager (alerts)
- NGINX Ingress Controller
- cert-manager (TLS certs)
- External Secrets Operator

✅ **Security**
- Network policies (default deny)
- Pod security standards
- Falco runtime security

## 📊 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| Grafana | http://localhost:3000 | admin / (from secret) |
| Prometheus | http://localhost:9090 | None |
| AlertManager | http://localhost:9093 | None |

**Port Forward Commands:**
```bash
# Grafana
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80

# Prometheus
kubectl port-forward -n monitoring svc/prometheus-kube-prometheus-prometheus 9090:9090

# AlertManager
kubectl port-forward -n monitoring svc/prometheus-kube-prometheus-alertmanager 9093:9093
```

## 🔧 Common Operations

### Scale AKS Nodes

```bash
az aks nodepool scale \
  --resource-group rg-teledoctor-dev-ne \
  --cluster-name aks-teledoctor-dev \
  --name application \
  --node-count 5
```

### View Infrastructure Costs

```bash
# Get current month costs
az consumption usage list \
  --start-date $(date -d "$(date +%Y-%m-01)" +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d)
```

### Check Resource Status

```bash
# All resources in resource group
az resource list \
  --resource-group rg-teledoctor-dev-ne \
  --output table

# AKS cluster info
az aks show \
  --resource-group rg-teledoctor-dev-ne \
  --name aks-teledoctor-dev
```

### Get Connection Strings

```bash
# SQL Database
terraform output -raw sql_server_fqdn

# Redis Cache
terraform output -raw redis_hostname

# Application Insights
terraform output -raw application_insights_connection_string
```

## 🚨 Troubleshooting

### Terraform State Lock

```bash
# If deployment hangs
terraform force-unlock <LOCK_ID>
```

### AKS Connection Issues

```bash
# Refresh credentials
az aks get-credentials \
  --resource-group rg-teledoctor-dev-ne \
  --name aks-teledoctor-dev \
  --overwrite-existing

# Check cluster
kubectl cluster-info
```

### Pod Not Starting

```bash
# Check pod status
kubectl describe pod <pod-name> -n <namespace>

# Check logs
kubectl logs <pod-name> -n <namespace>

# Check events
kubectl get events -n <namespace> --sort-by='.lastTimestamp'
```

### Network Connectivity

```bash
# Test from debug pod
kubectl run -it --rm debug --image=alpine --restart=Never -- sh

# Inside pod
apk add curl bind-tools
nslookup google.com
ping 8.8.8.8
curl https://google.com
```

## 🧹 Cleanup

### Destroy Everything

```bash
# Destroy Terraform resources (takes ~10 minutes)
cd infrastructure/terraform
terraform destroy -var-file="environments/dev/terraform.tfvars" -auto-approve

# Remove state storage (optional)
az group delete --name rg-teledoctor-tfstate --yes --no-wait
```

### Remove Kubernetes Resources Only

```bash
# Delete specific namespaces
kubectl delete namespace production
kubectl delete namespace staging

# Or use Ansible
ansible-playbook playbooks/cleanup-aks.yml
```

## 💰 Cost Estimates

**Development Environment (~$400/month)**
- AKS: 3 x D4s_v3 nodes = $210
- SQL: Basic tier = $5
- Redis: Basic = $15
- Firewall: Standard = $150 (optional)
- Monitoring: ~$20

**Production Environment (~$1,630/month)**
- See infrastructure/README.md for details

**Cost Saving Tips:**
- Use dev environment only during working hours
- Enable auto-shutdown scripts
- Use Azure Dev/Test pricing
- Reserved instances for production

## 📚 Next Steps

1. ✅ Review [NETWORK_ARCHITECTURE.md](NETWORK_ARCHITECTURE.md) for network details
2. ✅ Read [SRE_PRACTICES.md](SRE_PRACTICES.md) for operational procedures
3. ✅ Deploy application workloads to AKS
4. ✅ Configure monitoring alerts
5. ✅ Setup CI/CD pipeline for applications

## 📞 Support

- **Documentation**: All `.md` files in infrastructure directory
- **Issues**: Check troubleshooting section above
- **Email**: infrastructure@teledoctor.no

---

**Time to Deploy**: ~25 minutes  
**Complexity**: Intermediate  
**Cost**: ~$400/month (dev)

