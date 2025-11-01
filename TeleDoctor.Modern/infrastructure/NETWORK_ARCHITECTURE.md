# TeleDoctor - Network Architecture

## 📋 Overview

This document describes the comprehensive network architecture for TeleDoctor Modern, implementing enterprise-grade security, high availability, and Zero Trust Network Access (ZTNA) principles.

## 🏗️ Network Topology

### Hub-Spoke Architecture

TeleDoctor uses a **hub-spoke network topology** to centralize shared services while isolating workloads:

```
┌─────────────────────────────────────────────────────────┐
│                    Hub VNet (10.0.0.0/16)               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │Azure Firewall│  │  VPN Gateway │  │Azure Bastion │  │
│  │  10.0.1.0/24 │  │  10.0.2.0/24 │  │  10.0.3.0/24 │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         │                  │                  │          │
└─────────┼──────────────────┼──────────────────┼──────────┘
          │                  │                  │
          │ VNet Peering     │ BGP Routing      │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────┐
│          Production Spoke VNet (10.1.0.0/16)            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │
│  │   AKS    │  │   Data   │  │  App GW  │  │PrivateL │ │
│  │10.1.1.0/ │  │10.1.2.0/ │  │10.1.3.0/ │  │10.1.4.0/│ │
│  │    24    │  │    24    │  │    24    │  │   24    │ │
│  └──────────┘  └──────────┘  └──────────┘  └─────────┘ │
└─────────────────────────────────────────────────────────┘
```

## 📊 Detailed Network Configuration

### Hub VNet (10.0.0.0/16)

| Subnet Name | CIDR | Purpose | Services |
|-------------|------|---------|----------|
| AzureFirewallSubnet | 10.0.1.0/24 | Network security | Azure Firewall, threat protection |
| GatewaySubnet | 10.0.2.0/24 | VPN connectivity | VPN Gateway, ExpressRoute Gateway |
| AzureBastionSubnet | 10.0.3.0/24 | Secure admin access | Azure Bastion |
| ManagementSubnet | 10.0.4.0/24 | Management tools | Jump boxes, admin tools |

### Production Spoke VNet (10.1.0.0/16)

| Subnet Name | CIDR | Purpose | Services |
|-------------|------|---------|----------|
| aks-subnet | 10.1.1.0/24 | Container workloads | AKS nodes, pods |
| data-subnet | 10.1.2.0/24 | Data services | Azure SQL, Redis, private endpoints |
| appgw-subnet | 10.1.3.0/24 | Ingress gateway | Application Gateway |
| privatelink-subnet | 10.1.4.0/24 | Private connectivity | Private Link endpoints |

### Service Networking (AKS)

| Network Component | CIDR | Purpose |
|-------------------|------|---------|
| Service CIDR | 172.16.0.0/16 | Kubernetes services |
| DNS Service IP | 172.16.0.10 | CoreDNS |
| Docker Bridge CIDR | 172.17.0.1/16 | Container networking |

## 🔒 Network Security

### Network Security Groups (NSGs)

#### AKS NSG Rules

| Priority | Name | Direction | Action | Protocol | Port | Source | Destination |
|----------|------|-----------|--------|----------|------|--------|-------------|
| 100 | Allow-HTTPS-Inbound | Inbound | Allow | TCP | 443 | VirtualNetwork | * |
| 110 | Allow-K8s-API | Inbound | Allow | TCP | 6443 | VirtualNetwork | * |
| 4096 | Deny-All-Inbound | Inbound | Deny | * | * | * | * |

#### Data Subnet NSG Rules

| Priority | Name | Direction | Action | Protocol | Port | Source | Destination |
|----------|------|-----------|--------|----------|------|--------|-------------|
| 100 | Allow-SQL-From-AKS | Inbound | Allow | TCP | 1433 | 10.1.1.0/24 | * |
| 110 | Allow-Redis-From-AKS | Inbound | Allow | TCP | 6380 | 10.1.1.0/24 | * |
| 100 | Deny-Internet-Outbound | Outbound | Deny | * | * | * | Internet |
| 4096 | Deny-All-Inbound | Inbound | Deny | * | * | * | * |

### Azure Firewall Configuration

#### Application Rules

```yaml
Priority: 100
Action: Allow
Rules:
  - Name: allow-azure-services
    Target FQDNs:
      - *.azure.com
      - *.microsoft.com
      - *.windows.net
      - *.azurecr.io
      - *.blob.core.windows.net
    Protocol: HTTPS:443
  
  - Name: allow-healthcare-apis
    Target FQDNs:
      - *.helsenorge.no
      - api.openai.azure.com
    Protocol: HTTPS:443
```

#### Network Rules

```yaml
Priority: 200
Action: Allow
Rules:
  - Name: allow-dns
    Protocol: UDP
    Destination Ports: 53
    Destination Addresses: *
  
  - Name: allow-ntp
    Protocol: UDP
    Destination Ports: 123
    Destination Addresses: *
```

### Zero Trust Network Access (ZTNA)

**Principles Implemented:**

1. **Verify explicitly** - Azure AD authentication for all access
2. **Use least privilege access** - RBAC at network and application levels
3. **Assume breach** - Micro-segmentation and continuous monitoring

**Implementation:**

- **Private Endpoints**: All Azure PaaS services accessed via private endpoints
- **No Public IPs**: Application workloads have no direct internet access
- **Network Policies**: Calico policies for pod-to-pod communication control
- **Service Mesh**: Future consideration for mTLS between services

## 🌐 Hybrid Connectivity

### VPN Gateway Configuration

**Specifications:**
- **SKU**: VpnGw2AZ (Zone-redundant)
- **Type**: Route-based VPN
- **Generation**: Generation2
- **Active-Active**: Enabled
- **BGP**: Enabled (ASN: 65515)

**Use Cases:**
- Site-to-site connectivity for on-premises integration
- Point-to-site VPN for secure remote access
- Hybrid cloud scenarios

### BGP Configuration

```yaml
ASN: 65515
BGP Peering Address: Dynamically assigned
Routing:
  - Advertise default route: Disabled
  - Route propagation: Enabled
  - Custom routes: Hub VNet routes
```

### ExpressRoute (Production Recommendation)

```yaml
Circuit SKU: Standard/Premium
Bandwidth: 1 Gbps
Peering Type: Microsoft Peering, Private Peering
Geo-Redundant: Yes
Primary Location: Norway East
Secondary Location: West Europe
```

## 🔐 Private DNS Zones

| Service | Private DNS Zone | Purpose |
|---------|------------------|---------|
| Azure SQL | privatelink.database.windows.net | SQL Server private endpoints |
| Redis Cache | privatelink.redis.cache.windows.net | Redis private endpoints |
| Key Vault | privatelink.vaultcore.azure.net | Key Vault private endpoints |
| ACR | privatelink.azurecr.io | Container Registry access |
| Blob Storage | privatelink.blob.core.windows.net | Storage account access |

**VNet Links:**
- Hub VNet: Linked to all private DNS zones
- Spoke VNets: Linked to required private DNS zones
- Auto-registration: Disabled (manual registration)

## 🌍 Multi-Region Architecture

### Primary Region: Norway East

- Production workloads
- Active-active configuration
- Low latency for Norwegian users

### Secondary Region: West Europe

- Disaster recovery site
- Geo-redundant storage replication
- Failover target for business continuity

### Traffic Manager Configuration

```yaml
Routing Method: Performance
Endpoints:
  - Primary: Norway East (Priority: 1)
  - Secondary: West Europe (Priority: 2)
Health Checks:
  Protocol: HTTPS
  Port: 443
  Path: /health
  Interval: 30 seconds
  Timeout: 10 seconds
  Tolerated failures: 3
```

## 📊 Network Monitoring

### Network Watcher

**Enabled Features:**
- Connection Monitor
- Network Performance Monitor
- NSG Flow Logs
- Traffic Analytics
- Packet Capture
- Connection Troubleshoot

### Flow Logs Configuration

```yaml
Version: 2
Retention: 90 days
Storage Account: Log Analytics integration
Traffic Analytics: Enabled
Interval: 10 minutes
```

## 🔄 High Availability

### Zone Redundancy

All critical network components deployed across availability zones:

- Azure Firewall: Zones 1, 2, 3
- VPN Gateway: Zone-redundant SKU
- AKS Node Pools: Distributed across zones
- Application Gateway: Zone-redundant

### SLA Guarantees

| Component | SLA |
|-----------|-----|
| Azure Firewall | 99.95% |
| VPN Gateway | 99.95% (Zone-redundant) |
| Application Gateway | 99.95% (Zone-redundant) |
| AKS | 99.95% (with uptime SLA) |

## 🚨 Network Security Recommendations

### Implemented

✅ Network segmentation with hub-spoke topology  
✅ Azure Firewall for centralized security  
✅ NSGs on all subnets  
✅ Private endpoints for all PaaS services  
✅ Azure Bastion for secure admin access  
✅ No public IPs on application workloads  
✅ Calico network policies in AKS  
✅ DDoS Protection Standard  

### Future Enhancements

🔲 Azure Front Door for global load balancing  
🔲 Web Application Firewall (WAF) policies  
🔲 Azure Sentinel for security analytics  
🔲 Service Mesh (Istio/Linkerd) for mTLS  
🔲 Network Virtual Appliances (NVAs)  

## 📖 Network Operations

### Common Tasks

#### Add New Spoke VNet

```bash
# Create new spoke VNet
az network vnet create \
  --name vnet-spoke-staging \
  --resource-group rg-teledoctor-prod \
  --address-prefix 10.2.0.0/16

# Create VNet peering
az network vnet peering create \
  --name hub-to-staging \
  --resource-group rg-teledoctor-prod \
  --vnet-name vnet-hub-prod \
  --remote-vnet vnet-spoke-staging \
  --allow-forwarded-traffic \
  --allow-gateway-transit
```

#### Update Firewall Rules

```bash
# Add new application rule
az network firewall application-rule create \
  --collection-name teledoctor-app-rules \
  --firewall-name afw-teledoctor-prod \
  --name allow-new-api \
  --protocols Https=443 \
  --target-fqdns api.newservice.com \
  --source-addresses 10.1.1.0/24
```

## 📞 Support Contacts

- **Network Team**: network-ops@teledoctor.no
- **Security Team**: security@teledoctor.no
- **On-Call**: Use PagerDuty escalation

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Owner**: Infrastructure Team

