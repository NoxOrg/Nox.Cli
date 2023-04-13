################## Mandatory (Input variables) #################
variable "env" {
  description = "Name of the environment."
}

variable "region" {
  description = "Location where resources are being deployed."
}

variable "rp" {
  description = "Location prefix."
}

################## Azure Tags ###################
variable "tag_application_sla" {
  description = "TAG: Application_Sla"
}

variable "tag_business_criticality" {
  description = "TAG: Business_Criticality"
}


################## TF code specific (Input variables) ################
variable "parent_workspace_name" {
  description = "TFC parent workspace name, for reading state output"
}

variable "k8s_namespace" {
  description = "K8s namesapce name"
}

variable "microservice_name_short" {
  description = "Microservice name short version."
  default     = "<project-short-name>"
}

variable "microservice_name" {
  description = "microservice name"
  default     = "<project-pascal-name>"
}

variable "AZURE_TENANT_ID" {
  description = "Azure IWG tenant"
}
variable "AZURE_CLIENT_ID" {
  description = "Azure SP ID"
}
variable "AZURE_CLIENT_SECRET" {
  description = "Azure SP secret"
}

variable "aad_server_id" {
  description = "Azure AD server ID"
  default     = "6dae42f8-4368-4678-94ff-3960e28e3630"
}

variable "null_resource_trigger" {
  type        = bool
  description = "Trigger toggle for null resource."
  default     = true
}

variable "role_binded_aad_group_id" {
  description = "The AAD group for developers."
  default     = "62fbb65a-4d98-41ae-af34-84ad250405bd"
}

variable "kv_public_nacl_enabled" {
  type        = bool
  description = "Toggle switch of KV firewall."
  default     = true
}