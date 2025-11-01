# Google Cloud Platform Infrastructure
# Disaster recovery site and multi-cloud demonstration

terraform {
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = var.gcp_project_id
  region  = var.gcp_region
}

# VPC Network
resource "google_compute_network" "teledoctor" {
  name                    = "teledoctor-vpc-${var.environment}"
  auto_create_subnetworks = false
  routing_mode            = "GLOBAL"
  
  description = "TeleDoctor VPC for disaster recovery and multi-cloud deployment"
}

# Subnet for GKE
resource "google_compute_subnetwork" "gke" {
  name          = "gke-subnet-${var.environment}"
  ip_cidr_range = var.gke_subnet_cidr
  region        = var.gcp_region
  network       = google_compute_network.teledoctor.id
  
  # Secondary ranges for GKE pods and services
  secondary_ip_range {
    range_name    = "pods"
    ip_cidr_range = var.gke_pods_cidr
  }
  
  secondary_ip_range {
    range_name    = "services"
    ip_cidr_range = var.gke_services_cidr
  }
  
  # Enable private Google access
  private_ip_google_access = true
  
  log_config {
    aggregation_interval = "INTERVAL_10_MIN"
    flow_sampling        = 0.5
    metadata             = "INCLUDE_ALL_METADATA"
  }
}

# Subnet for data services
resource "google_compute_subnetwork" "data" {
  name          = "data-subnet-${var.environment}"
  ip_cidr_range = var.data_subnet_cidr
  region        = var.gcp_region
  network       = google_compute_network.teledoctor.id
  
  private_ip_google_access = true
}

# Cloud Router for BGP
resource "google_compute_router" "main" {
  name    = "teledoctor-router-${var.environment}"
  network = google_compute_network.teledoctor.name
  region  = var.gcp_region
  
  bgp {
    asn               = var.bgp_asn
    advertise_mode    = "CUSTOM"
    advertised_groups = ["ALL_SUBNETS"]
    
    advertised_ip_ranges {
      range = var.gke_subnet_cidr
    }
  }
}

# Cloud NAT for outbound internet connectivity
resource "google_compute_router_nat" "main" {
  name                               = "nat-${var.environment}"
  router                             = google_compute_router.main.name
  region                             = var.gcp_region
  nat_ip_allocate_option             = "AUTO_ONLY"
  source_subnetwork_ip_ranges_to_nat = "ALL_SUBNETWORKS_ALL_IP_RANGES"
  
  log_config {
    enable = true
    filter = "ERRORS_ONLY"
  }
}

# GKE Cluster (Disaster Recovery)
resource "google_container_cluster" "dr_cluster" {
  name     = "teledoctor-dr-${var.environment}"
  location = var.gcp_region
  
  # Remove default node pool
  remove_default_node_pool = true
  initial_node_count       = 1
  
  # Network configuration
  network    = google_compute_network.teledoctor.name
  subnetwork = google_compute_subnetwork.gke.name
  
  # IP allocation for pods and services
  ip_allocation_policy {
    cluster_secondary_range_name  = "pods"
    services_secondary_range_name = "services"
  }
  
  # Private cluster configuration
  private_cluster_config {
    enable_private_nodes    = true
    enable_private_endpoint = false
    master_ipv4_cidr_block  = var.gke_master_cidr
  }
  
  # Master authorized networks
  master_authorized_networks_config {
    cidr_blocks {
      cidr_block   = var.authorized_network_cidr
      display_name = "Azure VPN"
    }
  }
  
  # Network policy
  network_policy {
    enabled  = true
    provider = "CALICO"
  }
  
  # Workload identity
  workload_identity_config {
    workload_pool = "${var.gcp_project_id}.svc.id.goog"
  }
  
  # Add-ons
  addons_config {
    http_load_balancing {
      disabled = false
    }
    
    horizontal_pod_autoscaling {
      disabled = false
    }
    
    network_policy_config {
      disabled = false
    }
    
    gcp_filestore_csi_driver_config {
      enabled = true
    }
  }
  
  # Maintenance window
  maintenance_policy {
    daily_maintenance_window {
      start_time = "03:00"
    }
  }
  
  # Resource labels
  resource_labels = {
    environment = var.environment
    project     = "teledoctor"
    managed_by  = "terraform"
  }
}

# GKE Node Pool
resource "google_container_node_pool" "primary" {
  name       = "primary-pool"
  location   = var.gcp_region
  cluster    = google_container_cluster.dr_cluster.name
  node_count = var.gke_node_count
  
  autoscaling {
    min_node_count = var.gke_min_nodes
    max_node_count = var.gke_max_nodes
  }
  
  node_config {
    machine_type = var.gke_machine_type
    disk_size_gb = 100
    disk_type    = "pd-standard"
    
    # Use container-optimized OS
    image_type = "COS_CONTAINERD"
    
    # OAuth scopes
    oauth_scopes = [
      "https://www.googleapis.com/auth/cloud-platform"
    ]
    
    # Workload identity
    workload_metadata_config {
      mode = "GKE_METADATA"
    }
    
    # Metadata
    metadata = {
      disable-legacy-endpoints = "true"
    }
    
    labels = {
      environment = var.environment
      pool        = "primary"
    }
    
    tags = ["gke-node", "teledoctor-${var.environment}"]
  }
  
  management {
    auto_repair  = true
    auto_upgrade = true
  }
}

# Cloud VPN for Azure-GCP connectivity
resource "google_compute_ha_vpn_gateway" "azure_vpn" {
  name    = "vpn-to-azure-${var.environment}"
  network = google_compute_network.teledoctor.id
  region  = var.gcp_region
}

# External VPN Gateway (represents Azure VPN Gateway)
resource "google_compute_external_vpn_gateway" "azure" {
  name            = "azure-vpn-gateway"
  redundancy_type = "SINGLE_IP_INTERNALLY_REDUNDANT"
  
  interface {
    id         = 0
    ip_address = var.azure_vpn_gateway_ip
  }
}

# VPN Tunnel to Azure
resource "google_compute_vpn_tunnel" "azure_tunnel_1" {
  name                            = "tunnel-to-azure-1"
  region                          = var.gcp_region
  vpn_gateway                     = google_compute_ha_vpn_gateway.azure_vpn.id
  peer_external_gateway           = google_compute_external_vpn_gateway.azure.id
  peer_external_gateway_interface = 0
  shared_secret                   = var.vpn_shared_secret
  router                          = google_compute_router.main.id
  vpn_gateway_interface           = 0
}

# BGP Peer for Azure VPN
resource "google_compute_router_interface" "azure_1" {
  name       = "interface-to-azure-1"
  router     = google_compute_router.main.name
  region     = var.gcp_region
  ip_range   = "169.254.21.2/30"
  vpn_tunnel = google_compute_vpn_tunnel.azure_tunnel_1.name
}

resource "google_compute_router_peer" "azure_1" {
  name                      = "peer-to-azure-1"
  router                    = google_compute_router.main.name
  region                    = var.gcp_region
  peer_ip_address           = "169.254.21.1"
  peer_asn                  = 65515  # Azure BGP ASN
  advertised_route_priority = 100
  interface                 = google_compute_router_interface.azure_1.name
}

# Partner Interconnect (for production-grade connectivity)
resource "google_compute_interconnect_attachment" "azure_interconnect" {
  count                    = var.enable_cloud_interconnect ? 1 : 0
  name                     = "interconnect-to-azure-${var.environment}"
  router                   = google_compute_router.main.id
  region                   = var.gcp_region
  type                     = "PARTNER"
  edge_availability_domain = "AVAILABILITY_DOMAIN_1"
  admin_enabled            = true
  bandwidth                = "BPS_1G"
  
  description = "Partner Interconnect to Azure for low-latency connectivity"
}

# Cloud SQL (PostgreSQL for DR)
resource "google_sql_database_instance" "main" {
  name             = "teledoctor-db-${var.environment}"
  database_version = "POSTGRES_15"
  region           = var.gcp_region
  
  settings {
    tier              = var.database_tier
    availability_type = "REGIONAL"
    disk_size         = 100
    disk_type         = "PD_SSD"
    
    backup_configuration {
      enabled                        = true
      start_time                     = "03:00"
      point_in_time_recovery_enabled = true
      backup_retention_settings {
        retained_backups = 30
      }
    }
    
    ip_configuration {
      ipv4_enabled    = false
      private_network = google_compute_network.teledoctor.id
      require_ssl     = true
    }
    
    database_flags {
      name  = "log_checkpoints"
      value = "on"
    }
  }
  
  deletion_protection = true
}

# Memorystore for Redis (DR cache)
resource "google_redis_instance" "cache" {
  name               = "teledoctor-cache-${var.environment}"
  tier               = "STANDARD_HA"
  memory_size_gb     = var.redis_memory_gb
  region             = var.gcp_region
  authorized_network = google_compute_network.teledoctor.id
  
  redis_version = "REDIS_7_0"
  display_name  = "TeleDoctor Redis Cache DR"
  
  redis_configs = {
    maxmemory-policy = "allkeys-lru"
  }
  
  maintenance_policy {
    weekly_maintenance_window {
      day = "SUNDAY"
      start_time {
        hours   = 3
        minutes = 0
      }
    }
  }
}

# Firewall Rules
resource "google_compute_firewall" "allow_internal" {
  name    = "allow-internal-${var.environment}"
  network = google_compute_network.teledoctor.name
  
  allow {
    protocol = "tcp"
    ports    = ["0-65535"]
  }
  
  allow {
    protocol = "udp"
    ports    = ["0-65535"]
  }
  
  allow {
    protocol = "icmp"
  }
  
  source_ranges = [
    var.gke_subnet_cidr,
    var.data_subnet_cidr,
    var.gke_pods_cidr,
    var.gke_services_cidr
  ]
  
  priority = 1000
}

resource "google_compute_firewall" "allow_azure" {
  name    = "allow-from-azure-${var.environment}"
  network = google_compute_network.teledoctor.name
  
  allow {
    protocol = "tcp"
  }
  
  allow {
    protocol = "udp"
  }
  
  source_ranges = ["10.0.0.0/8"]  # Azure VNets
  priority      = 1100
}

resource "google_compute_firewall" "deny_all" {
  name    = "deny-all-ingress-${var.environment}"
  network = google_compute_network.teledoctor.name
  
  deny {
    protocol = "all"
  }
  
  source_ranges = ["0.0.0.0/0"]
  priority      = 65534
}

# Cloud Monitoring for GCP resources
resource "google_monitoring_notification_channel" "email" {
  display_name = "TeleDoctor Ops Team"
  type         = "email"
  
  labels = {
    email_address = var.alert_email
  }
}

# Uptime check for GKE workloads
resource "google_monitoring_uptime_check_config" "api_health" {
  display_name = "TeleDoctor API Health Check"
  timeout      = "10s"
  period       = "60s"
  
  http_check {
    path         = "/health"
    port         = 443
    use_ssl      = true
    validate_ssl = true
  }
  
  monitored_resource {
    type = "uptime_url"
    labels = {
      project_id = var.gcp_project_id
      host       = var.api_hostname
    }
  }
}

