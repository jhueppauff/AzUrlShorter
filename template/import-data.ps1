# Import data to table storage

if (Get-Module -ListAvailable -Name AzTable) {
    Write-Host "Module exists, skipping install"
} 
else {
    Write-Host "Module does not exist, installing ..."
    Install-Module AzTable
}

$storageAccountName = "sturlshorterdevweu001"
$tableName = "domains"
$resourceGroupName = "rg-urlshorter-dev-weu-001"

$ctx = (Get-AzStorageAccount -Name $storageAccountName -ResourceGroupName $resourceGroupName).Context

$cloudTable = (Get-AzStorageTable –Name $tableName –Context $ctx).CloudTable

Get-AzTableRow -table $cloudTable | Remove-AzTableRow -table $cloudTable 

$json = Get-Content './domains.json' | ConvertFrom-Json

foreach ($item in $json) {
    Add-AzTableRow -table $cloudTable -PartitionKey $item -RowKey 1
}