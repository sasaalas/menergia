# MinunEnergia API Test Script
# This script demonstrates how to interact with the MinunEnergia API

Write-Host "MinunEnergia API Test Script" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000"
$apiUrl = "$baseUrl/api/consumption"

Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host "API URL: $apiUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Health Check
Write-Host "Test 1: Health Check Endpoint" -ForegroundColor Green
Write-Host "------------------------------" -ForegroundColor Green
try {
    $health = Invoke-RestMethod -Uri "$apiUrl/health" -Method Get
    Write-Host "Status: " -NoNewline
    Write-Host $health.status -ForegroundColor Green
    Write-Host "Timestamp: $($health.timestamp)"
} catch {
    Write-Host "Error: API is not running or not accessible" -ForegroundColor Red
    Write-Host "Please start the API using: cd MinunEnergia.Api; dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test 2: Current Month Consumption
Write-Host "Test 2: Current Month Consumption" -ForegroundColor Green
Write-Host "----------------------------------" -ForegroundColor Green
try {
    $consumption = Invoke-RestMethod -Uri "$apiUrl/current-month" -Method Get

    Write-Host "Period: " -NoNewline
    Write-Host $consumption.period -ForegroundColor Cyan
    Write-Host ""

    Write-Host "Consumption:" -ForegroundColor Yellow
    Write-Host "  Total: $($consumption.totalConsumption) $($consumption.unit)"
    Write-Host "  Cost: $($consumption.totalCost) $($consumption.currency)"
    Write-Host ""

    if ($consumption.previousYearComparison) {
        Write-Host "Previous Year Comparison:" -ForegroundColor Yellow
        Write-Host "  Previous: $($consumption.previousYearComparison.previousYearConsumption) $($consumption.unit)"
        Write-Host "  Difference: $($consumption.previousYearComparison.difference) $($consumption.unit)"
        Write-Host "  $($consumption.previousYearComparison.differenceText)"
        Write-Host ""
    }

    if ($consumption.temperature) {
        Write-Host "Temperature Data:" -ForegroundColor Yellow
        Write-Host "  Min: $($consumption.temperature.minimum) $($consumption.temperature.unit)"
        Write-Host "  Avg: $($consumption.temperature.average) $($consumption.temperature.unit)"
        Write-Host "  Max: $($consumption.temperature.maximum) $($consumption.temperature.unit)"
    }

} catch {
    Write-Host "Error retrieving consumption data: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Green

# Display the raw JSON response
Write-Host ""
Write-Host "Raw JSON Response:" -ForegroundColor Yellow
Write-Host "------------------" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/current-month" -Method Get
    $response | ConvertTo-Json -Depth 10 | Write-Host
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
