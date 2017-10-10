## Get-AppInstances.ps1  
## Get all app instances for a product id on a particular web application 
##  
## Usage:  
##  
##  ## Get all App instances 
##  Get-AppInstances -productId <ProductId> -webAppUrl <webAppUrl> 
##  
param( 
    [Guid] $productId = "{b1717448-51ef-42ad-9027-123ccbca70a8}", #[Parameter(Mandatory=$true)]
    [String] $webAppUrl = "http://sope.sopeapps.com.au" #[Parameter(Mandatory=$true)]
)
 
function GetAllInstances($productId = $null, $webAppUrl = $null)  { 
    $outAppName = ""; 
    $sites  = Get-SPSite -WebApplication $webAppUrl 
    $outWebs = @() 
    foreach($site in $sites){ 
        if($site.AdministrationSiteType -ne "None"){ 
            continue; 
        } 
        $webs = Get-SPWeb -site $site 
        foreach($web in $webs) { 
            $appinstances = Get-SPAppInstance -Web $web 
            foreach($instance in $appinstances) { 
                if($productId -eq $instance.App.ProductId) { 
                    if ($outAppName -eq "") { 
                        $outAppName = $instance.Title; 
                    } 
                    $outWebs += $web; 
                } 
            } 
        } 
    } 
    return ($outAppName,$outWebs) 
} 
Write-Host "This script will search all the sites in the webAppUrl for installed instances of the App." 
$confirm = Read-Host "This can take a while. Proceed? (y/n)" 
if($confirm -ne "y"){ 
    Exit 
} 
 
$global:appName = $null; 
$global:webs = $null; 
 
[Microsoft.SharePoint.SPSecurity]::RunWithElevatedPrivileges( 
{ 
    $returnvalue = GetAllInstances -productId $productId -webAppUrl $webAppUrl; 
    $global:appName = $returnvalue[0]; 
    $global:webs = $returnvalue[1]; 
} 
); 
 
$count = $global:webs.Count; 
if($count -gt 0){ 
    Write-Host "App Name:" $global:appName; 
    Write-Host "Product Id: $productId"; 
    Write-Host "Number of instances: $count"; 
    Write-Host ""; 
    Write-Host "Urls:"; 
 
    foreach($web in $global:webs) { 
        Write-Host $web.Url;     
    } 
} 
else { 
    Write-Host "No instances of the App with Product Id $productId found."; 
} 
return; 
