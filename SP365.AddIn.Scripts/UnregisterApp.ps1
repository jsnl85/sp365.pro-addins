# NOTE: You may need to install the 'AzureAD' PS module
# Install-Module -Name AzureAD
Import-Module "AzureAD";

Write-Host "Connecting to your Azure AD.";
$conn = Connect-AzureAD;
if (-not $conn) { throw "Could not connect to your Azure AD."; exit; }
Write-Host "Connected to your Azure AD.";

function RemoveAppByClientId($clientId) {
	Write-Host "Getting the App Principal from youru Azure AD.";
	$app = Get-AzureADServicePrincipal |? { $_.AppId -eq $clientId };
	if ($app) {
		Write-Host "Unregistering the App with clientId '$($clientId)' from your Azure AD.";
		Remove-AzureADServicePrincipal -ObjectId $app.ObjectId;
	}
	else { Write-Host "App with clientId '$($clientId)' was not found in your Azure AD."; }
}

RemoveAppByClientId -clientId:"eb88d8f2-8f31-4aa9-afc8-4ef8db2beeb3";
RemoveAppByClientId -clientId:"cdcb95cd-16ee-409e-8a99-f4642c84591b";


