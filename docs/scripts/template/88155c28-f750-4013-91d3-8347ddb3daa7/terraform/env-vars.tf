module "<env-name>_variables" {
  source  = "app.terraform.io/iwgplc/tfc-variable/tfe"
  version = "1.0.0"

  variable_params = {
    # ------------------------------ West Europe ---------------------------------
    we_var01 = {
      key             = "env"
      value           = "<env-name>"
      category        = "terraform"
      description     = "Name of the environment."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var02 = {
      key             = "region"
      value           = "West Europe"
      category        = "terraform"
      description     = "Location where resources are being deployed."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var03 = {
      key             = "rp"
      value           = "we"
      category        = "terraform"
      description     = "Location prefix."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var04 = {
      key             = "tag_application_sla"
      value           = "NOSLA"
      category        = "terraform"
      description     = "TAG attribute."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var05 = {
      key             = "tag_business_criticality"
      value           = "BC1"
      category        = "terraform"
      description     = "TAG attribute."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var06 = {
      key             = "parent_workspace_name"
      value           = var.heimdall_workspace_preprod_we
      category        = "terraform"
      description     = "TFC parent workspace name, for reading state output."
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    }
    we_var07 = {
      key             = "k8s_namespace"
      value           = "<k8s-namespace>"
      category        = "terraform"
      description     = "K8s namespace of the environment"
      variable_set_id = null
      workspace_id    = module.<env-name>_we.workspaces["${local.app_name}_<env-name>_we"].id
      hcl             = null
      sensitive       = null
    },
    # ------------------------------ North Europe ---------------------------------
    ne_var01 = {
      key             = "env"
      value           = "<env-name>"
      category        = "terraform"
      description     = "Name of the environment."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var02 = {
      key             = "region"
      value           = "North Europe"
      category        = "terraform"
      description     = "Location where resources are being deployed."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var03 = {
      key             = "rp"
      value           = "ne"
      category        = "terraform"
      description     = "Location prefix."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var04 = {
      key             = "tag_application_sla"
      value           = "NOSLA"
      category        = "terraform"
      description     = "TAG attribute."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var05 = {
      key             = "tag_business_criticality"
      value           = "BC1"
      category        = "terraform"
      description     = "TAG attribute."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var06 = {
      key             = "parent_workspace_name"
      value           = var.heimdall_workspace_preprod_ne
      category        = "terraform"
      description     = "TFC parent workspace name, for reading state output."
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
    ne_var07 = {
      key             = "k8s_namespace"
      value           = "<k8s-namespace>"
      category        = "terraform"
      description     = "K8s namespace of the environment"
      variable_set_id = null
      workspace_id    = module.<env-name>_ne.workspaces["${local.app_name}_<env-name>_ne"].id
      hcl             = null
      sensitive       = null
    }
  }
}