# Azure Kubernetes Service Module

resource "azurerm_kubernetes_cluster" "main" {
  name                = var.cluster_name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.cluster_name
  kubernetes_version  = var.kubernetes_version
  
  # Automatic upgrades
  automatic_channel_upgrade = "stable"
  
  # System node pool
  default_node_pool {
    name                = var.system_node_pool.name
    vm_size             = var.system_node_pool.vm_size
    node_count          = var.system_node_pool.enable_auto_scaling ? null : var.system_node_pool.node_count
    enable_auto_scaling = var.system_node_pool.enable_auto_scaling
    min_count           = var.system_node_pool.enable_auto_scaling ? var.system_node_pool.min_count : null
    max_count           = var.system_node_pool.enable_auto_scaling ? var.system_node_pool.max_count : null
    max_pods            = var.system_node_pool.max_pods
    os_disk_size_gb     = var.system_node_pool.os_disk_size_gb
    zones               = var.system_node_pool.zones
    vnet_subnet_id      = var.vnet_subnet_id
    
    upgrade_settings {
      max_surge = "33%"
    }
  }
  
  # Network configuration
  network_profile {
    network_plugin     = var.network_plugin
    network_policy     = var.network_policy
    service_cidr       = var.service_cidr
    dns_service_ip     = var.dns_service_ip
    docker_bridge_cidr = var.docker_bridge_cidr
    load_balancer_sku  = "standard"
    outbound_type      = "loadBalancer"
  }
  
  # Identity
  identity {
    type = "SystemAssigned"
  }
  
  # Azure AD integration
  dynamic "azure_active_directory_role_based_access_control" {
    for_each = var.enable_azure_ad_rbac ? [1] : []
    content {
      managed                = true
      admin_group_object_ids = var.admin_group_object_ids
      azure_rbac_enabled     = true
    }
  }
  
  # Ingress Application Gateway
  dynamic "ingress_application_gateway" {
    for_each = var.enable_ingress_application_gateway ? [1] : []
    content {
      subnet_id = var.ingress_application_gateway_subnet_id
    }
  }
  
  # Azure Policy
  dynamic "azure_policy_enabled" {
    for_each = var.enable_azure_policy ? [true] : []
    content {
      enabled = true
    }
  }
  
  # OMS Agent (Azure Monitor)
  dynamic "oms_agent" {
    for_each = var.enable_oms_agent ? [1] : []
    content {
      log_analytics_workspace_id = var.log_analytics_workspace_id
    }
  }
  
  # Key Vault secrets provider
  key_vault_secrets_provider {
    secret_rotation_enabled  = true
    secret_rotation_interval = "2m"
  }
  
  # Maintenance window
  maintenance_window {
    allowed {
      day   = "Sunday"
      hours = [2, 3, 4]
    }
  }
  
  tags = var.tags
}

# User node pools
resource "azurerm_kubernetes_cluster_node_pool" "user" {
  for_each = var.user_node_pools
  
  name                  = each.key
  kubernetes_cluster_id = azurerm_kubernetes_cluster.main.id
  vm_size               = each.value.vm_size
  node_count            = each.value.enable_auto_scaling ? null : each.value.node_count
  enable_auto_scaling   = each.value.enable_auto_scaling
  min_count             = each.value.enable_auto_scaling ? each.value.min_count : null
  max_count             = each.value.enable_auto_scaling ? each.value.max_count : null
  max_pods              = each.value.max_pods
  vnet_subnet_id        = var.vnet_subnet_id
  zones                 = each.value.zones
  
  node_labels = each.value.node_labels
  node_taints = each.value.node_taints
  
  upgrade_settings {
    max_surge = "33%"
  }
  
  tags = var.tags
}

# Role assignment for ACR pull
resource "azurerm_role_assignment" "aks_acr_pull" {
  count                = var.acr_id != "" ? 1 : 0
  scope                = var.acr_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
}

# Role assignment for Network Contributor (for CNI)
resource "azurerm_role_assignment" "aks_network_contributor" {
  scope                = var.vnet_subnet_id
  role_definition_name = "Network Contributor"
  principal_id         = azurerm_kubernetes_cluster.main.identity[0].principal_id
}

