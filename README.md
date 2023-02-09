<!-- Improved compatibility of back to top link: See: https://github.com/othneildrew/Best-README-Template/pull/73 -->
<a name="readme-top"></a>
<!--
*** Thanks for checking out the Best-README-Template. If you have a suggestion
*** that would make this better, please fork the repo and create a pull request
*** or simply open an issue with the tag "enhancement".
*** Don't forget to give the project a star!
*** Thanks again! Now go create something AMAZING! :D
-->

<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->
[![Build][build-shield]][build-url]
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Security][security-shield]][security-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/noxorg/nox.cli">
    <img src="docs/images/NoxCli_logo.png" alt="Logo" width="150">
  </a>
<!--
<h3 align="center">Nox</h3>
-->
  <p align="center">
    <br />
    Command line tool for supporting Nox projects
    <br />
    <br />
    <a href="https://noxorg.dev/"><strong>View the documentation »</strong></a>
    <br />
    <br />
    <a href="https://github.com/noxorg/nox.cli">View Demo</a>
    ·
    <a href="https://github.com/noxorg/nox.cli/issues">Report Bug</a>
    ·
    <a href="https://github.com/noxorg/nox.cli/issues">Request Feature</a>
  </p>
</div>
    <br />

<!-- ABOUT THE PROJECT -->
## About The Project
***
Nox.Cli is a companion command-line tool that supports the core [Nox library](https://github.com/NoxOrg/Nox). Its *raison d'être* is to build upon Nox's strong efficiency focus by extending that vision to the entire Enterprise Software Roadmap. This includes fast-tracking of DevOps functions like deployment to multiple environments, setting up CI/CD pipelines and configuring resource permissions. This is achieved by integrating seamlessly with existing enterprise assets and services like Helm charts, Azure KeyVault and Active Directory.

Nox.Cli is declarative in nature with the primary aim to describe a project or solution. It uses with a range of plugin technologies in the development, infrastructure and DevOps realms to rapidly configure and deploy a solution in the enterprise.

<div align="center">
    <img src="docs/images/nox-cli-simple-overview.png" alt="Overview" width="80%">
</div>
<br />

### Main Features
---

- Accelerate and simplify the development and deployment of enterprise-grade microservices**
- Cross-platform implementation with support for Windows, Linux and MacOS
- Easy to install and run on developer/engineer desktop or integrate directly into DevOps pipeline
- Workflows use YAML-syntax and is based on GitHub Actions
- Self-documentation of project workflow to fast-track productivity of all team members that work on a project
- Open-source plugin-based architecture allows for using/customising existing actions or developing your own
- Users are identified based on Azure Login or other interactive authorisation service
- Input commands (workflows) can be project-specific or organisation-centric by way of a Tenant manifest maintained by DevOps
- Both project and Tenant KeyVaults are supported
- Can be used both for local development environment and DevOps pipeline
- Central management and deployment of common organisational scripts
- Automatically updates scripts and notifies developers of version updates
- Support secure remote task execution


** In conjunction with the Nox .NET library

### Component Diagram

<div align="center">
    <img src="docs/images/Nox - CLI Diagram v2.png" alt="Overview" width="100%">
</div>
<br />





<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
## Getting Started

The section below will walk you though installation of the Nox.Cli tool as well as a sample project to illustrate how simple it is to set up set up the associated DevOps environment.
***
### Prerequisites
---

Make sure you have .NET 6 installed on your PC.
```powershell
PS> dotnet --version
6.0.404
```
### Install the Nox.Cli tool
---
The Nox.Cli tool is hosted on nuget.org [here](https://www.nuget.org/packages/Nox.Cli) and additional installation options are detailed there. The recommended installation method is outlined below: -
```powershell
dotnet tool install --global Nox.Cli
```
Running the Nox.Cli tool installation with the global option yields the following output: -

<div align="center">
    <img src="docs/images/nox-cli-install-clean.png" alt="Overview" width="100%">
</div>

Running Nox.Cli for the first time yields the following output: -

```powershell
Nox
```

<div align="center">
    <img src="docs/images/nox-cli-run-check-credentials.png" alt="Overview" width="100%">
    <br/>
    <br/>
</div>

> 💡 You may notice from the screenshot that if you are logged in to Azure it will automatically use these credentials. In the instance that you're not logged into Azure you will be redirected to Microsoft login screen in a browser: -

<div align="center">
    <img src="docs/images/windows-login-selection.png" alt="Overview">
    <br/>
    <br/>
</div>

> 💡 The second important thing to note is that apart from the `--version` and `--logout` commands, the additional commands listed above are dynamically added from the workflows folder in your local repository or from a script repository which is linked to your organisation, hosted at `https://noxorg.dev/workflows/{tenant.id}/index.php`

## Sample Project: Setting up a Simple Environment
---

The development project built for our [Nox](https://github.com/NoxOrg/Nox) sample was a [simple currency microservice](https://github.com/NoxOrg/Nox#creating-a-project) with all the expected CRUD endpoints to add & maintain the currencies of our choice. We're going to use the Nox.Cli companion tool to set up the entire DevOps environment that accompanies that project.

### Defining the Project

Running the `Nox new` command outlines its usage options as seen below: -

```powershell
Nox new
```

<div align="center">
    <img src="docs/images/nox-cli-new.png" alt="Overview">
    <br/>
    <br/>
</div>

Let's create a folder for our project and run the `Nox new service` command from within the newly created folder: -

```powershell
mkdir CurrencyConverter
cd .\CurrencyConverter\
Nox new service
```
A series of questions follows which will determine the initial configuration of our project environment. Upon completetion of the input, these options will be saved into the project configuration file called `{service.name}.service.nox.yaml`

<div align="center">
    <img src="docs/images/nox-cli-new-input.png" alt="Overview">
    <br/>
    <br/>
</div>

```powershell
PS> ls
```

As we can see, the sercvice configuration file is now present in our default project directory. Opening this file in a code editor of your choice will reveal configuration options that echoes your input from the interactive step preceding this.

<div align="center">
    <img src="docs/images/nox-directory-service-yaml.png" alt="Overview">
    <br/>
    <br/>
</div>

The YAML file is registered with [schema.org](https://schema.org/) so we get linting and auto-completion to ensure accuracy and speed in configuring our service. The schema used to describe our sample project can be viewed [here](https://noxorg.dev/schemas/NoxConfiguration.json).

<div align="center">
    <img src="docs/images/vscode-service-yaml.png" alt="Overview">
    <br/>
</div>

### Commissioning the Environment

Now we have defined the project attributes but all we have is a simple YAML file, but no environment as yet. Well this is where the magic really starts. We'll turn our attention to the `Nox sync` command and its various options to read our project configuration file and set up the project environment as per our input.

```powershell
PS> Nox sync
```

<div align="center">
    <img src="docs/images/nox-cli-sync.png" alt="Overview">
    <br/>
</div>

#### Version Control

This step will go and check the specified DevOps server and create our version control repository if not already present.
```powershell
PS> Nox sync version-control
```
<div align="center">
    <img src="docs/images/nox-cli-sync-version-control.png" alt="Overview">
    <br/>
    <br/>
</div>

Subsequent to running this command a cursory glance at our DevOps server will reveal the newly created project.

<div align="center">
    <img src="docs/images/dev-azure-com-projects.png" alt="Overview">
    <br/>
    <br/>
</div>

```powershell
PS> Nox sync azure-active-directory
```
<div align="center">
    <img src="docs/images/nox-cli-sync-azure-active-directory.png" alt="Overview">
    <br/>
    <br/>
</div>

You'll notice that your project group have now been created on Azure Active Directory.
<div align="center">
    <img src="docs/images/portal-azure-com-group-overview.png" alt="Overview">
    <br/>
    <br/>
</div>

If you click on the Members link of the group you'll seet that the users are as per your earlier project configuration.
<div align="center">
    <img src="docs/images/portal-azure-com-group-members.png" alt="Overview">
    <br/>
    <br/>
</div>

```powershell
PS> Nox sync database
```
<div align="center">
    <img src="docs/images/nox-cli-sync-database.png" alt="Overview">
    <br/>
    <br/>
</div>

```powershell
PS> Nox sync helm-chart
```
<div align="center">
    <img src="docs/images/nox-cli-sync-helm-chart.png" alt="Overview">
    <br/>
    <br/>
</div>

Having a look at your project on the DevOps server will reveal that the Helm Chart repo have been created.

<div align="center">
    <img src="docs/images/dev-azure-com-repos-helm-chart.png" alt="Overview">
    <br/>
    <br/>
</div>

And selecting the `App.Helmchart` repo will show the relevant files.

<div align="center">
    <img src="docs/images/dev-azure-com-repos-helm-chart-detail.png" alt="Overview">
    <br/>
</div>

## Anatomy of a Manifest file

The manifest file is tasked with propogating the enterprise–level configurations related to our project. It is hosted in a tenant-specific folder on the Nox website and would typically be maintained by the DevOps team.

<div align="center">
    <img src="docs/images/vscode-manifest-yaml.png" alt="Overview">
    <br/>
    <br/>
</div>

Firstly, the `secrets:` section is where we link to our secrets provider and vault url.

The `branches:` section is where we specify the command 'categories'—for the lack of a more tech–propriate term.
You'll recall from running the `Nox` command previously that it listed the `new`, `sync` and `version` commands as options. We'll disregard the `logout` command for now as it's baked into the tool. You will however see how these three commands have been propogated from the manfiest file.

<div align="center">
    <img src="docs/images/vscode-new-service.png" alt="Overview">
    <br/>
    <br/>
</div>

Reviewing the various workflow yaml files will show how commands and their aliases are linked to the branches, outlined above. In the `NewNoxService.workflow.nox.yaml` example above, we can see how the `service` command, the `sv` alias and `|sv - Creates a new NOX app/service` description is linked to the `new` branch.

Further examination of the file will also reveal the various steps performed as part of the workflow.

<div align="center">
    <img src="docs/images/nox-cli-new-service-highlighted.png" alt="Overview">
    <br/>
    <br/>
</div>

Similarly, in the `SyncDatabaseScript.workflow.nox.yaml` example below, we can see how the `database` command, the `db` alias and `|db - Ensures hosted database and roles exist for your NOX definition` description is linked to the `sync` branch.

<div align="center">
    <img src="docs/images/vscode-sync-database.png" alt="Overview">
    <br/>
    <br/>
</div>

And the output when running the `Nox sync` command without any arguments reflect this: -

<div align="center">
    <img src="docs/images/nox-cli-sync-database-highlighted.png" alt="Overview">
    <br/>
    <br/>
</div>

Finally, the `remote-task-proxy:` section points to the url and authorisation–provide for the remote task executor which will conver in more detail later.

<!-- CONTRIBUTING -->
## Contributing
***
Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

Twitter: [@AndreSharpe72](https://twitter.com/AndreSharpe72) 

Project Link: [https://github.com/noxorg/nox.cli](https://github.com/noxorg/nox.cli)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* Nox was inspired and draws heavily from [Paul DeVito](https://github.com/pdevito3)'s very impressive [Wrapt](https://wrapt.dev/) project. Nox is essentially (a less feature-rich) Wrapt without the code generation and aims to keep developer code 100% separate from the framework, and imposes no constraints on application architechture.
* Nox would not have been possible without the many open-source projects that it draws from. The goal is to build on top of an already rich ecosystem of great libraries and tools like Microsoft's .NetCore, YamlDotNet, NewtonSoft.Json, Hangfire, Serilog, SqlKata, ETLBox, Entity Framework, MassTransit and others.

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[build-shield]: https://img.shields.io/github/actions/workflow/status/noxorg/nox.cli/nox_ci.yaml?branch=main&event=push&label=Build&style=for-the-badge
[build-url]: https://github.com/noxorg/nox.cli/actions/workflows/nox_ci.yaml?query=branch%3Amain
[contributors-shield]: https://img.shields.io/github/contributors/noxorg/nox.cli.svg?style=for-the-badge
[contributors-url]: https://github.com/noxorg/nox.cli/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/noxorg/nox.cli.svg?style=for-the-badge
[forks-url]: https://github.com/noxorg/nox.cli/network/members
[stars-shield]: https://img.shields.io/github/stars/noxorg/nox.cli.svg?style=for-the-badge
[stars-url]: https://github.com/noxorg/nox.cli/stargazers
[issues-shield]: https://img.shields.io/github/issues/noxorg/nox.cli.svg?style=for-the-badge
[issues-url]: https://github.com/noxorg/nox.cli/issues
[license-shield]: https://img.shields.io/github/license/noxorg/nox.cli.svg?style=for-the-badge
[license-url]: https://github.com/noxorg/nox.cli/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://ch.linkedin.com/in/sharpeandre
[security-shield]: https://img.shields.io/sonar/vulnerabilities/NoxOrg_Nox/main?server=https%3A%2F%2Fsonarcloud.io&style=for-the-badge
[security-url]: https://sonarcloud.io/project/security_hotspots?id=NoxOrg_Nox
[product-screenshot]: images/goo-goo.gif
[ETLBox]: https://img.shields.io/badge/ETLBox-000000?style=for-the-badge
[ETLBox-url]: https://www.etlbox.net/
[React.js]: https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB
[React-url]: https://reactjs.org/
[Bootstrap.com]: https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white
[Bootstrap-url]: https://getbootstrap.com
[Hangfire.io]: https://img.shields.io/badge/Hangfire-0769AD?style=for-the-badge
[Hangfire-url]: https://www.hangfire.io/ 
[.NET]: https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[.NET-url]: https://dotnet.microsoft.com/
[MassTransit]: https://img.shields.io/badge/MassTransit-0EA5E9?style=for-the-badge
[MassTransit-url]: https://masstransit-project.com/
[YamlDotNet]: https://img.shields.io/badge/YamlDotNet-8B0000?style=for-the-badge
[YamlDotNet-url]: https://github.com/aaubry/YamlDotNet
[AutoMapper]: https://img.shields.io/badge/AutoMapper-BE161D?style=for-the-badge
[AutoMapper-url]: https://automapper.org/
[FluentValidation]: https://img.shields.io/badge/FluentValidation-2980B9?style=for-the-badge
[FluentValidation-url]: https://docs.fluentvalidation.net/
