$base = 'http://localhost:5268/api/simulations'
## Use a far-future fixed StartTime to avoid transient validation edge cases
$createBody = @{ Name = 'PatchTest'; StartTime = '2100-01-01T00:00:00Z'; DataSource = 'C:\temp\sample.csv' } | ConvertTo-Json -Depth 5
Write-Host '--- CREATE ---'
try {
    $create = Invoke-WebRequest -Method Post -Uri $base -Body $createBody -ContentType 'application/json' -ErrorAction Stop
    Write-Host "Status: $($create.StatusCode)"
    Write-Host 'Headers:'
    $create.Headers.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }
    $location = $create.Headers['Location']
    $etag_create = $create.Headers['ETag']
    Write-Host "Location: $location"
    Write-Host "ETag(Create): $etag_create"
} catch {
    Write-Host 'Create failed:'
    Write-Host $_.Exception.ToString()
    exit 1
}

Write-Host '--- GET ---'
try {
    # Build absolute resource URL from returned Location header
    $resource = "http://localhost:5268" + $location
    $get = Invoke-WebRequest -Uri $resource -ErrorAction Stop
    Write-Host "Status: $($get.StatusCode)"
    $get.Headers.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }
    $etag_get = $get.Headers['ETag']
    Write-Host "ETag(Get): $etag_get"
} catch {
    Write-Host 'Get failed:'
    Write-Host $_.Exception.ToString()
    exit 1
}

$patchBody = @{ Name = 'PatchTest-Updated' } | ConvertTo-Json -Depth 5
Write-Host '--- PATCH ---'
try {
    $headers = @{}
    if ($etag_get) { $headers['If-Match'] = $etag_get }
    Write-Host "Sending If-Match header: $($headers['If-Match'])"
    $patch = Invoke-WebRequest -Method Patch -Uri $resource -Body $patchBody -ContentType 'application/json' -Headers $headers -ErrorAction Stop
    Write-Host "Status: $($patch.StatusCode)"
    $patch.Headers.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }
    Write-Host 'Body:'
    Write-Host $patch.Content
} catch {
    Write-Host 'Patch failed:'
    if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        Write-Host 'Response from server:'
        try {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $body = $sr.ReadToEnd()
            Write-Host $body
        } catch {
            Write-Host 'Could not read response body'
        }
    } else {
        Write-Host $_.Exception.ToString()
    }
}

Write-Host '--- TAIL LOG (last 200 lines) ---'
try {
    Get-Content -Path 'C:\temp\weather_api.log' -Tail 200 -ErrorAction Stop | ForEach-Object { Write-Host $_ }
} catch {
    Write-Host 'Could not read server log at C:\temp\weather_api.log'
}
