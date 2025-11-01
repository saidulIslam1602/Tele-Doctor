variable "gcp_project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "gcp_region" {
  description = "GCP region for resources"
  type        = string
  default     = "europe-west1"
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "gke_subnet_cidr" {
  description = "CIDR for GKE subnet"
  type        = string
  default     = "10.20.0.0/24"
}

variable "gke_pods_cidr" {
  description = "CIDR for GKE pods"
  type        = string
  default     = "10.21.0.0/16"
}

variable "gke_services_cidr" {
  description = "CIDR for GKE services"
  type        = string
  default     = "10.22.0.0/16"
}

variable "data_subnet_cidr" {
  description = "CIDR for data subnet"
  type        = string
  default     = "10.20.1.0/24"
}

variable "gke_master_cidr" {
  description = "CIDR for GKE master nodes"
  type        = string
  default     = "172.16.0.0/28"
}

variable "gke_machine_type" {
  description = "Machine type for GKE nodes"
  type        = string
  default     = "e2-standard-4"
}

variable "gke_node_count" {
  description = "Initial number of GKE nodes"
  type        = number
  default     = 3
}

variable "gke_min_nodes" {
  description = "Minimum number of GKE nodes"
  type        = number
  default     = 3
}

variable "gke_max_nodes" {
  description = "Maximum number of GKE nodes"
  type        = number
  default     = 10
}

variable "database_tier" {
  description = "Cloud SQL database tier"
  type        = string
  default     = "db-custom-4-16384"
}

variable "redis_memory_gb" {
  description = "Redis memory size in GB"
  type        = number
  default     = 5
}

variable "bgp_asn" {
  description = "BGP ASN for Cloud Router"
  type        = number
  default     = 65516
}

variable "azure_vpn_gateway_ip" {
  description = "Azure VPN Gateway public IP address"
  type        = string
  default     = ""
}

variable "vpn_shared_secret" {
  description = "Shared secret for VPN tunnel to Azure"
  type        = string
  sensitive   = true
  default     = ""
}

variable "authorized_network_cidr" {
  description = "Authorized network for GKE master access"
  type        = string
  default     = "10.0.0.0/8"
}

variable "enable_cloud_interconnect" {
  description = "Enable Cloud Interconnect to Azure"
  type        = bool
  default     = false
}

variable "alert_email" {
  description = "Email address for monitoring alerts"
  type        = string
  default     = "ops@teledoctor.no"
}

variable "api_hostname" {
  description = "API hostname for uptime monitoring"
  type        = string
  default     = "api.teledoctor.no"
}

