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

variable "wan_type" {
  description = "Virtual WAN type"
  type        = string
  default     = "Standard"
  validation {
    condition     = contains(["Basic", "Standard"], var.wan_type)
    error_message = "WAN type must be Basic or Standard."
  }
}

variable "primary_region" {
  description = "Primary Azure region"
  type        = string
}

variable "primary_region_short" {
  description = "Short name for primary region"
  type        = string
}

variable "secondary_region" {
  description = "Secondary Azure region for DR"
  type        = string
  default     = ""
}

variable "secondary_region_short" {
  description = "Short name for secondary region"
  type        = string
  default     = ""
}

variable "primary_hub_address_prefix" {
  description = "Address prefix for primary virtual hub"
  type        = string
  default     = "10.100.0.0/23"
}

variable "secondary_hub_address_prefix" {
  description = "Address prefix for secondary virtual hub"
  type        = string
  default     = "10.101.0.0/23"
}

variable "spoke_vnet_id" {
  description = "ID of the spoke VNet to connect"
  type        = string
}

variable "enable_multi_region" {
  description = "Enable multi-region deployment"
  type        = bool
  default     = false
}

variable "enable_internet_security" {
  description = "Enable internet security on hub connections"
  type        = bool
  default     = true
}

variable "enable_office365_breakout" {
  description = "Enable Office 365 local breakout optimization"
  type        = bool
  default     = false
}

variable "enable_firewall_in_hub" {
  description = "Deploy Azure Firewall in Virtual Hub"
  type        = bool
  default     = true
}

variable "firewall_tier" {
  description = "Azure Firewall tier in Virtual Hub"
  type        = string
  default     = "Standard"
  validation {
    condition     = contains(["Standard", "Premium"], var.firewall_tier)
    error_message = "Firewall tier must be Standard or Premium."
  }
}

variable "enable_vpn_gateway" {
  description = "Enable VPN gateway in Virtual Hub"
  type        = bool
  default     = true
}

variable "bgp_asn" {
  description = "BGP ASN for VPN gateway"
  type        = number
  default     = 65515
}

variable "enable_expressroute_gateway" {
  description = "Enable ExpressRoute gateway in Virtual Hub"
  type        = bool
  default     = false
}

variable "expressroute_scale_units" {
  description = "Scale units for ExpressRoute gateway"
  type        = number
  default     = 1
}

variable "enable_p2s_vpn" {
  description = "Enable Point-to-Site VPN"
  type        = bool
  default     = false
}

variable "p2s_scale_unit" {
  description = "Scale unit for P2S VPN gateway"
  type        = number
  default     = 1
}

variable "vpn_client_address_pools" {
  description = "Address pools for VPN clients"
  type        = list(string)
  default     = ["10.200.0.0/24"]
}

variable "vpn_root_certificate" {
  description = "Root certificate for P2S VPN authentication"
  type        = string
  default     = ""
  sensitive   = true
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

