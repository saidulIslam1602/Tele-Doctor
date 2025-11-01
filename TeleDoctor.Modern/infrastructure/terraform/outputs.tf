# TeleDoctor Infrastructure - Outputs

# Resource Group
output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "resource_group_location" {
  description = "Location of the resource group"
  value       = azurerm_resource_group.main.location
}

# Networking
output "hub_vnet_id" {
  description = "ID of the hub virtual network"
  value       = module.networking.hub_vnet_id
}

output "spoke_vnet_ids" {
  description = "IDs of spoke virtual networks"
  value       = module.networking.spoke_vnet_ids
}

output "firewall_private_ip" {
  description = "Private IP address of Azure Firewall"
  value       = module.networking.firewall_private_ip
  sensitive   = true
}

output "vpn_gateway_id" {
  description = "ID of the VPN Gateway"
  value       = module.networking.vpn_gateway_id
}

# AKS
output "aks_cluster_name" {
  description = "Name of the AKS cluster"
  value       = module.aks.cluster_name
}

output "aks_cluster_id" {
  description = "ID of the AKS cluster"
  value       = module.aks.cluster_id
}

output "aks_cluster_fqdn" {
  description = "FQDN of the AKS cluster"
  value       = module.aks.cluster_fqdn
}

output "aks_cluster_identity" {
  description = "Identity of the AKS cluster"
  value       = module.aks.cluster_identity
  sensitive   = true
}

output "aks_ingress_application_gateway_id" {
  description = "ID of the Application Gateway used for ingress"
  value       = module.aks.ingress_application_gateway_id
}

# SQL Database
output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL server"
  value       = module.sql.server_fqdn
}

output "sql_database_names" {
  description = "Names of SQL databases"
  value       = module.sql.database_names
}

# Redis Cache
output "redis_hostname" {
  description = "Hostname of the Redis cache"
  value       = module.redis.hostname
  sensitive   = true
}

output "redis_ssl_port" {
  description = "SSL port of the Redis cache"
  value       = module.redis.ssl_port
}

# Monitoring
output "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace"
  value       = module.monitoring.log_analytics_workspace_id
}

output "log_analytics_workspace_name" {
  description = "Name of the Log Analytics workspace"
  value       = module.monitoring.log_analytics_workspace_name
}

output "application_insights_instrumentation_key" {
  description = "Instrumentation key for Application Insights"
  value       = module.monitoring.application_insights_instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Connection string for Application Insights"
  value       = module.monitoring.application_insights_connection_string
  sensitive   = true
}

# Key Vault
output "key_vault_id" {
  description = "ID of the Key Vault"
  value       = module.keyvault.key_vault_id
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = module.keyvault.key_vault_uri
}

# Container Registry
output "acr_login_server" {
  description = "Login server of the Container Registry"
  value       = module.acr.login_server
}

output "acr_id" {
  description = "ID of the Container Registry"
  value       = module.acr.acr_id
}

# Kubeconfig
output "kube_config" {
  description = "Kubernetes configuration for AKS cluster"
  value       = module.aks.kube_config
  sensitive   = true
}

