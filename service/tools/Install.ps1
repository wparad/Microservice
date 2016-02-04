try {
	$serviceExecutable = Get-ChildItem -Path (Get-Item ($MyInvocation.MyCommand.Definition | split-path)).Parent.FullName -include *.exe -Recurse
	if ($serviceExecutable -eq $null -or $serviceExecutable -isnot [System.IO.FileInfo]){ throw [System.IO.FileNotFoundException] "Did not find exactly one executable: ${serviceExecutable}" }
	$filePath = $serviceExecutable.FullName
	$serviceName = $serviceExecutable.BaseName

	#Uninstall service if it already exists. Stops the service first if it's running  
	& $serviceExecutable stop | Out-Null
	& $serviceExecutable uninstall | Out-Null

	Write-Host "Installing the service $serviceName"
	& $serviceExecutable install | Out-Null

	Write-Host "Starting the service $serviceName"
	& $serviceExecutable start | Out-Null

	Write-Host "Successful installed the service $serviceName"
}
catch
{
  Write-Host "Failed to install the service: $($_.Exception.Message)"
  throw
}