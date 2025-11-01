output "expressroute_gateway_id" {
  description = "ID of the ExpressRoute Gateway"
  value       = azurerm_virtual_network_gateway.expressroute.id
}

output "expressroute_circuit_id" {
  description = "ID of the ExpressRoute Circuit"
  value       = azurerm_express_route_circuit.main.id
}

output "expressroute_circuit_service_key" {
  description = "Service key for ExpressRoute Circuit provisioning"
  value       = azurerm_express_route_circuit.main.service_key
  sensitive   = true
}

output "private_peering_id" {
  description = "ID of the private peering configuration"
  value       = azurerm_express_route_circuit_peering.private.id
}

output "microsoft_peering_id" {
  description = "ID of the Microsoft peering configuration"
  value       = var.enable_microsoft_peering ? azurerm_express_route_circuit_peering.microsoft[0].id : null
}

output "connection_id" {
  description = "ID of the ExpressRoute connection"
  value       = azurerm_virtual_network_gateway_connection.expressroute.id
}

