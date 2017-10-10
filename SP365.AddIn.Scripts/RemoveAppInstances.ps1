## Remove-App.ps1  
## Remove (uninstall) all app instances for a product id on an particular web application 
##  
## Usage:  
##  
##  ## Remove an App by uninstalling all the instances of an App 
##  Remove-App -productId <ProductId> -webAppUrl <webAppUrl> 
##  
param( 
    [Guid] $productId = "{b1717448-51ef-42ad-9027-123ccbca70a8}", #[Parameter(Mandatory=$true)]
    [String] $webAppUrl = "http://sope.sopeapps.com.au" #[Parameter(Mandatory=$true)]
)

function RemoveInstances($productId = $null, $webAppUrl = $null) {
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
                    Write-Host "Uninstalling from" $web.Url; 
                    Uninstall-SPAppInstance -Identity $instance -confirm:$false 
                } 
            } 
        } 
    } 
    return ($outAppName,$outWebs) 
} 
 
$confirm = Read-Host "This will uninstall all instances of the App and is irreversible. Proceed? (y/n)" 
if($confirm -ne "y"){ 
    Exit 
} 
 
$global:appName = $null; 
$global:webs = $null; 
 
[Microsoft.SharePoint.SPSecurity]::RunWithElevatedPrivileges( 
{ 
    $returnvalue = RemoveInstances -productId $productId -webAppUrl $webAppUrl; 
    $global:appName = $returnvalue[0]; 
    $global:webs = $returnvalue[1]; 
} 
); 
 
$count = $global:webs.Count; 
if($count -gt 0){ 
    Write-Host "All the instances of the following App have been uninstalled:"; 
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
