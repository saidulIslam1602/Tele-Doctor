output "vpc_id" {
  description = "ID of the VPC"
  value       = aws_vpc.main.id
}

output "vpc_cidr" {
  description = "CIDR block of the VPC"
  value       = aws_vpc.main.cidr_block
}

output "private_subnet_ids" {
  description = "IDs of private subnets"
  value       = aws_subnet.private[*].id
}

output "public_subnet_ids" {
  description = "IDs of public subnets"
  value       = aws_subnet.public[*].id
}

output "eks_cluster_name" {
  description = "Name of the EKS cluster"
  value       = aws_eks_cluster.main.name
}

output "eks_cluster_endpoint" {
  description = "Endpoint of the EKS cluster"
  value       = aws_eks_cluster.main.endpoint
  sensitive   = true
}

output "eks_cluster_certificate_authority" {
  description = "Certificate authority data for EKS cluster"
  value       = aws_eks_cluster.main.certificate_authority[0].data
  sensitive   = true
}

output "transit_gateway_id" {
  description = "ID of the Transit Gateway"
  value       = var.enable_transit_gateway ? aws_ec2_transit_gateway.main[0].id : null
}

output "vpn_connection_id" {
  description = "ID of the VPN connection to Azure"
  value       = var.enable_site_to_site_vpn && var.enable_transit_gateway ? aws_vpn_connection.azure[0].id : null
}

output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = var.enable_rds ? aws_db_instance.main[0].endpoint : null
  sensitive   = true
}

output "redis_endpoint" {
  description = "ElastiCache Redis endpoint"
  value       = var.enable_elasticache ? aws_elasticache_replication_group.main[0].primary_endpoint_address : null
  sensitive   = true
}

output "nat_gateway_ips" {
  description = "Public IPs of NAT Gateways"
  value       = aws_eip.nat[*].public_ip
}

