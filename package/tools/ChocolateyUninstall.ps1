try
{
	$serviceExecutable = Get-ChildItem -Path (Get-Item ($MyInvocation.MyCommand.Definition | split-path)).Parent.FullName -include *.exe -Recurse
	if ($serviceExecutable -eq $null -or $serviceExecutable -isnot [System.IO.FileInfo]){ throw [System.IO.FileNotFoundException] "Did not find exactly one executable: ${serviceExecutable}" }
	$serviceName = $serviceExecutable.BaseName
	
	Write-Host "Stopping the service $serviceName"
	& $serviceExecutable stop | Out-Null
	
	Write-Host "Uninstalling the service $serviceName"
	& $serviceExecutable uninstall | Out-Null
	
	Write-Host "Successfully uninstalled the service $serviceName"
}
catch
{
	Write-Host "Failed Uninstalling service: $($_.Exception.Message)"
	throw
}