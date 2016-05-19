Add-Type -Assembly System.IO.Compression.FileSystem;

$CWD = $PSScriptRoot;
$packageDirectory = Join-Path $CWD "package";
$binariesDirectory = Join-Path $packageDirectory "binaries";

$version = '1.0.0.1';
if($env:bamboo_buildNumber) { $version = "1.0.0.$env:bamboo_buildNumber"; }
Write-Host "Build Number: ${version}";
tools\nuget.exe restore -Verbosity detailed -NonInteractive;
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:publish /p:Configuration=Release /p:ApplicationVersion=$version /p:ToolsVersion=4.0 /p:OutputPath=$binariesDirectory /m;

if( $LastExitCode -ne 0)
{
	throw "MSBuild exited with error: ${LastExitCode}";
}

$Username = $env:DeploymentUsername
$Password = $env:DeploymentPassword
$EncryptedPassword = ConvertTo-SecureString -AsPlainText $Password -Force
$Credentials = New-Object System.Management.Automation.PSCredential -ArgumentList $Username,$EncryptedPassword
New-PSDrive -Credential $Credentials -Name sharedrive -PSProvider FileSystem -Root \\ShareDrive\location
Move-Item ($binariesDirectory  + 'app.publish\*') sharedrive:publish_directory -Force
