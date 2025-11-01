variable "cluster_name" {
  description = "Name of the AKS cluster"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "kubernetes_version" {
  description = "Kubernetes version"
  type        = string
}

variable "vnet_subnet_id" {
  description = "ID of the subnet for AKS nodes"
  type        = string
}

variable "network_plugin" {
  description = "Network plugin (azure or kubenet)"
  type        = string
  default     = "azure"
}

variable "network_policy" {
  description = "Network policy (calico or azure)"
  type        = string
  default     = "calico"
}

variable "service_cidr" {
  description = "Service CIDR"
  type        = string
}

variable "dns_service_ip" {
  description = "DNS service IP"
  type        = string
}

variable "docker_bridge_cidr" {
  description = "Docker bridge CIDR"
  type        = string
}

variable "system_node_pool" {
  description = "Configuration for system node pool"
  type = object({
    name                = string
    vm_size             = string
    node_count          = number
    enable_auto_scaling = bool
    min_count           = number
    max_count           = number
    max_pods            = number
    os_disk_size_gb     = number
    zones               = list(string)
  })
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
  default = {}
}

variable "enable_azure_ad_rbac" {
  description = "Enable Azure AD RBAC"
  type        = bool
  default     = true
}

variable "admin_group_object_ids" {
  description = "Azure AD group object IDs for admin access"
  type        = list(string)
  default     = []
}

variable "enable_ingress_application_gateway" {
  description = "Enable Application Gateway Ingress Controller"
  type        = bool
  default     = true
}

variable "ingress_application_gateway_subnet_id" {
  description = "Subnet ID for Application Gateway"
  type        = string
  default     = ""
}

variable "enable_azure_policy" {
  description = "Enable Azure Policy"
  type        = bool
  default     = true
}

variable "enable_oms_agent" {
  description = "Enable OMS agent"
  type        = bool
  default     = true
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace ID"
  type        = string
  default     = ""
}

variable "acr_id" {
  description = "Azure Container Registry ID"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

