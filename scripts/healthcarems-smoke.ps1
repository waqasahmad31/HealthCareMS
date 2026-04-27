param(
    [string]$WebBaseUrl = "http://localhost",
    [string]$ApiBaseUrl = "http://localhost",
    [int]$TimeoutSec = 20
)

$checks = @(
    @{ Name = "Client root"; Uri = "$WebBaseUrl/" },
    @{ Name = "API liveness"; Uri = "$ApiBaseUrl/health/live" },
    @{ Name = "API readiness"; Uri = "$ApiBaseUrl/health/ready" },
    @{ Name = "API ping"; Uri = "$ApiBaseUrl/api/v1/system/ping" },
    @{ Name = "Payments page"; Uri = "$WebBaseUrl/payments" },
    @{ Name = "Lab workflow page"; Uri = "$WebBaseUrl/lab-workflow" },
    @{ Name = "Analytics page"; Uri = "$WebBaseUrl/analytics" },
    @{ Name = "Security page"; Uri = "$WebBaseUrl/security" },
    @{ Name = "Help center page"; Uri = "$WebBaseUrl/help" }
)

$hasFailure = $false

foreach ($check in $checks) {
    try {
        $response = Invoke-WebRequest -Uri $check.Uri -Method Get -UseBasicParsing -TimeoutSec $TimeoutSec
        if ($response.StatusCode -ne 200) {
            throw "Expected HTTP 200 but received $($response.StatusCode)."
        }

        Write-Host "[PASS] $($check.Name): $($check.Uri)"
    }
    catch {
        $hasFailure = $true
        Write-Error "[FAIL] $($check.Name): $($check.Uri) :: $($_.Exception.Message)"
    }
}

if ($hasFailure) {
    exit 1
}

Write-Host "Smoke verification completed successfully."
