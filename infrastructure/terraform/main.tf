# Terraform configuration for DeltaGrid Azure infrastructure
# This is a template; customize based on your Azure subscription and requirements

terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource group
resource "azurerm_resource_group" "deltagrid" {
  name     = "rg-deltagrid-prod"
  location = "West Europe"  # Change to your preferred region
}

# Key Vault
resource "azurerm_key_vault" "deltagrid" {
  name                       = "kv-deltagrid-prod"
  location                   = azurerm_resource_group.deltagrid.location
  resource_group_name        = azurerm_resource_group.deltagrid.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = true

  network_acls {
    default_action = "Deny"
    bypass         = "AzureServices"
    ip_rules       = var.allowed_ip_ranges  # Add your allowed IP ranges
  }

  tags = {
    Environment = "Production"
    Project     = "DeltaGrid"
  }
}

# Private endpoint for Key Vault
resource "azurerm_private_endpoint" "keyvault" {
  name                = "pe-kv-deltagrid"
  location            = azurerm_resource_group.deltagrid.location
  resource_group_name = azurerm_resource_group.deltagrid.name
  subnet_id           = azurerm_subnet.private.id

  private_service_connection {
    name                           = "psc-kv-deltagrid"
    private_connection_resource_id = azurerm_key_vault.deltagrid.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }
}

# App Service Plan
resource "azurerm_service_plan" "deltagrid" {
  name                = "asp-deltagrid-prod"
  location            = azurerm_resource_group.deltagrid.location
  resource_group_name = azurerm_resource_group.deltagrid.name
  os_type             = "Linux"
  sku_name            = "P1v3"  # Premium plan for better performance
}

# App Services (example for API)
resource "azurerm_linux_web_app" "api" {
  name                = "app-deltagrid-api-prod"
  location            = azurerm_resource_group.deltagrid.location
  resource_group_name = azurerm_resource_group.deltagrid.name
  service_plan_id     = azurerm_service_plan.deltagrid.id

  site_config {
    always_on         = true
    https_only        = true
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = "8.0"
    }
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "KeyVault__Url"          = azurerm_key_vault.deltagrid.vault_uri
  }

  tags = {
    Environment = "Production"
    Service     = "API"
  }
}

# Key Vault access policy for App Service
resource "azurerm_key_vault_access_policy" "api" {
  key_vault_id = azurerm_key_vault.deltagrid.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.api.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

# Azure SQL Database
resource "azurerm_mssql_server" "deltagrid" {
  name                         = "sql-deltagrid-prod"
  resource_group_name          = azurerm_resource_group.deltagrid.name
  location                     = azurerm_resource_group.deltagrid.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  minimum_tls_version         = "1.2"

  azuread_administrator {
    login_username = "DeltaGridAdmin"
    object_id      = var.azuread_admin_object_id
  }
}

resource "azurerm_mssql_database" "deltagrid" {
  name      = "db-deltagrid-prod"
  server_id = azurerm_mssql_server.deltagrid.id
  sku_name  = "S2"  # Standard tier
}

# Private endpoint for SQL
resource "azurerm_private_endpoint" "sql" {
  name                = "pe-sql-deltagrid"
  location            = azurerm_resource_group.deltagrid.location
  resource_group_name = azurerm_resource_group.deltagrid.name
  subnet_id           = azurerm_subnet.private.id

  private_service_connection {
    name                           = "psc-sql-deltagrid"
    private_connection_resource_id = azurerm_mssql_server.deltagrid.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }
}

# Virtual Network (simplified)
resource "azurerm_virtual_network" "deltagrid" {
  name                = "vnet-deltagrid-prod"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.deltagrid.location
  resource_group_name = azurerm_resource_group.deltagrid.name
}

resource "azurerm_subnet" "private" {
  name                 = "snet-private"
  resource_group_name  = azurerm_resource_group.deltagrid.name
  virtual_network_name = azurerm_virtual_network.deltagrid.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Data sources
data "azurerm_client_config" "current" {}

# Variables
variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "allowed_ip_ranges" {
  description = "Allowed IP ranges for Key Vault access"
  type        = list(string)
  default     = []
}

variable "azuread_admin_object_id" {
  description = "Azure AD administrator object ID"
  type        = string
}


