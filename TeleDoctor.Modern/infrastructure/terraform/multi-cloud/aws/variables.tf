variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "eu-north-1"
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.30.0.0/16"
}

variable "availability_zones" {
  description = "Availability zones"
  type        = list(string)
  default     = ["eu-north-1a", "eu-north-1b", "eu-north-1c"]
}

variable "cluster_name" {
  description = "EKS cluster name"
  type        = string
  default     = "teledoctor-eks"
}

variable "kubernetes_version" {
  description = "Kubernetes version for EKS"
  type        = string
  default     = "1.28"
}

variable "node_instance_type" {
  description = "EC2 instance type for EKS nodes"
  type        = string
  default     = "t3.large"
}

variable "node_desired_count" {
  description = "Desired number of EKS nodes"
  type        = number
  default     = 3
}

variable "node_min_count" {
  description = "Minimum number of EKS nodes"
  type        = number
  default     = 3
}

variable "node_max_count" {
  description = "Maximum number of EKS nodes"
  type        = number
  default     = 10
}

variable "enable_transit_gateway" {
  description = "Enable Transit Gateway for hybrid connectivity"
  type        = bool
  default     = true
}

variable "bgp_asn" {
  description = "BGP ASN for AWS side"
  type        = number
  default     = 64512
}

variable "enable_site_to_site_vpn" {
  description = "Enable Site-to-Site VPN to Azure"
  type        = bool
  default     = false
}

variable "azure_vpn_gateway_ip" {
  description = "Azure VPN Gateway public IP"
  type        = string
  default     = ""
}

variable "enable_rds" {
  description = "Enable RDS PostgreSQL instance"
  type        = bool
  default     = true
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.large"
}

variable "enable_elasticache" {
  description = "Enable ElastiCache Redis"
  type        = bool
  default     = true
}

variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.medium"
}

variable "kms_key_arn" {
  description = "KMS key ARN for encryption"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {
    Project    = "TeleDoctor"
    ManagedBy  = "Terraform"
  }
}

