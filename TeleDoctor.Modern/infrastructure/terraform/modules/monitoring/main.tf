# Monitoring and Observability Module

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.log_analytics_sku
  retention_in_days   = var.log_analytics_retention_in_days
  
  tags = var.tags
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "appi-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = var.application_insights_type
  retention_in_days   = var.log_analytics_retention_in_days
  
  tags = var.tags
}

# Action Group for alerts
resource "azurerm_monitor_action_group" "main" {
  name                = "ag-teledoctor-${var.environment}"
  resource_group_name = var.resource_group_name
  short_name          = "teledoctor"
  
  dynamic "email_receiver" {
    for_each = var.action_group_email_receivers
    content {
      name                    = "Email-${email_receiver.key}"
      email_address           = email_receiver.value
      use_common_alert_schema = true
    }
  }
  
  dynamic "sms_receiver" {
    for_each = var.action_group_sms_receivers
    content {
      name         = sms_receiver.value.name
      country_code = sms_receiver.value.country_code
      phone_number = sms_receiver.value.phone_number
    }
  }
  
  tags = var.tags
}

# Metric Alert - High CPU Usage
resource "azurerm_monitor_metric_alert" "cpu_high" {
  count               = var.enable_cpu_alerts && var.target_resource_id != "" ? 1 : 0
  name                = "alert-cpu-high-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [var.target_resource_id]
  description         = "Alert when CPU usage is above 80%"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_cpu_usage_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = var.tags
}

# Metric Alert - High Memory Usage
resource "azurerm_monitor_metric_alert" "memory_high" {
  count               = var.enable_memory_alerts && var.target_resource_id != "" ? 1 : 0
  name                = "alert-memory-high-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [var.target_resource_id]
  description         = "Alert when memory usage is above 85%"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_memory_working_set_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = var.tags
}

# Metric Alert - Disk Usage
resource "azurerm_monitor_metric_alert" "disk_high" {
  count               = var.enable_disk_alerts && var.target_resource_id != "" ? 1 : 0
  name                = "alert-disk-high-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [var.target_resource_id]
  description         = "Alert when disk usage is above 90%"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_disk_usage_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 90
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = var.tags
}

# Log Analytics Solutions
resource "azurerm_log_analytics_solution" "container_insights" {
  solution_name         = "ContainerInsights"
  resource_group_name   = var.resource_group_name
  location              = var.location
  workspace_resource_id = azurerm_log_analytics_workspace.main.id
  workspace_name        = azurerm_log_analytics_workspace.main.name
  
  plan {
    publisher = "Microsoft"
    product   = "OMSGallery/ContainerInsights"
  }
  
  tags = var.tags
}

resource "azurerm_log_analytics_solution" "security_center" {
  solution_name         = "SecurityCenterFree"
  resource_group_name   = var.resource_group_name
  location              = var.location
  workspace_resource_id = azurerm_log_analytics_workspace.main.id
  workspace_name        = azurerm_log_analytics_workspace.main.name
  
  plan {
    publisher = "Microsoft"
    product   = "OMSGallery/SecurityCenterFree"
  }
  
  tags = var.tags
}

