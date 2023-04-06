locals {
  app_name = "<app-name>"
}

module "test_we" {
  source = "../module"

  app_name            = local.app_name
  vcs_repo_identifier = "iwgplc/Terraform/_git/<app-name>-infrastructure-tf"
  environment         = "test"
  region              = "we"

}
module "test_ne" {
  source = "../module"

  app_name            = local.app_name
  vcs_repo_identifier = "iwgplc/Terraform/_git/<app-name>-infrastructure-tf"
  environment         = "test"
  region              = "ne"

}
