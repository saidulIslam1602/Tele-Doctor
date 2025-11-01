# TeleDoctor Infrastructure - Variables

variable "environment" {
  description = "Environment name (dev, staging, production)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "production"], var.environment)
    error_message = "Environment must be dev, staging, or production."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "norwayeast"
}

variable "location_short" {
  description = "Short name for location"
  type        = string
  default     = "ne"
}

variable "secondary_location" {
  description = "Secondary Azure region for DR"
  type        = string
  default     = "westeurope"
}

# Network Configuration
variable "hub_vnet_address_space" {
  description = "Address space for hub VNet"
  type        = list(string)
  default     = ["10.0.0.0/16"]
}

variable "spoke_vnet_address_spaces" {
  description = "Address spaces for spoke VNets"
  type = map(list(string))
  default = {
    production = ["10.1.0.0/16"]
    staging    = ["10.2.0.0/16"]
  }
}

variable "enable_azure_firewall" {
  description = "Enable Azure Firewall"
  type        = bool
  default     = true
}

variable "enable_vpn_gateway" {
  description = "Enable VPN Gateway"
  type        = bool
  default     = true
}

variable "enable_bastion" {
  description = "Enable Azure Bastion"
  type        = bool
  default     = true
}

# AKS Configuration
variable "kubernetes_version" {
  description = "Kubernetes version"
  type        = string
  default     = "1.28"
}

variable "system_node_pool_vm_size" {
  description = "VM size for system node pool"
  type        = string
  default     = "Standard_D4s_v3"
}

variable "system_node_pool_count" {
  description = "Initial node count for system pool"
  type        = number
  default     = 3
}

variable "system_node_pool_min_count" {
  description = "Minimum nodes for system pool"
  type        = number
  default     = 3
}

variable "system_node_pool_max_count" {
  description = "Maximum nodes for system pool"
  type        = number
  default     = 10
}

variable "user_node_pools" {
  description = "User node pools configuration"
  type = map(object({
    vm_size             = string
    node_count          = number
    enable_auto_scaling = bool
    min_count           = number
    max_count           = number
    max_pods            = number
    node_labels         = map(string)
    node_taints         = list(string)
    zones               = list(string)
  }))
  default = {
    application = {
      vm_size             = "Standard_D8s_v3"
      node_count          = 3
      enable_auto_scaling = true
      min_count           = 3
      max_count           = 20
      max_pods            = 110
      node_labels = {
        "workload" = "application"
      }
      node_taints = []
      zones       = ["1", "2", "3"]
    }
  }
}

variable "aks_admin_group_object_ids" {
  description = "Azure AD group object IDs for AKS admin access"
  type        = list(string)
  default     = []
}

# SQL Database Configuration
variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "sql_sku_name" {
  description = "SQL Database SKU"
  type        = string
  default     = "GP_Gen5_4"
}

variable "sql_max_size_gb" {
  description = "Maximum size of SQL database in GB"
  type        = number
  default     = 250
}

variable "sql_aad_admin_object_id" {
  description = "Azure AD admin object ID for SQL"
  type        = string
  default     = ""
}

# Redis Configuration
variable "redis_sku_name" {
  description = "Redis Cache SKU"
  type        = string
  default     = "Premium"
}

variable "redis_capacity" {
  description = "Redis Cache capacity"
  type        = number
  default     = 1
}

variable "redis_family" {
  description = "Redis Cache family"
  type        = string
  default     = "P"
}

# Monitoring Configuration
variable "log_analytics_sku" {
  description = "Log Analytics workspace SKU"
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_days" {
  description = "Log Analytics data retention in days"
  type        = number
  default     = 90
}

variable "alert_email_receivers" {
  description = "Email addresses for alert notifications"
  type        = list(string)
  default     = []
}

variable "alert_sms_receivers" {
  description = "SMS receivers for alert notifications"
  type = list(object({
    name         = string
    country_code = string
    phone_number = string
  }))
  default = []
}

# Container Registry Configuration
variable "acr_sku" {
  description = "Container Registry SKU"
  type        = string
  default     = "Premium"
}

variable "enable_geo_replication" {
  description = "Enable geo-replication for ACR"
  type        = bool
  default     = true
}

# Secrets
variable "jwt_secret_key" {
  description = "JWT secret key"
  type        = string
  sensitive   = true
}

# Tags
variable "tags" {
  description = "Additional tags for resources"
  type        = map(string)
  default     = {}
}

