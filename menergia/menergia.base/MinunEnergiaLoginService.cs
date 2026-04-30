using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using menergiabase.Models;

namespace menergiabase.Services;

public class MinunEnergiaLoginService
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly string _username;
    private readonly string _password;

    public MinunEnergiaLoginService(string username, string password)
    {
        _username = username;
        _password = password;
        _cookieContainer = new CookieContainer();
        
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.minunenergia.fi/")
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            // First, get the login page to establish session and extract form fields
            Console.WriteLine("Fetching login page...");
            var loginPageResponse = await _httpClient.GetAsync("eServices/Online/LoggedOut");
            loginPageResponse.EnsureSuccessStatusCode();

            var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();

            // Save the login page for debugging
            await File.WriteAllTextAsync("login_page.html", loginPageHtml);
            Console.WriteLine("Login page saved to login_page.html for inspection.");

            // Parse the HTML to extract form fields
            var formData = ParseLoginForm(loginPageHtml);

            if (formData == null)
            {
                Console.WriteLine("Failed to parse login form.");
                return false;
            }

            // Add username and password to form data with correct field names
            formData["UserName"] = _username;  // Note: Capital U and N
            formData["Password"] = _password;  // Note: Capital P

            // Log the form data being sent
            Console.WriteLine("Submitting login form with fields:");
            foreach (var field in formData)
            {
                if (field.Key.ToLower() == "password")
                    Console.WriteLine($"  {field.Key}: ********");
                else if (field.Key == "__RequestVerificationToken")
                    Console.WriteLine($"  {field.Key}: {field.Value[..20]}...");
                else
                    Console.WriteLine($"  {field.Key}: {field.Value}");
            }

            // Prepare login data
            var loginData = new FormUrlEncodedContent(formData);

            // Attempt login - post to the correct endpoint
            Console.WriteLine("Posting login request to eServices/Online/Login...");
            var loginResponse = await _httpClient.PostAsync("eServices/Online/Login", loginData);

            // Check if login was successful by examining response
            var responseContent = await loginResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Response status: {loginResponse.StatusCode}");
            Console.WriteLine($"Response URL: {loginResponse.RequestMessage?.RequestUri}");

            // Successful login shows the dashboard with user's name in JavaScript
            bool isLoggedIn = loginResponse.IsSuccessStatusCode && 
                             (responseContent.Contains($"userName = \"{_username}\"", StringComparison.OrdinalIgnoreCase) ||
                              responseContent.Contains("Kirjaudu ulos", StringComparison.OrdinalIgnoreCase) ||
                              responseContent.Contains("/eServices/Online/Logout", StringComparison.OrdinalIgnoreCase));

            if (isLoggedIn)
            {
                Console.WriteLine("Login successful!");
                Console.WriteLine($"Authenticated as: {_username}");
                return true;
            }
            else
            {
                Console.WriteLine("Login failed. Please check credentials.");

                // Save response for debugging
                await File.WriteAllTextAsync("login_response.html", responseContent);
                Console.WriteLine("Response saved to login_response.html for debugging.");

                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private Dictionary<string, string>? ParseLoginForm(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var formData = new Dictionary<string, string>();

            // Find all input fields in the login form
            var inputs = doc.DocumentNode.SelectNodes("//input");

            if (inputs == null)
            {
                Console.WriteLine("No input fields found in the form.");
                return null;
            }

            foreach (var input in inputs)
            {
                var name = input.GetAttributeValue("name", "");
                var value = input.GetAttributeValue("value", "");
                var type = input.GetAttributeValue("type", "text");

                if (!string.IsNullOrEmpty(name))
                {
                    // Skip UserName field - we'll add it later with actual value
                    if (name.Equals("UserName", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Skip Password field - we'll add it later with actual value
                    if (name.Equals("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Add all other fields (including __RequestVerificationToken)
                    formData[name] = value;
                }
            }

            return formData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing form: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetAuthenticatedContentAsync(string relativePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(relativePath);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching content: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<string> PostAuthenticatedAsync(string relativePath, Dictionary<string, string> formData)
    {
        try
        {
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(relativePath, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting data: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<JsonDocument?> GetJsonAsync(string relativePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(relativePath);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching JSON: {ex.Message}");
            return null;
        }
    }

    public async Task<JsonDocument?> PostJsonAsync(string relativePath, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(relativePath, content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting JSON: {ex.Message}");
            return null;
        }
    }

    // Get customer consumption data
    public async Task<string> GetConsumptionDataAsync(
        string renderingPointCode, 
        string billingSourceCompanyCode = "LRE000",
        string networkCode = "LRE000",
        bool showOldContracts = true,
        bool loadLastYearData = true)
    {
        try
        {
            var url = $"Reporting/CustomerConsumption?renderingPointCode={renderingPointCode}" +
                     $"&BillingSourceCompanyCode={billingSourceCompanyCode}" +
                     $"&NetworkCode={networkCode}" +
                     $"&ShowOldContracts={showOldContracts}" +
                     $"&loadLastYearData={loadLastYearData}";

            Console.WriteLine($"Fetching consumption data from: {url}");
            return await GetAuthenticatedContentAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching consumption data: {ex.Message}");
            return string.Empty;
        }
    }

    // Get all consumption data for all contracts
    public async Task<string> GetAllConsumptionDataAsync()
    {
        try
        {
            var url = "Reporting/CustomerConsumption?loadLastYearData=True";
            Console.WriteLine($"Fetching all consumption data from: {url}");
            return await GetAuthenticatedContentAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching consumption data: {ex.Message}");
            return string.Empty;
        }
    }

    // Automatically fetch initial data that the frontend loads after login
    public async Task<string> GetInitialDataAsync()
    {
        try
        {
            var url = "Reporting/CustomerConsumption?showOldContracts=True&loadLastYearData=True";
            Console.WriteLine($"Fetching initial data (mimicking frontend behavior): {url}");
            return await GetAuthenticatedContentAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching initial data: {ex.Message}");
            return string.Empty;
        }
    }

    // Get invoices
    public async Task<string> GetInvoicesAsync(int yearLimit = 10)
    {
        try
        {
            var url = $"eServices/Invoice?yearLimit={yearLimit}";
            return await GetAuthenticatedContentAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching invoices: {ex.Message}");
            return string.Empty;
        }
    }

    // Get contract information
    public async Task<string> GetContractsAsync()
    {
        try
        {
            var url = "eServices/ContractInformation";
            return await GetAuthenticatedContentAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching contracts: {ex.Message}");
            return string.Empty;
        }
    }

    // Get consumption data as strongly-typed model
    public async Task<ConsumptionDataModel?> GetConsumptionDataModelAsync(
        string meteringPointCode,
        string mpSourceCompanyCode = "LRE000",
        string networkCode = "LRE000",
        bool showOldContracts = true,
        bool loadLastYearData = true)
    {
        try
        {
            var url = $"Reporting/CustomerConsumption?meteringPointCode={meteringPointCode}" +
                     $"&mpSourceCompanyCode={mpSourceCompanyCode}" +
                     $"&networkCode={networkCode}" +
                     $"&showOldContracts={showOldContracts}" +
                     $"&loadLastYearData={loadLastYearData}";

            Console.WriteLine($"Fetching consumption data from: {url}");
            var html = await GetAuthenticatedContentAsync(url);

            if (string.IsNullOrEmpty(html))
            {
                Console.WriteLine("Received empty response");
                return null;
            }

            // Save HTML for debugging
            await File.WriteAllTextAsync("consumption_response.html", html);
            Console.WriteLine("HTML response saved to consumption_response.html");

            // Extract JSON from JavaScript variable
            var json = ExtractModelFromJavaScript(html);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Failed to extract JSON from response");
                return null;
            }

            // Save extracted JSON for debugging
            await File.WriteAllTextAsync("consumption_model.json", json);
            Console.WriteLine("Extracted JSON saved to consumption_model.json");

            // Deserialize to model
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var model = JsonSerializer.Deserialize<ConsumptionDataModel>(json, options);
            Console.WriteLine($"Successfully deserialized consumption data");
            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching consumption data model: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    // Helper method to extract JSON from JavaScript embedded in HTML
    private string? ExtractModelFromJavaScript(string html)
    {
        try
        {
            // Look for pattern: var model = { ... };
            var match = Regex.Match(html, @"var\s+model\s*=\s*(\{[\s\S]*?\});", RegexOptions.Multiline);

            if (!match.Success)
            {
                Console.WriteLine("Could not find 'var model = {...}' in response");
                return null;
            }

            var json = match.Groups[1].Value;

            // Clean up JavaScript-specific syntax that isn't valid JSON

            // Replace JavaScript Date constructor with ISO string format
            // Pattern: new Date(year, month, day, hour, minute, second, millisecond)
            json = Regex.Replace(json, @"new\s+Date\((\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)", 
                m => {
                    var year = int.Parse(m.Groups[1].Value);
                    var month = int.Parse(m.Groups[2].Value) + 1; // JavaScript months are 0-based
                    var day = int.Parse(m.Groups[3].Value);
                    var hour = int.Parse(m.Groups[4].Value);
                    var minute = int.Parse(m.Groups[5].Value);
                    var second = int.Parse(m.Groups[6].Value);
                    var ms = int.Parse(m.Groups[7].Value);
                    var dt = new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Local);
                    return $"\"{dt:O}\""; // ISO 8601 format
                });

            // Pattern: new Date(year, month, day, hour, minute, second)
            json = Regex.Replace(json, @"new\s+Date\((\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)", 
                m => {
                    var year = int.Parse(m.Groups[1].Value);
                    var month = int.Parse(m.Groups[2].Value) + 1;
                    var day = int.Parse(m.Groups[3].Value);
                    var hour = int.Parse(m.Groups[4].Value);
                    var minute = int.Parse(m.Groups[5].Value);
                    var second = int.Parse(m.Groups[6].Value);
                    var dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
                    return $"\"{dt:O}\"";
                });

            // Pattern: new Date(year, month, day)
            json = Regex.Replace(json, @"new\s+Date\((\d+),\s*(\d+),\s*(\d+)\)", 
                m => {
                    var year = int.Parse(m.Groups[1].Value);
                    var month = int.Parse(m.Groups[2].Value) + 1;
                    var day = int.Parse(m.Groups[3].Value);
                    var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
                    return $"\"{dt:O}\"";
                });

            // Pattern: new Date(timestamp) - Unix timestamp in milliseconds (handles negative values)
            json = Regex.Replace(json, @"new\s+Date\((-?\d+)\)", 
                m => {
                    var timestamp = long.Parse(m.Groups[1].Value);
                    var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                    return $"\"{dt:O}\"";
                });

            // Pattern: new Date("date string")
            json = Regex.Replace(json, @"new\s+Date\(""([^""]+)""\)", "\"$1\"");

            return json;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting model from JavaScript: {ex.Message}");
            return null;
        }
    }

    // Load consumption data model from a JSON file (for testing with saved data)
    public async Task<ConsumptionDataModel?> LoadConsumptionDataModelFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var model = JsonSerializer.Deserialize<ConsumptionDataModel>(json, options);
            Console.WriteLine($"Successfully loaded consumption data from file");
            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading consumption data from file: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
