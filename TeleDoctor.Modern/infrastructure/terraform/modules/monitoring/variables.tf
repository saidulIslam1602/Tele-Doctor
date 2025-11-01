variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "log_analytics_sku" {
  description = "SKU for Log Analytics workspace"
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_in_days" {
  description = "Data retention in days"
  type        = number
  default     = 90
}

variable "application_insights_type" {
  description = "Application Insights type"
  type        = string
  default     = "web"
}

variable "action_group_email_receivers" {
  description = "Email addresses for alert notifications"
  type        = list(string)
  default     = []
}

variable "action_group_sms_receivers" {
  description = "SMS receivers for alert notifications"
  type = list(object({
    name         = string
    country_code = string
    phone_number = string
  }))
  default = []
}

variable "enable_cpu_alerts" {
  description = "Enable CPU usage alerts"
  type        = bool
  default     = true
}

variable "enable_memory_alerts" {
  description = "Enable memory usage alerts"
  type        = bool
  default     = true
}

variable "enable_disk_alerts" {
  description = "Enable disk usage alerts"
  type        = bool
  default     = true
}

variable "target_resource_id" {
  description = "Target resource ID for alerts"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

