output "hub_vnet_id" {
  description = "ID of the hub virtual network"
  value       = azurerm_virtual_network.hub.id
}

output "hub_vnet_name" {
  description = "Name of the hub virtual network"
  value       = azurerm_virtual_network.hub.name
}

output "spoke_vnet_ids" {
  description = "IDs of spoke virtual networks"
  value = {
    production = azurerm_virtual_network.spoke_prod.id
  }
}

output "aks_subnet_id" {
  description = "ID of the AKS subnet"
  value       = azurerm_subnet.aks.id
}

output "data_subnet_id" {
  description = "ID of the data subnet"
  value       = azurerm_subnet.data.id
}

output "appgw_subnet_id" {
  description = "ID of the Application Gateway subnet"
  value       = azurerm_subnet.appgw.id
}

output "privatelink_subnet_id" {
  description = "ID of the Private Link subnet"
  value       = azurerm_subnet.privatelink.id
}

output "firewall_private_ip" {
  description = "Private IP of Azure Firewall"
  value       = var.enable_firewall ? azurerm_firewall.main[0].ip_configuration[0].private_ip_address : null
}

output "vpn_gateway_id" {
  description = "ID of the VPN Gateway"
  value       = var.enable_vpn_gateway ? azurerm_virtual_network_gateway.vpn[0].id : null
}

output "sql_private_dns_zone_id" {
  description = "ID of SQL private DNS zone"
  value       = azurerm_private_dns_zone.sql.id
}

output "redis_private_dns_zone_id" {
  description = "ID of Redis private DNS zone"
  value       = azurerm_private_dns_zone.redis.id
}

output "keyvault_private_dns_zone_id" {
  description = "ID of Key Vault private DNS zone"
  value       = azurerm_private_dns_zone.keyvault.id
}

output "acr_private_dns_zone_id" {
  description = "ID of ACR private DNS zone"
  value       = azurerm_private_dns_zone.acr.id
}

