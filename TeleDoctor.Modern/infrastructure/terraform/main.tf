# TeleDoctor Modern - Azure Infrastructure
# Multi-region deployment with hub-spoke network topology

terraform {
  required_version = ">= 1.6.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.45"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.23"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.11"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "teledoctor-tfstate-rg"
    storage_account_name = "teledoctortfstate"
    container_name       = "tfstate"
    key                  = "terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true
    }
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Data sources
data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-teledoctor-${var.environment}-${var.location_short}"
  location = var.location
  
  tags = local.common_tags
}

# Hub-Spoke Network Architecture
module "networking" {
  source = "./modules/networking"
  
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  environment         = var.environment
  
  hub_vnet_address_space    = var.hub_vnet_address_space
  spoke_vnet_address_spaces = var.spoke_vnet_address_spaces
  
  enable_firewall    = var.enable_azure_firewall
  enable_vpn_gateway = var.enable_vpn_gateway
  enable_bastion     = var.enable_bastion
  
  tags = local.common_tags
}

# Azure Kubernetes Service
module "aks" {
  source = "./modules/aks"
  
  cluster_name        = "aks-teledoctor-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  
  kubernetes_version = var.kubernetes_version
  
  # Network configuration
  vnet_subnet_id     = module.networking.aks_subnet_id
  network_plugin     = "azure"
  network_policy     = "calico"
  service_cidr       = "172.16.0.0/16"
  dns_service_ip     = "172.16.0.10"
  docker_bridge_cidr = "172.17.0.1/16"
  
  # System node pool
  system_node_pool = {
    name                = "system"
    vm_size             = var.system_node_pool_vm_size
    node_count          = var.system_node_pool_count
    enable_auto_scaling = true
    min_count           = var.system_node_pool_min_count
    max_count           = var.system_node_pool_max_count
    max_pods            = 110
    os_disk_size_gb     = 128
    zones               = ["1", "2", "3"]
  }
  
  # User node pools for application workloads
  user_node_pools = var.user_node_pools
  
  # RBAC and Identity
  enable_azure_ad_rbac = true
  admin_group_object_ids = var.aks_admin_group_object_ids
  
  # Add-ons
  enable_ingress_application_gateway = true
  ingress_application_gateway_subnet_id = module.networking.appgw_subnet_id
  
  enable_azure_policy = true
  enable_oms_agent    = true
  log_analytics_workspace_id = module.monitoring.log_analytics_workspace_id
  
  tags = local.common_tags
  
  depends_on = [module.networking]
}

# Azure SQL Database with Private Endpoint
module "sql" {
  source = "./modules/sql"
  
  server_name         = "sql-teledoctor-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  
  administrator_login    = var.sql_admin_username
  administrator_password = var.sql_admin_password
  
  # Database configuration
  databases = {
    teledoctor = {
      sku_name                    = var.sql_sku_name
      max_size_gb                 = var.sql_max_size_gb
      zone_redundant              = true
      geo_backup_enabled          = true
      backup_retention_days       = 35
      long_term_retention_enabled = true
    }
  }
  
  # Private endpoint configuration
  enable_private_endpoint = true
  private_endpoint_subnet_id = module.networking.data_subnet_id
  private_dns_zone_ids    = [module.networking.sql_private_dns_zone_id]
  
  # Security
  enable_azure_ad_authentication = true
  azure_ad_admin_object_id       = var.sql_aad_admin_object_id
  
  enable_threat_detection = true
  enable_vulnerability_assessment = true
  
  tags = local.common_tags
}

# Redis Cache for distributed caching
module "redis" {
  source = "./modules/redis"
  
  name                = "redis-teledoctor-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  
  sku_name            = var.redis_sku_name
  capacity            = var.redis_capacity
  family              = var.redis_family
  enable_non_ssl_port = false
  
  # Private endpoint
  enable_private_endpoint    = true
  private_endpoint_subnet_id = module.networking.data_subnet_id
  private_dns_zone_ids       = [module.networking.redis_private_dns_zone_id]
  
  # High availability
  zones = ["1", "2", "3"]
  
  tags = local.common_tags
}

# Monitoring and Observability
module "monitoring" {
  source = "./modules/monitoring"
  
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  environment         = var.environment
  
  log_analytics_sku               = var.log_analytics_sku
  log_analytics_retention_in_days = var.log_analytics_retention_days
  
  application_insights_type = "web"
  
  # Alert configuration
  action_group_email_receivers = var.alert_email_receivers
  action_group_sms_receivers   = var.alert_sms_receivers
  
  # Metric alerts
  enable_cpu_alerts    = true
  enable_memory_alerts = true
  enable_disk_alerts   = true
  
  tags = local.common_tags
}

# Azure Key Vault for secrets management
module "keyvault" {
  source = "./modules/keyvault"
  
  name                = "kv-teledoctor-${var.environment}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  
  tenant_id = data.azurerm_client_config.current.tenant_id
  
  # Network configuration
  enable_private_endpoint    = true
  private_endpoint_subnet_id = module.networking.data_subnet_id
  private_dns_zone_ids       = [module.networking.keyvault_private_dns_zone_id]
  
  # RBAC access
  enable_rbac_authorization = true
  
  # Secrets to create
  secrets = {
    sql-connection-string = azurerm_mssql_database.main.connection_string
    redis-connection-string = module.redis.primary_connection_string
    jwt-secret-key = var.jwt_secret_key
  }
  
  # Soft delete and purge protection
  soft_delete_retention_days = 90
  enable_purge_protection    = true
  
  tags = local.common_tags
}

# Container Registry
module "acr" {
  source = "./modules/acr"
  
  name                = "acrteledoctor${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  
  sku = var.acr_sku
  
  # Geo-replication
  georeplications = var.enable_geo_replication ? [
    {
      location                = var.secondary_location
      zone_redundancy_enabled = true
    }
  ] : []
  
  # Private endpoint
  enable_private_endpoint    = true
  private_endpoint_subnet_id = module.networking.data_subnet_id
  private_dns_zone_ids       = [module.networking.acr_private_dns_zone_id]
  
  # Security
  enable_admin_user = false
  
  # Content trust
  enable_content_trust = true
  
  tags = local.common_tags
}

# Random suffix for unique naming
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

# Local variables
locals {
  common_tags = merge(
    var.tags,
    {
      Environment = var.environment
      ManagedBy   = "Terraform"
      Project     = "TeleDoctor"
      CostCenter  = "Healthcare"
    }
  )
}

