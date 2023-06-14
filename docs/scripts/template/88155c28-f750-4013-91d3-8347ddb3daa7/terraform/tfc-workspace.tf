locals {
  app_name = "<project-snake-name>"
}

module "test_we" {
  source = "../module"

  app_name            = local.app_name
  vcs_repo_identifier = "iwgplc/<project-name>/_git/<project-dash-name>-infrastructure-tf"
  environment         = "test"
  region              = "we"

}
module "test_ne" {
  source = "../module"

  app_name            = local.app_name
  vcs_repo_identifier = "iwgplc/<project-name>/_git/<project-dash-name>-infrastructure-tf"
  environment         = "test"
  region              = "ne"

}
