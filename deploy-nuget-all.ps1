<#
    deploy-nuget-all.ps1

    Calls all "deploy-nuget-*.ps1" scripts in the repository root (excluding deploy-nuget-base.ps1)
    Executes each script in sequence and continues even if a script fails.

    Usage: run this script from the repository root or anywhere; it resolves the script directory
           and invokes the child scripts.
#>

        param(
            [Parameter(Mandatory=$true)]
            [string]$ApiKey
        )

        # Inform about presence (do not print the key itself)
        Write-Host "ApiKey provided (will be passed to child scripts via -ApiKey parameter)."

try
{
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
catch
{
    # Fallback: if running in an environment where MyInvocation isn't available
    $scriptDir = (Get-Location).Path
}

Write-Host "Deploy NuGet - All: Searching for deploy-nuget-*.ps1 in '$scriptDir'"

$exclude = @('deploy-nuget-base.ps1','deploy-nuget-all.ps1')

$scripts = Get-ChildItem -Path $scriptDir -Filter 'deploy-nuget-*.ps1' -File |
    Where-Object { $exclude -notcontains $_.Name } |
    Sort-Object Name

if (-not $scripts)
{
    Write-Host "No deploy-nuget-*.ps1 scripts found. Nothing to do."
    exit 0
}

$results = @()

foreach ($s in $scripts)
{
    $path = $s.FullName
    Write-Host ""
    Write-Host "------------------------------------------------------------"
    Write-Host ("Calling script: {0}" -f $s.Name)

    try
    {
        # Prefer running each script in a separate PowerShell process so that failures
        # in the child script do not terminate this controller script and exit codes
        # are reported reliably.

        $pwExe = $null

        if (Get-Command -Name pwsh -ErrorAction SilentlyContinue)
        {
            $pwExe = 'pwsh'
        }
        elseif (Get-Command -Name powershell -ErrorAction SilentlyContinue)
        {
            $pwExe = 'powershell'
        }

        if ($pwExe)
        {
            # Escape single quotes in values so the embedded command remains valid
            $escapedKey = $ApiKey.Replace("'","''")
            $escapedPath = $path.Replace("'","''")

            # Invoke the child script and pass -ApiKey parameter explicitly
            $cmd = "& '$escapedPath' -ApiKey '$escapedKey'"

            & $pwExe -NoProfile -ExecutionPolicy Bypass -Command $cmd
            $exitCode = $LASTEXITCODE
        }
        else
        {
            # Fallback: call in current process and pass parameter
            & $path -ApiKey $ApiKey
            $exitCode = $LASTEXITCODE
        }

        if ($null -eq $exitCode)
        {
            # If child didn't set an exit code, treat non-terminating errors via $?
            $exitCode = 0
        }

        $success = ($? -and ($exitCode -eq 0))

        if ($success)
        {
            Write-Host ("OK: {0} (ExitCode: {1})" -f $s.Name, $exitCode)
        }
        else
        {
            Write-Host ("FAILED: {0} (ExitCode: {1})" -f $s.Name, $exitCode)
        }

        $results += [PSCustomObject]@{
            Script = $s.Name
            Path = $path
            ExitCode = $exitCode
            Success = $success
        }
    }
    catch
    {
        Write-Host ("ERROR while calling {0}: {1}" -f $s.Name, $_.Exception.Message)
        $results += [PSCustomObject]@{
            Script = $s.Name
            Path = $path
            ExitCode = -1
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Summary
Write-Host "------------------------------------------------------------"
Write-Host "Summary:"

foreach ($r in $results)
{
    if ($r.Success)
    {
        Write-Host ("{0,-60} : {1}" -f $r.Script, "Succeeded")
    }
    else
    {
        Write-Host ("{0,-60} : {1} (ExitCode: {2})" -f $r.Script, "Failed", $r.ExitCode)
    }
}

$failCount = ($results | Where-Object { -not $_.Success }).Count
if ($failCount -gt 0)
{
    Write-Host ""
    Write-Host ("Finished with {0} failed script(s)." -f $failCount)
}
else
{
    Write-Host ""
    Write-Host "Finished all scripts successfully."
}

# Always exit with 0 so the caller can decide; the script continues on failures as requested.
exit 0

