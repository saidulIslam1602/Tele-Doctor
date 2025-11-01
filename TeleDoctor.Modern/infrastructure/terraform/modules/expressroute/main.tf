# Azure ExpressRoute Module
# Provides dedicated private connectivity to Azure

resource "azurerm_public_ip" "expressroute_gateway" {
  name                = "pip-ergw-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"
  sku                 = "Standard"
  zones               = ["1", "2", "3"]
  
  tags = var.tags
}

# ExpressRoute Gateway
resource "azurerm_virtual_network_gateway" "expressroute" {
  name                = "ergw-teledoctor-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  type     = "ExpressRoute"
  vpn_type = "RouteBased"
  sku      = var.gateway_sku
  
  ip_configuration {
    name                          = "vnetGatewayConfig"
    public_ip_address_id          = azurerm_public_ip.expressroute_gateway.id
    private_ip_address_allocation = "Dynamic"
    subnet_id                     = var.gateway_subnet_id
  }
  
  tags = var.tags
}

# ExpressRoute Circuit
resource "azurerm_express_route_circuit" "main" {
  name                  = "er-teledoctor-${var.environment}"
  resource_group_name   = var.resource_group_name
  location              = var.location
  service_provider_name = var.service_provider
  peering_location      = var.peering_location
  bandwidth_in_mbps     = var.bandwidth_mbps
  
  sku {
    tier   = var.circuit_tier
    family = var.circuit_family
  }
  
  allow_classic_operations = false
  
  tags = var.tags
}

# Private Peering Configuration
resource "azurerm_express_route_circuit_peering" "private" {
  peering_type                  = "AzurePrivatePeering"
  express_route_circuit_name    = azurerm_express_route_circuit.main.name
  resource_group_name           = var.resource_group_name
  peer_asn                      = var.peer_asn
  primary_peer_address_prefix   = var.primary_peer_address_prefix
  secondary_peer_address_prefix = var.secondary_peer_address_prefix
  vlan_id                       = var.vlan_id
  shared_key                    = var.shared_key
  
  ipv4_enabled = true
}

# Microsoft Peering (for Azure PaaS services)
resource "azurerm_express_route_circuit_peering" "microsoft" {
  count                         = var.enable_microsoft_peering ? 1 : 0
  peering_type                  = "MicrosoftPeering"
  express_route_circuit_name    = azurerm_express_route_circuit.main.name
  resource_group_name           = var.resource_group_name
  peer_asn                      = var.peer_asn
  primary_peer_address_prefix   = var.microsoft_primary_peer_address_prefix
  secondary_peer_address_prefix = var.microsoft_secondary_peer_address_prefix
  vlan_id                       = var.microsoft_vlan_id
  
  ipv4_enabled = true
  
  microsoft_peering_config {
    advertised_public_prefixes = var.advertised_public_prefixes
  }
}

# Connection between ExpressRoute Circuit and Gateway
resource "azurerm_virtual_network_gateway_connection" "expressroute" {
  name                = "conn-er-teledoctor-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  type                       = "ExpressRoute"
  virtual_network_gateway_id = azurerm_virtual_network_gateway.expressroute.id
  express_route_circuit_id   = azurerm_express_route_circuit.main.id
  
  routing_weight = 10
  
  tags = var.tags
}

# Route Table for ExpressRoute
resource "azurerm_route_table" "expressroute" {
  name                          = "rt-expressroute-${var.environment}"
  location                      = var.location
  resource_group_name           = var.resource_group_name
  disable_bgp_route_propagation = false
  
  tags = var.tags
}

# Route for on-premises connectivity
resource "azurerm_route" "to_onprem" {
  name                = "route-to-onprem"
  resource_group_name = var.resource_group_name
  route_table_name    = azurerm_route_table.expressroute.name
  
  address_prefix         = var.onprem_address_prefix
  next_hop_type          = "VirtualNetworkGateway"
  next_hop_in_ip_address = null
}

