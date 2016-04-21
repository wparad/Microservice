Try {
	$Username = $env:DeploymentUsername
	$Password = $env:DeploymentPassword
	$Version = $env:version
	Write-Host "Starting deployment ${Version} from ${Username}."
	$zipfilename = "package.zip"
	$ApplicationMachine = "remote-machine"

	$EncryptedPassword = ConvertTo-SecureString -AsPlainText $Password -Force
	$Credentials = New-Object System.Management.Automation.PSCredential -ArgumentList $Username,$EncryptedPassword
	$Result = Invoke-Command -ComputerName $ApplicationMachine -Credential $Credentials -FilePath ([IO.Path]::Combine($PSScriptRoot, "scripts", "Install.ps1")) -ArgumentList $zipfilename,$env:bamboo_planKey,$env:bamboo_buildNumber,$env:bamboo_deploy_version,$Username,$Password

	Write-Host "Results: ${Result}"
}
Catch {
	Write-Host $_.Exception.Message
	Write-Host $_.Exception.ItemName
	Throw;
}
