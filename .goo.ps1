<# goo.ps1 - Type less. Code more.

    Develop, build, test and run helper script built on Powershell

    Developed by Andre Sharpe on October, 24 2020.

    www.goo.dev

    1. '.\.goo' will output the comment headers for each implemented command
    
    2. Add a function with its purpose in its comment header to extend this project's goo file 

    3. 'goo <function>' will run your commands 
#>

<# --- NEW GOO INSTANCE --- #>

using module '.\.goo\goo.psm1'

$goo = [Goo]::new($args)


<# --- SET GLOBAL SCRIPT VARIABLES HERE --- #>

$script:SolutionName            = 'Nox.Cli'

$script:RootFolder              = (Get-Location).Path
$script:SourceFolder            = '.\src'
$script:SamplesFolder          	= '.\samples'
$script:TestsFolder            	= '.\tests'
$script:SolutionFolder          = $script:SourceFolder
$script:SolutionFile            = "$script:SolutionFolder\Nox.Cli.sln"
$script:ProjectFolder           = "$script:SolutionFolder\Nox.Cli"
$script:ProjectFile             = "$script:ProjectFolder\Nox.Cli.csproj"
$script:ServerFolder            = "$script:SolutionFolder\Nox.Cli.Server"
$script:ServerFile              = "$script:SolutionFolder\Nox.Cli.Server.csproj"

$script:DefaultEnvironment      = 'Development'

$script:DockerContainerName     = 'nox'

<# --- SET YOUR PROJECT'S ENVIRONMENT VARIABLES HERE --- #>

if($null -eq $Env:Environment)
{
    $Env:ENVIRONMENT = $script:DefaultEnvironment
    $Env:ASPNETCORE_ENVIRONMENT = $script:DefaultEnvironment
}


<# --- ADD YOUR COMMAND DEFINITIONS HERE --- #>

<# 
    A good 'init' command will ensure a freshly cloned project will run first time.
    Guide the developer to do so easily. Check for required tools. Install them if needed. Set magic environment variables if needed.
    This should ideally replace your "Getting Started" section in your README.md
    Type less. Code more. (And get your team or collaboraters started quickly and productively!)
#>

# command: goo init | Run this command first, or to reset project completely. 
$goo.Command.Add( 'init', {
    $goo.Command.Run( 'clean' )
    $goo.Command.Run( 'build' )
    $goo.Command.Run( 'run', "--configuration Release --no-build" )
})

# command: goo clean | Removes data and build output
$goo.Command.Add( 'clean', {
    $goo.Console.WriteInfo( "Cleaning data and distribution folders..." )
    $goo.IO.EnsureRemoveFolder("./dist")
    $goo.Command.RunExternal('dotnet','clean --verbosity:quiet --nologo',$script:SolutionFolder)
    $goo.Command.RunExternal('dotnet','restore --verbosity:quiet --nologo',$script:SolutionFolder)
    $goo.StopIfError("Failed to clean previous builds. (Release)")
})


# command: goo build | Builds the solution and command line app. 
$goo.Command.Add( 'build', {
    $goo.Console.WriteInfo("Building solution...")
    $goo.Command.RunExternal('dotnet','build /clp:ErrorsOnly --warnaserror --configuration Release', $script:SolutionFolder)
    $goo.StopIfError("Failed to build solution. (Release)")
    $goo.Command.RunExternal('dotnet','publish --configuration Release --output ../dist --no-build', $script:SolutionFolder)
    $goo.StopIfError("Failed to publish CLI project. (Release)")
})

# command: goo run [<cliParameters>]| Run the console application
$goo.Command.Add( 'run', { param([string]$dotNetOptions, [string]$cliOptions)
    $goo.Console.WriteLine("Starting: dotnet run $dotNetOptions")
    $goo.Command.RunExternal('dotnet',"run $dotNetOptions", $script:ProjectFolder)
})

# command: goo listen | Start the Cli server 
$goo.Command.Add( 'listen', {
    $goo.Console.WriteLine( "starting the Cli Server at https://localhost:8000..." )
    $goo.Command.RunExternal('dotnet','run',$script:ServerFolder)
})

# command: goo env | Show all environment variables
$goo.Command.Add( 'env', { param($dbEnvironment,$dbInstance)
    $goo.Console.WriteLine( "environment variables" )
    $goo.Console.WriteLine( "=====================" )
    Get-ChildItem -Path Env: | Sort-Object -Property Name | Out-Host

    $goo.Console.WriteLine( "dotnet user-secrets" )
    $goo.Console.WriteLine( "===================" )
    $goo.Console.WriteLine() 
    $goo.Command.RunExternal('dotnet',"user-secrets list --project $script:SolutionFolder")
})

# command: goo setenv <env> | Sets local environment to <env> environment
$goo.Command.Add( 'setenv', { param( $Environment )
    $oldEnv = $Env:ENVIRONMENT
    $Env:ENVIRONMENT = $Environment
    $Env:ASPNETCORE_ENVIRONMENT = $Environment
    $goo.Console.WriteInfo("Environment changed from [$oldEnv] to [$Env:ENVIRONMENT]")
})

# command: goo dev | Start up Visual Studio and VS Code for solution
$goo.Command.Add( 'dev', { 
    $goo.Command.StartProcess($script:SolutionFile)
    $goo.Command.StartProcess('code','.')
})

# command: goo feature <name> | Creates a new feature branch from your main git branch
$goo.Command.Add( 'feature', { param( $featureName )
    $goo.Git.CheckoutFeature($featureName)
})

# command: goo push <message> | Performs 'git add -A', 'git commit -m <message>', 'git -u push origin'
$goo.Command.Add( 'push', { param( $message )
    $current = $goo.Git.CurrentBranch()
    $head = $goo.Git.HeadBranch()
    if($head -eq $current) {
        $goo.Error("You can't push directly to the '$head' branch")
    }
    else {
        $goo.Git.AddCommitPushRemote($message)
    }
})

# command: goo main | Checks out the main branch and prunes features removed at origin
$goo.Command.Add( 'main', { param( $featureName )
    $goo.Git.CheckoutMain()
})

### some versioning helpers. TODO: move to goo project at some point

## extract a version object (file, xpath, value) table from all csproj files
$goo.Command.Add( 'get-project-version-table', {
    $xpaths = $args
    $files = (Get-ChildItem -Filter "*.csproj" -Recurse)
    $xml = New-Object XML
    $versionInfoTable = @()
    foreach($file in $files){
        $xml.Load($file)
        foreach($xpath in $xpaths){ 
            $node = $xml.SelectSingleNode($xpath)
            if($null -ne $node){
                $version = (($node.InnerText ?? $node.Value) -split '\.')
                $versionInfo = [pscustomobject]@{file=$file;xpath=$xpath;version=$version}
                $versionInfoTable += $versionInfo
            }
        }
    }
    return $versionInfoTable;
})

## get highest frequency version, first three segments only
$goo.Command.Add( 'get-project-version-vote', { param($versionInfoTable)
    $versionCounter = @{}
    foreach($versionInfo in $versionInfoTable){
        $version = ($versionInfo.version[0..2] -join '.')
        if($versionCounter.ContainsKey($version)){
            $versionCounter[$version]++
        } else {
            $versionCounter[$version] = 0
        }
    }
    $maxVal = -1
    $maxKey = $null
    foreach($key in $versionCounter.Keys){
        if($versionCounter[$key] -gt $maxVal){
            $maxVal = $versionCounter[$key]
            $maxKey = $key
        }
    }
    return $maxKey
})

## set the version from version table
$goo.Command.Add( 'set-project-version', { param( $versionInfoTable, $version )
    $xml = New-Object XML
    $currentFile = $null
    foreach($versionInfo in $versionInfoTable){
        if($currentFile -ne $versionInfo.file){
            if($null -ne $currentFile) { $xml.Save($currentFile) }
            $currentFile = $versionInfo.file
            $xml.Load($currentFile)
        }
        $node = $xml.SelectSingleNode($versionInfo.xpath)
        if($null -ne $node){
            $versionNew = (($node.InnerText ?? $node.Value) -split '\.')
            for($i=0; ($i -lt $versionNew.Length) -and ($i -lt $version.Length); $i++){
                $versionNew[$i] = $version[$i]
            }
            $node.InnerText = ($versionNew -join '.')
        }
        $relativePath = Resolve-Path -Path $versionInfo.file -Relative 
        $goo.Console.WriteLine("Bumping $relativePath to version $($versionNew -join '.') ($($versionInfo.xpath))..." )
    }
    if($null -ne $currentFile) { $xml.Save($currentFile) }

})

# command: goo bump-version [<version>]| Sets or increments the project version
$goo.Command.Add( 'bump-version', { param($version)
    $versionInfoTable = $goo.Command.Run('get-project-version-table', 
        @("//AssemblyVersion","//FileVersion","//PackageVersion")
    )
    $versionArray = $null;
    if($null -eq $version){
        $version = $goo.Command.Run('get-project-version-vote', $versionInfoTable)
        $versionArray = ($version -split '\.')
        $versionArray[2] = [string]([int]$versionArray[2]+1)
    } else {
        $versionArray = ($version -split '\.')
    }
    $goo.Command.Run('set-project-version', @($versionInfoTable, $versionArray))
})

$goo.Command.Add( 'publish', { 

    $goo.Error("This command is depricated and now handled by Github Actions.")

    $goo.Console.WriteInfo("Updating version for ($script:SourceFolder\Nox.Lib) and dependancies...")
    $goo.Command.Run( 'bump-version' )

    $goo.Console.WriteInfo("Compiling project ($script:SourceFolder\Nox.Lib)...")
    $goo.Command.RunExternal('dotnet','build /clp:ErrorsOnly --warnaserror --configuration Release', "$script:SourceFolder\Nox.Lib")
    $goo.StopIfError("Failed to build solution. (Release)")

    $goo.Console.WriteInfo("Packing project ($script:SourceFolder\Nox.Lib)...")
    $goo.Command.RunExternal('dotnet','pack /clp:ErrorsOnly --no-build --configuration Release', "$script:SourceFolder\Nox.Lib")
    $goo.StopIfError("Failed to pack Nox.Lib (Release)")

    $goo.Console.WriteInfo("Publishing project ($script:SourceFolder\Nox.Lib) to Nuget.org...")
    $nupkgFile = Get-ChildItem "$script:SourceFolder\Nox.Lib\bin\Release\Nox.Lib.*.nupkg" | Sort-Object -Property LastWriteTime | Select-Object -Last 1
    $goo.Command.RunExternal('dotnet',"nuget push $($nupkgFile.FullName) --api-key $Env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json", "$script:SourceFolder\Nox.Lib")
    $goo.StopIfError("Failed to publish Nox.Lib to nuget. (Release)")

})

<# --- START GOO EXECUTION --- #>

$goo.Start()



<# --- EOF --- #>
