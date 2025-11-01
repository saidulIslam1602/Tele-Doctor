output "virtual_wan_id" {
  description = "ID of the Virtual WAN"
  value       = azurerm_virtual_wan.main.id
}

output "primary_hub_id" {
  description = "ID of the primary Virtual Hub"
  value       = azurerm_virtual_hub.primary.id
}

output "secondary_hub_id" {
  description = "ID of the secondary Virtual Hub"
  value       = var.enable_multi_region ? azurerm_virtual_hub.secondary[0].id : null
}

output "firewall_id" {
  description = "ID of the Azure Firewall in Virtual Hub"
  value       = var.enable_firewall_in_hub ? azurerm_firewall.vwan[0].id : null
}

output "vpn_gateway_id" {
  description = "ID of the VPN Gateway in Virtual Hub"
  value       = var.enable_vpn_gateway ? azurerm_vpn_gateway.hub[0].id : null
}

output "expressroute_gateway_id" {
  description = "ID of the ExpressRoute Gateway in Virtual Hub"
  value       = var.enable_expressroute_gateway ? azurerm_express_route_gateway.hub[0].id : null
}

output "p2s_vpn_gateway_id" {
  description = "ID of the Point-to-Site VPN Gateway"
  value       = var.enable_p2s_vpn ? azurerm_point_to_site_vpn_gateway.hub[0].id : null
}

