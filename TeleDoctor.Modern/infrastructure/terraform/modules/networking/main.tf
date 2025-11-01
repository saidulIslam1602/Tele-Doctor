# Networking Module - Hub-Spoke Topology with Azure Firewall, VPN Gateway, and NSGs

# Hub Virtual Network
resource "azurerm_virtual_network" "hub" {
  name                = "vnet-hub-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  address_space       = var.hub_vnet_address_space
  
  tags = var.tags
}

# Hub Subnets
resource "azurerm_subnet" "firewall" {
  count                = var.enable_firewall ? 1 : 0
  name                 = "AzureFirewallSubnet"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.hub.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_subnet" "gateway" {
  count                = var.enable_vpn_gateway ? 1 : 0
  name                 = "GatewaySubnet"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.hub.name
  address_prefixes     = ["10.0.2.0/24"]
}

resource "azurerm_subnet" "bastion" {
  count                = var.enable_bastion ? 1 : 0
  name                 = "AzureBastionSubnet"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.hub.name
  address_prefixes     = ["10.0.3.0/24"]
}

resource "azurerm_subnet" "management" {
  name                 = "snet-management"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.hub.name
  address_prefixes     = ["10.0.4.0/24"]
}

# Production Spoke VNet
resource "azurerm_virtual_network" "spoke_prod" {
  name                = "vnet-spoke-prod-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  address_space       = var.spoke_vnet_address_spaces["production"]
  
  tags = var.tags
}

# Production Spoke Subnets
resource "azurerm_subnet" "aks" {
  name                 = "snet-aks"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.spoke_prod.name
  address_prefixes     = ["10.1.1.0/24"]
}

resource "azurerm_subnet" "data" {
  name                 = "snet-data"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.spoke_prod.name
  address_prefixes     = ["10.1.2.0/24"]
  
  # Enable private endpoint network policies
  private_endpoint_network_policies_enabled = false
}

resource "azurerm_subnet" "appgw" {
  name                 = "snet-appgw"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.spoke_prod.name
  address_prefixes     = ["10.1.3.0/24"]
}

resource "azurerm_subnet" "privatelink" {
  name                 = "snet-privatelink"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.spoke_prod.name
  address_prefixes     = ["10.1.4.0/24"]
  
  private_endpoint_network_policies_enabled = false
}

# Network Security Groups
resource "azurerm_network_security_group" "aks" {
  name                = "nsg-aks-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  tags = var.tags
}

resource "azurerm_network_security_rule" "aks_allow_https" {
  name                        = "Allow-HTTPS-Inbound"
  priority                    = 100
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "Tcp"
  source_port_range           = "*"
  destination_port_range      = "443"
  source_address_prefix       = "VirtualNetwork"
  destination_address_prefix  = "*"
  resource_group_name         = var.resource_group_name
  network_security_group_name = azurerm_network_security_group.aks.name
}

resource "azurerm_network_security_rule" "aks_deny_all_inbound" {
  name                        = "Deny-All-Inbound"
  priority                    = 4096
  direction                   = "Inbound"
  access                      = "Deny"
  protocol                    = "*"
  source_port_range           = "*"
  destination_port_range      = "*"
  source_address_prefix       = "*"
  destination_address_prefix  = "*"
  resource_group_name         = var.resource_group_name
  network_security_group_name = azurerm_network_security_group.aks.name
}

resource "azurerm_subnet_network_security_group_association" "aks" {
  subnet_id                 = azurerm_subnet.aks.id
  network_security_group_id = azurerm_network_security_group.aks.id
}

# Data subnet NSG
resource "azurerm_network_security_group" "data" {
  name                = "nsg-data-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  tags = var.tags
}

resource "azurerm_network_security_rule" "data_deny_internet" {
  name                        = "Deny-Internet-Outbound"
  priority                    = 100
  direction                   = "Outbound"
  access                      = "Deny"
  protocol                    = "*"
  source_port_range           = "*"
  destination_port_range      = "*"
  source_address_prefix       = "*"
  destination_address_prefix  = "Internet"
  resource_group_name         = var.resource_group_name
  network_security_group_name = azurerm_network_security_group.data.name
}

resource "azurerm_subnet_network_security_group_association" "data" {
  subnet_id                 = azurerm_subnet.data.id
  network_security_group_id = azurerm_network_security_group.data.id
}

# Azure Firewall
resource "azurerm_public_ip" "firewall" {
  count               = var.enable_firewall ? 1 : 0
  name                = "pip-firewall-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"
  sku                 = "Standard"
  zones               = ["1", "2", "3"]
  
  tags = var.tags
}

resource "azurerm_firewall" "main" {
  count               = var.enable_firewall ? 1 : 0
  name                = "afw-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = "AZFW_VNet"
  sku_tier            = "Standard"
  zones               = ["1", "2", "3"]
  
  ip_configuration {
    name                 = "configuration"
    subnet_id            = azurerm_subnet.firewall[0].id
    public_ip_address_id = azurerm_public_ip.firewall[0].id
  }
  
  tags = var.tags
}

# Firewall Application Rules
resource "azurerm_firewall_application_rule_collection" "main" {
  count               = var.enable_firewall ? 1 : 0
  name                = "teledoctor-app-rules"
  azure_firewall_name = azurerm_firewall.main[0].name
  resource_group_name = var.resource_group_name
  priority            = 100
  action              = "Allow"
  
  rule {
    name = "allow-azure-services"
    source_addresses = ["*"]
    target_fqdns = [
      "*.azure.com",
      "*.microsoft.com",
      "*.windows.net",
      "*.azurecr.io",
      "*.blob.core.windows.net"
    ]
    protocol {
      port = "443"
      type = "Https"
    }
  }
  
  rule {
    name = "allow-healthcare-apis"
    source_addresses = ["*"]
    target_fqdns = [
      "*.helsenorge.no",
      "api.openai.azure.com"
    ]
    protocol {
      port = "443"
      type = "Https"
    }
  }
}

# VPN Gateway
resource "azurerm_public_ip" "vpn_gateway" {
  count               = var.enable_vpn_gateway ? 1 : 0
  name                = "pip-vpngw-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"
  sku                 = "Standard"
  zones               = ["1", "2", "3"]
  
  tags = var.tags
}

resource "azurerm_virtual_network_gateway" "vpn" {
  count               = var.enable_vpn_gateway ? 1 : 0
  name                = "vgw-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  type     = "Vpn"
  vpn_type = "RouteBased"
  
  active_active = false
  enable_bgp    = true
  sku           = "VpnGw2AZ"
  generation    = "Generation2"
  
  bgp_settings {
    asn = 65515
  }
  
  ip_configuration {
    name                          = "vnetGatewayConfig"
    public_ip_address_id          = azurerm_public_ip.vpn_gateway[0].id
    private_ip_address_allocation = "Dynamic"
    subnet_id                     = azurerm_subnet.gateway[0].id
  }
  
  tags = var.tags
}

# VNet Peering - Hub to Spoke
resource "azurerm_virtual_network_peering" "hub_to_spoke" {
  name                      = "peer-hub-to-spoke-prod"
  resource_group_name       = var.resource_group_name
  virtual_network_name      = azurerm_virtual_network.hub.name
  remote_virtual_network_id = azurerm_virtual_network.spoke_prod.id
  
  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true
  use_remote_gateways          = false
}

# VNet Peering - Spoke to Hub
resource "azurerm_virtual_network_peering" "spoke_to_hub" {
  name                      = "peer-spoke-prod-to-hub"
  resource_group_name       = var.resource_group_name
  virtual_network_name      = azurerm_virtual_network.spoke_prod.name
  remote_virtual_network_id = azurerm_virtual_network.hub.id
  
  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = false
  use_remote_gateways          = var.enable_vpn_gateway
  
  depends_on = [azurerm_virtual_network_gateway.vpn]
}

# Private DNS Zones
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = var.resource_group_name
  
  tags = var.tags
}

resource "azurerm_private_dns_zone" "redis" {
  name                = "privatelink.redis.cache.windows.net"
  resource_group_name = var.resource_group_name
  
  tags = var.tags
}

resource "azurerm_private_dns_zone" "keyvault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = var.resource_group_name
  
  tags = var.tags
}

resource "azurerm_private_dns_zone" "acr" {
  name                = "privatelink.azurecr.io"
  resource_group_name = var.resource_group_name
  
  tags = var.tags
}

# Link Private DNS Zones to VNets
resource "azurerm_private_dns_zone_virtual_network_link" "sql_hub" {
  name                  = "sql-hub-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = azurerm_virtual_network.hub.id
  registration_enabled  = false
  
  tags = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql_spoke" {
  name                  = "sql-spoke-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = azurerm_virtual_network.spoke_prod.id
  registration_enabled  = false
  
  tags = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "redis_spoke" {
  name                  = "redis-spoke-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.redis.name
  virtual_network_id    = azurerm_virtual_network.spoke_prod.id
  registration_enabled  = false
  
  tags = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "keyvault_spoke" {
  name                  = "kv-spoke-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.keyvault.name
  virtual_network_id    = azurerm_virtual_network.spoke_prod.id
  registration_enabled  = false
  
  tags = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "acr_spoke" {
  name                  = "acr-spoke-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.acr.name
  virtual_network_id    = azurerm_virtual_network.spoke_prod.id
  registration_enabled  = false
  
  tags = var.tags
}

