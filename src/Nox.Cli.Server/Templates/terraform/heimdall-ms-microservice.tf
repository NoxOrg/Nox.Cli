module "heimdall_<microservice-name>_workspaces" {
  source  = "app.terraform.io/iwgplc/tfc-workspace/tfe"
  version = "1.0.5"

  org_name = var.org_name

  workspace_params = {
    #----------------------------------------------PROD----------------------------------------------------#
    #-------------------------------PROD PLATFORM P WEST EUROPE ----------------------------------#
    nox_server_prod_we = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall prod <microservice-friendly-name> microservice environment in West Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_prod_we")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this_prod.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["prod", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice-friendly-name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
    #-------------------------------PROD PLATFORM P NORTH EUROPE ----------------------------------#
    nox_server_prod_ne = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall prod <microservice-friendly-name> microservice environment in North Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_prod_ne")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this_prod.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["prod", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice-friendly-name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
    #-------------------------------UAT PLATFORM P WEST EUROPE ----------------------------------#
    nox_server_uat_we = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall uat <microservice-friendly-name> microservice environment in West Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_uat_we")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this_prod.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["uat", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice-friendly-name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
    #-------------------------------UAT PLATFORM P NORTH EUROPE ----------------------------------#
    nox_server_uat_ne = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall uat <microservice-friendly-name> microservice environment in North Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_uat_ne")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this_prod.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["uat", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice-friendly-name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
    #----------------------------------------------NONPROD----------------------------------------------------#
    #-------------------------------TEST PLATFORM N WEST EUROPE ----------------------------------#
    nox_server_test_we = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall test <microservice-friendly-name> microservice environment in West Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_test_we")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["test", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice-friendly-name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
    #-------------------------------TEST PLATFORM N NORTH EUROPE ----------------------------------#
    <microservice_name>_test_ne = {
      allow_destroy_plan            = true
      auto_apply                    = false
      description                   = "Heimdall test <microservice-friendly-name> microservice environment in North Europe"
      file_triggers_enabled         = false
      global_remote_state           = true
      name                          = lower("ws_Heimdall_<microservice-name>_test_ne")
      remote_state_consumer_ids     = null
      trigger_patterns              = null
      trigger_prefixes              = null
      force_delete                  = false
      project_id                    = module.project.projects["project_01_ms_platform"].id
      assessments_enabled           = true
      execution_mode                = "agent"
      agent_pool_id                 = data.tfe_agent_pool.this.id
      queue_all_runs                = false
      speculative_enabled           = true
      ssh_key_id                    = null
      structured_run_output_enabled = true
      tag_names                     = ["test", "<microservice-friendly-name>"]
      terraform_version             = "1.3.0"
      vcs_repo = [
        {
          branch             = "main"
          identifier         = "iwgplc/<project-name>/_git/<microservice_friendly_name>-infrastructure-tf"
          ingress_submodules = false
          oauth_token_id     = var.oauth_token_id
          tags_regex         = null
        }
      ]
      working_directory = ""
    },
  }
}