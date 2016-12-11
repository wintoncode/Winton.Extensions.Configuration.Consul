# Taken from psake https://github.com/psake/psake and modified
param(
    [Parameter(Position=0,Mandatory=0)][bool]$VersionAndPublish = $true
)

<#  
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec  
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0)
    {
        throw ("Exec: " + $errorMessage)
    }
}

exec { & dotnet restore }
cd src\Winton.Extensions.Configuration.Consul
if ($VersionAndPublish)
{
    exec { & dotnet gitversion }
}
cd ..\..\
exec { & dotnet build src\*\project.json test\*\project.json --configuration Release }
exec { & dotnet test --no-build --configuration Release -f netcoreapp1.0 test\Winton.Extensions.Configuration.Consul.Test\project.json }
if ($VersionAndPublish)
{
    exec { & dotnet pack --no-build src\Winton.Extensions.Configuration.Consul\project.json --configuration Release }
}