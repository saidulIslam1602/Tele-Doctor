# AWS Infrastructure
# Alternative cloud platform demonstration

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# VPC
resource "aws_vpc" "main" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-vpc-${var.environment}"
    }
  )
}

# Internet Gateway
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-igw-${var.environment}"
    }
  )
}

# Private Subnets for EKS
resource "aws_subnet" "private" {
  count             = length(var.availability_zones)
  vpc_id            = aws_vpc.main.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index)
  availability_zone = var.availability_zones[count.index]
  
  tags = merge(
    var.tags,
    {
      Name                              = "teledoctor-private-${var.availability_zones[count.index]}"
      "kubernetes.io/role/internal-elb" = "1"
      "kubernetes.io/cluster/${var.cluster_name}" = "shared"
    }
  )
}

# Public Subnets for NAT Gateways and Load Balancers
resource "aws_subnet" "public" {
  count                   = length(var.availability_zones)
  vpc_id                  = aws_vpc.main.id
  cidr_block              = cidrsubnet(var.vpc_cidr, 8, count.index + 100)
  availability_zone       = var.availability_zones[count.index]
  map_public_ip_on_launch = true
  
  tags = merge(
    var.tags,
    {
      Name                              = "teledoctor-public-${var.availability_zones[count.index]}"
      "kubernetes.io/role/elb"          = "1"
      "kubernetes.io/cluster/${var.cluster_name}" = "shared"
    }
  )
}

# Elastic IPs for NAT Gateways
resource "aws_eip" "nat" {
  count  = length(var.availability_zones)
  domain = "vpc"
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-nat-eip-${count.index + 1}"
    }
  )
}

# NAT Gateways for outbound internet access
resource "aws_nat_gateway" "main" {
  count         = length(var.availability_zones)
  allocation_id = aws_eip.nat[count.index].id
  subnet_id     = aws_subnet.public[count.index].id
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-nat-${var.availability_zones[count.index]}"
    }
  )
  
  depends_on = [aws_internet_gateway.main]
}

# Route Tables
resource "aws_route_table" "private" {
  count  = length(var.availability_zones)
  vpc_id = aws_vpc.main.id
  
  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.main[count.index].id
  }
  
  # Route to Azure via Transit Gateway
  dynamic "route" {
    for_each = var.enable_transit_gateway ? [1] : []
    content {
      cidr_block         = "10.0.0.0/8"
      transit_gateway_id = aws_ec2_transit_gateway.main[0].id
    }
  }
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-private-rt-${count.index + 1}"
    }
  )
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id
  
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-public-rt"
    }
  )
}

# Route Table Associations
resource "aws_route_table_association" "private" {
  count          = length(var.availability_zones)
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private[count.index].id
}

resource "aws_route_table_association" "public" {
  count          = length(var.availability_zones)
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# Transit Gateway for multi-VPC and hybrid connectivity
resource "aws_ec2_transit_gateway" "main" {
  count                           = var.enable_transit_gateway ? 1 : 0
  description                     = "TeleDoctor Transit Gateway for hybrid connectivity"
  default_route_table_association = "enable"
  default_route_table_propagation = "enable"
  amazon_side_asn                 = var.bgp_asn
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-tgw-${var.environment}"
    }
  )
}

# Transit Gateway VPC Attachment
resource "aws_ec2_transit_gateway_vpc_attachment" "main" {
  count              = var.enable_transit_gateway ? 1 : 0
  subnet_ids         = aws_subnet.private[*].id
  transit_gateway_id = aws_ec2_transit_gateway.main[0].id
  vpc_id             = aws_vpc.main.id
  
  dns_support                                     = "enable"
  ipv6_support                                    = "disable"
  appliance_mode_support                          = "disable"
  transit_gateway_default_route_table_association = true
  transit_gateway_default_route_table_propagation = true
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-tgw-attachment"
    }
  )
}

# VPN Gateway for Azure connectivity
resource "aws_customer_gateway" "azure" {
  count      = var.enable_site_to_site_vpn ? 1 : 0
  bgp_asn    = 65515  # Azure BGP ASN
  ip_address = var.azure_vpn_gateway_ip
  type       = "ipsec.1"
  
  tags = merge(
    var.tags,
    {
      Name = "cgw-azure-${var.environment}"
    }
  )
}

resource "aws_vpn_connection" "azure" {
  count               = var.enable_site_to_site_vpn && var.enable_transit_gateway ? 1 : 0
  customer_gateway_id = aws_customer_gateway.azure[0].id
  transit_gateway_id  = aws_ec2_transit_gateway.main[0].id
  type                = "ipsec.1"
  
  static_routes_only = false  # Enable BGP
  
  tags = merge(
    var.tags,
    {
      Name = "vpn-to-azure-${var.environment}"
    }
  )
}

# EKS Cluster (Alternative to AKS/GKE)
resource "aws_eks_cluster" "main" {
  name     = var.cluster_name
  role_arn = aws_iam_role.eks_cluster.arn
  version  = var.kubernetes_version
  
  vpc_config {
    subnet_ids              = aws_subnet.private[*].id
    endpoint_private_access = true
    endpoint_public_access  = false
    security_group_ids      = [aws_security_group.eks_cluster.id]
  }
  
  enabled_cluster_log_types = ["api", "audit", "authenticator", "controllerManager", "scheduler"]
  
  encryption_config {
    provider {
      key_arn = var.kms_key_arn
    }
    resources = ["secrets"]
  }
  
  tags = var.tags
  
  depends_on = [
    aws_iam_role_policy_attachment.eks_cluster_policy
  ]
}

# EKS Node Group
resource "aws_eks_node_group" "main" {
  cluster_name    = aws_eks_cluster.main.name
  node_group_name = "teledoctor-nodes"
  node_role_arn   = aws_iam_role.eks_nodes.arn
  subnet_ids      = aws_subnet.private[*].id
  
  scaling_config {
    desired_size = var.node_desired_count
    min_size     = var.node_min_count
    max_size     = var.node_max_count
  }
  
  instance_types = [var.node_instance_type]
  capacity_type  = "ON_DEMAND"
  disk_size      = 100
  
  labels = {
    environment = var.environment
    role        = "application"
  }
  
  tags = var.tags
  
  depends_on = [
    aws_iam_role_policy_attachment.eks_worker_node_policy,
    aws_iam_role_policy_attachment.eks_cni_policy,
    aws_iam_role_policy_attachment.eks_container_registry_policy
  ]
}

# IAM Roles and Policies
resource "aws_iam_role" "eks_cluster" {
  name = "teledoctor-eks-cluster-role-${var.environment}"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "eks.amazonaws.com"
      }
    }]
  })
  
  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "eks_cluster_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
  role       = aws_iam_role.eks_cluster.name
}

resource "aws_iam_role" "eks_nodes" {
  name = "teledoctor-eks-node-role-${var.environment}"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "ec2.amazonaws.com"
      }
    }]
  })
  
  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "eks_worker_node_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy"
  role       = aws_iam_role.eks_nodes.name
}

resource "aws_iam_role_policy_attachment" "eks_cni_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy"
  role       = aws_iam_role.eks_nodes.name
}

resource "aws_iam_role_policy_attachment" "eks_container_registry_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
  role       = aws_iam_role.eks_nodes.name
}

# Security Groups
resource "aws_security_group" "eks_cluster" {
  name        = "teledoctor-eks-cluster-sg-${var.environment}"
  description = "Security group for EKS cluster control plane"
  vpc_id      = aws_vpc.main.id
  
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-eks-cluster-sg"
    }
  )
}

# RDS PostgreSQL (Alternative to Azure SQL)
resource "aws_db_subnet_group" "main" {
  name       = "teledoctor-db-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.private[*].id
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-db-subnet-group"
    }
  )
}

resource "aws_db_instance" "main" {
  count                  = var.enable_rds ? 1 : 0
  identifier             = "teledoctor-db-${var.environment}"
  engine                 = "postgres"
  engine_version         = "15.4"
  instance_class         = var.db_instance_class
  allocated_storage      = 100
  storage_type           = "gp3"
  storage_encrypted      = true
  kms_key_id             = var.kms_key_arn
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  multi_az               = true
  publicly_accessible    = false
  
  backup_retention_period = 30
  backup_window           = "03:00-04:00"
  maintenance_window      = "sun:04:00-sun:05:00"
  
  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]
  
  deletion_protection = true
  skip_final_snapshot = false
  final_snapshot_identifier = "teledoctor-db-final-${var.environment}"
  
  tags = var.tags
}

resource "aws_security_group" "rds" {
  name        = "teledoctor-rds-sg-${var.environment}"
  description = "Security group for RDS instance"
  vpc_id      = aws_vpc.main.id
  
  ingress {
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.eks_cluster.id]
    description     = "PostgreSQL from EKS"
  }
  
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-rds-sg"
    }
  )
}

# ElastiCache Redis
resource "aws_elasticache_subnet_group" "main" {
  name       = "teledoctor-cache-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.private[*].id
  
  tags = var.tags
}

resource "aws_elasticache_replication_group" "main" {
  count                      = var.enable_elasticache ? 1 : 0
  replication_group_id       = "teledoctor-cache-${var.environment}"
  replication_group_description = "TeleDoctor Redis cache"
  engine                     = "redis"
  engine_version             = "7.0"
  node_type                  = var.redis_node_type
  num_cache_clusters         = 2
  automatic_failover_enabled = true
  multi_az_enabled           = true
  subnet_group_name          = aws_elasticache_subnet_group.main.name
  security_group_ids         = [aws_security_group.elasticache.id]
  at_rest_encryption_enabled = true
  transit_encryption_enabled = true
  auth_token_enabled         = true
  
  snapshot_retention_limit = 7
  snapshot_window          = "03:00-05:00"
  maintenance_window       = "sun:05:00-sun:07:00"
  
  log_delivery_configuration {
    destination      = aws_cloudwatch_log_group.redis.name
    destination_type = "cloudwatch-logs"
    log_format       = "json"
    log_type         = "slow-log"
  }
  
  tags = var.tags
}

resource "aws_security_group" "elasticache" {
  name        = "teledoctor-cache-sg-${var.environment}"
  description = "Security group for ElastiCache"
  vpc_id      = aws_vpc.main.id
  
  ingress {
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_security_group.eks_cluster.id]
    description     = "Redis from EKS"
  }
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-cache-sg"
    }
  )
}

# CloudWatch Log Groups
resource "aws_cloudwatch_log_group" "redis" {
  name              = "/aws/elasticache/teledoctor-${var.environment}"
  retention_in_days = 90
  
  tags = var.tags
}

resource "aws_cloudwatch_log_group" "eks" {
  name              = "/aws/eks/teledoctor-${var.environment}/cluster"
  retention_in_days = 90
  
  tags = var.tags
}

# VPC Flow Logs
resource "aws_flow_log" "main" {
  iam_role_arn    = aws_iam_role.vpc_flow_log.arn
  log_destination = aws_cloudwatch_log_group.vpc_flow_log.arn
  traffic_type    = "ALL"
  vpc_id          = aws_vpc.main.id
  
  tags = merge(
    var.tags,
    {
      Name = "teledoctor-vpc-flow-log"
    }
  )
}

resource "aws_cloudwatch_log_group" "vpc_flow_log" {
  name              = "/aws/vpc/teledoctor-${var.environment}"
  retention_in_days = 90
  
  tags = var.tags
}

resource "aws_iam_role" "vpc_flow_log" {
  name = "teledoctor-vpc-flow-log-role-${var.environment}"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "vpc-flow-logs.amazonaws.com"
      }
    }]
  })
  
  tags = var.tags
}

resource "aws_iam_role_policy" "vpc_flow_log" {
  name = "vpc-flow-log-policy"
  role = aws_iam_role.vpc_flow_log.id
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogGroups",
        "logs:DescribeLogStreams"
      ]
      Effect   = "Allow"
      Resource = "*"
    }]
  })
}

