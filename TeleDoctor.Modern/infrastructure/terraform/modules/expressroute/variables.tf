variable "environment" {
  description = "Environment name"
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

variable "gateway_subnet_id" {
  description = "ID of the gateway subnet"
  type        = string
}

variable "gateway_sku" {
  description = "SKU for ExpressRoute gateway"
  type        = string
  default     = "Standard"
  validation {
    condition     = contains(["Standard", "HighPerformance", "UltraPerformance", "ErGw1AZ", "ErGw2AZ", "ErGw3AZ"], var.gateway_sku)
    error_message = "Gateway SKU must be valid ExpressRoute SKU."
  }
}

variable "service_provider" {
  description = "ExpressRoute service provider name"
  type        = string
  default     = "Equinix"
}

variable "peering_location" {
  description = "ExpressRoute peering location"
  type        = string
  default     = "Oslo"
}

variable "bandwidth_mbps" {
  description = "ExpressRoute circuit bandwidth in Mbps"
  type        = number
  default     = 1000
  validation {
    condition     = contains([50, 100, 200, 500, 1000, 2000, 5000, 10000], var.bandwidth_mbps)
    error_message = "Bandwidth must be a valid ExpressRoute bandwidth."
  }
}

variable "circuit_tier" {
  description = "ExpressRoute circuit tier"
  type        = string
  default     = "Standard"
  validation {
    condition     = contains(["Standard", "Premium"], var.circuit_tier)
    error_message = "Circuit tier must be Standard or Premium."
  }
}

variable "circuit_family" {
  description = "ExpressRoute circuit billing family"
  type        = string
  default     = "MeteredData"
  validation {
    condition     = contains(["MeteredData", "UnlimitedData"], var.circuit_family)
    error_message = "Circuit family must be MeteredData or UnlimitedData."
  }
}

variable "peer_asn" {
  description = "BGP ASN for ExpressRoute peering"
  type        = number
  default     = 65515
}

variable "primary_peer_address_prefix" {
  description = "Primary peer address prefix (/30)"
  type        = string
  default     = "192.168.10.0/30"
}

variable "secondary_peer_address_prefix" {
  description = "Secondary peer address prefix (/30)"
  type        = string
  default     = "192.168.10.4/30"
}

variable "vlan_id" {
  description = "VLAN ID for private peering"
  type        = number
  default     = 100
  validation {
    condition     = var.vlan_id >= 1 && var.vlan_id <= 4094
    error_message = "VLAN ID must be between 1 and 4094."
  }
}

variable "shared_key" {
  description = "Shared key for peering authentication"
  type        = string
  default     = null
  sensitive   = true
}

variable "enable_microsoft_peering" {
  description = "Enable Microsoft peering for Azure PaaS services"
  type        = bool
  default     = false
}

variable "microsoft_primary_peer_address_prefix" {
  description = "Microsoft peering primary address prefix"
  type        = string
  default     = "192.168.11.0/30"
}

variable "microsoft_secondary_peer_address_prefix" {
  description = "Microsoft peering secondary address prefix"
  type        = string
  default     = "192.168.11.4/30"
}

variable "microsoft_vlan_id" {
  description = "VLAN ID for Microsoft peering"
  type        = number
  default     = 200
}

variable "advertised_public_prefixes" {
  description = "Public IP prefixes to advertise via Microsoft peering"
  type        = list(string)
  default     = []
}

variable "onprem_address_prefix" {
  description = "On-premises network address prefix"
  type        = string
  default     = "192.168.0.0/16"
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

