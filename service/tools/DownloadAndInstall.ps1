Param(
	[Parameter(Mandatory=$True,Position=0)]
	[String]$ArchiveName,
	[Parameter(Mandatory=$True,Position=1)]
	[String]$Key,
	[Parameter(Mandatory=$True,Position=2)]
	[String]$BuildNumber,
	[Parameter(Mandatory=$True,Position=3)]
	[String]$Version,
	[Parameter(Mandatory=$True,Position=4)]
	[String]$Username,
	[Parameter(Mandatory=$True,Position=5)]
	[String]$Password
)

#This script runs on the remote machine
$TmpDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())

Try {
	Add-Type -Assembly System.IO.Compression.FileSystem

	$PackageDirectory = "C:\Package"
	$VersionedServiceDirectory = [System.IO.Path]::Combine($PackageDirectory, $Version)
	if (-Not (Test-Path $VersionedServiceDirectory)) {
		[System.IO.Directory]::CreateDirectory($TmpDir)

		#Download the artifact to the local machine
		$ArtifactPath = "https://artifactserver/artifact/${Key}/shared/build-${BuildNumber}/artifact/${ArchiveName}?os_authType=basic"
		Write-Host "Downloading artifact from ${ArtifactPath}"
		$OutputPath = [System.IO.Path]::Combine($TmpDir, $ArchiveName)
		$WebClient = New-Object System.Net.WebClient
		$WebClient.Credentials = New-Object System.Net.Networkcredential($Username, $Password)
		$WebClient.DownloadFile($ArtifactPath, $OutputPath)

		Write-Host "Extracting $OutputPath to $VersionedServiceDirectory"
		New-Item $PackageDirectory -Type Directory -Force
		[System.IO.Compression.ZipFile]::ExtractToDirectory($OutputPath, $VersionedServiceDirectory)
	}

	$serviceExecutable = Get-ChildItem -Path $VersionedServiceDirectory -include *.exe -Recurse
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
Catch
{
  Write-Host "Failed to install the service: $($_.Exception.Message)"
  Throw
}
Finally
{
	[System.IO.Directory]::Delete($TmpDir, $True)
}
