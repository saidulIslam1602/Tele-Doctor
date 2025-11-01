output "vpc_network_id" {
  description = "ID of the VPC network"
  value       = google_compute_network.teledoctor.id
}

output "vpc_network_name" {
  description = "Name of the VPC network"
  value       = google_compute_network.teledoctor.name
}

output "gke_cluster_name" {
  description = "Name of the GKE cluster"
  value       = google_container_cluster.dr_cluster.name
}

output "gke_cluster_endpoint" {
  description = "Endpoint of the GKE cluster"
  value       = google_container_cluster.dr_cluster.endpoint
  sensitive   = true
}

output "gke_cluster_ca_certificate" {
  description = "CA certificate of the GKE cluster"
  value       = google_container_cluster.dr_cluster.master_auth[0].cluster_ca_certificate
  sensitive   = true
}

output "cloud_sql_connection_name" {
  description = "Cloud SQL instance connection name"
  value       = google_sql_database_instance.main.connection_name
}

output "cloud_sql_private_ip" {
  description = "Private IP of Cloud SQL instance"
  value       = google_sql_database_instance.main.private_ip_address
  sensitive   = true
}

output "redis_host" {
  description = "Redis instance host"
  value       = google_redis_instance.cache.host
  sensitive   = true
}

output "redis_port" {
  description = "Redis instance port"
  value       = google_redis_instance.cache.port
}

output "cloud_router_id" {
  description = "ID of the Cloud Router"
  value       = google_compute_router.main.id
}

output "vpn_gateway_id" {
  description = "ID of the HA VPN Gateway"
  value       = google_compute_ha_vpn_gateway.azure_vpn.id
}

output "interconnect_attachment_id" {
  description = "ID of the Cloud Interconnect attachment"
  value       = var.enable_cloud_interconnect ? google_compute_interconnect_attachment.azure_interconnect[0].id : null
}

output "interconnect_pairing_key" {
  description = "Pairing key for Cloud Interconnect"
  value       = var.enable_cloud_interconnect ? google_compute_interconnect_attachment.azure_interconnect[0].pairing_key : null
  sensitive   = true
}

