# Azure Virtual WAN Module
# Provides global transit connectivity for multi-region deployments

resource "azurerm_virtual_wan" "main" {
  name                = "vwan-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  type = var.wan_type
  
  # Office365 local breakout optimization
  office365_local_breakout_category = var.enable_office365_breakout ? "OptimizeAndAllow" : "None"
  
  tags = var.tags
}

# Virtual Hub (Primary Region)
resource "azurerm_virtual_hub" "primary" {
  name                = "vhub-${var.primary_region_short}-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.primary_region
  virtual_wan_id      = azurerm_virtual_wan.main.id
  address_prefix      = var.primary_hub_address_prefix
  
  tags = var.tags
}

# Virtual Hub (Secondary Region for DR)
resource "azurerm_virtual_hub" "secondary" {
  count               = var.enable_multi_region ? 1 : 0
  name                = "vhub-${var.secondary_region_short}-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.secondary_region
  virtual_wan_id      = azurerm_virtual_wan.main.id
  address_prefix      = var.secondary_hub_address_prefix
  
  tags = var.tags
}

# Virtual Hub Connection to Production Spoke VNet
resource "azurerm_virtual_hub_connection" "spoke_prod" {
  name                      = "conn-spoke-prod"
  virtual_hub_id            = azurerm_virtual_hub.primary.id
  remote_virtual_network_id = var.spoke_vnet_id
  
  internet_security_enabled = var.enable_internet_security
  
  routing {
    associated_route_table_id = azurerm_virtual_hub_route_table.default.id
    
    propagated_route_table {
      labels = ["default", "production"]
      route_table_ids = [
        azurerm_virtual_hub_route_table.default.id
      ]
    }
  }
}

# Default Route Table
resource "azurerm_virtual_hub_route_table" "default" {
  name           = "rt-default"
  virtual_hub_id = azurerm_virtual_hub.primary.id
  
  labels = ["default"]
}

# Azure Firewall in Virtual Hub
resource "azurerm_firewall" "vwan" {
  count               = var.enable_firewall_in_hub ? 1 : 0
  name                = "afw-vwan-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = "AZFW_Hub"
  sku_tier            = var.firewall_tier
  firewall_policy_id  = azurerm_firewall_policy.main[0].id
  
  virtual_hub {
    virtual_hub_id  = azurerm_virtual_hub.primary.id
    public_ip_count = 1
  }
  
  tags = var.tags
}

# Firewall Policy
resource "azurerm_firewall_policy" "main" {
  count               = var.enable_firewall_in_hub ? 1 : 0
  name                = "afwp-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.firewall_tier
  
  dns {
    proxy_enabled = true
  }
  
  threat_intelligence_mode = "Alert"
  
  tags = var.tags
}

# VPN Gateway in Virtual Hub
resource "azurerm_vpn_gateway" "hub" {
  count               = var.enable_vpn_gateway ? 1 : 0
  name                = "vpngw-hub-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  virtual_hub_id      = azurerm_virtual_hub.primary.id
  
  bgp_settings {
    asn         = var.bgp_asn
    peer_weight = 0
  }
  
  tags = var.tags
}

# ExpressRoute Gateway in Virtual Hub
resource "azurerm_express_route_gateway" "hub" {
  count               = var.enable_expressroute_gateway ? 1 : 0
  name                = "ergw-hub-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  virtual_hub_id      = azurerm_virtual_hub.primary.id
  scale_units         = var.expressroute_scale_units
  
  tags = var.tags
}

# Point-to-Site VPN Configuration
resource "azurerm_point_to_site_vpn_gateway" "hub" {
  count                   = var.enable_p2s_vpn ? 1 : 0
  name                    = "p2svpngw-${var.environment}"
  resource_group_name     = var.resource_group_name
  location                = var.location
  virtual_hub_id          = azurerm_virtual_hub.primary.id
  vpn_server_configuration_id = azurerm_vpn_server_configuration.main[0].id
  scale_unit              = var.p2s_scale_unit
  
  connection_configuration {
    name = "default"
    
    vpn_client_address_pool {
      address_prefixes = var.vpn_client_address_pools
    }
  }
  
  tags = var.tags
}

# VPN Server Configuration for P2S
resource "azurerm_vpn_server_configuration" "main" {
  count               = var.enable_p2s_vpn ? 1 : 0
  name                = "vpnsc-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  vpn_authentication_types = ["Certificate"]
  
  client_root_certificate {
    name             = "RootCert"
    public_cert_data = var.vpn_root_certificate
  }
  
  tags = var.tags
}

# Network Security Group for Virtual WAN
resource "azurerm_network_security_group" "vwan" {
  name                = "nsg-vwan-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  
  tags = var.tags
}

# Security rule to allow ExpressRoute traffic
resource "azurerm_network_security_rule" "allow_expressroute" {
  name                        = "Allow-ExpressRoute"
  priority                    = 100
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "*"
  source_port_range           = "*"
  destination_port_range      = "*"
  source_address_prefix       = var.onprem_address_prefix
  destination_address_prefix  = "*"
  resource_group_name         = var.resource_group_name
  network_security_group_name = azurerm_network_security_group.vwan.name
}

