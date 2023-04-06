module "heimdall_<app-snake-name>_workspaces" {
  source = "./heimdall-ms-<app-dash-name>"

  heimdall_workspace_preprod_we = module.Heimdall_Workspaces.workspaces["preprod_we"].name
  heimdall_workspace_preprod_ne = module.Heimdall_Workspaces.workspaces["preprod_ne"].name
  heimdall_workspace_prod_we    = module.Heimdall_Workspaces.workspaces["prod_we"].name
  heimdall_workspace_prod_ne    = module.Heimdall_Workspaces.workspaces["prod_ne"].name

}