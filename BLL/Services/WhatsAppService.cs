using DynamicForm.Models.DTOs.WhatsApp;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicForm.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly string? _apiToken;
        private readonly string _baseUrl = "https://crm.morasalaty.net/api";
        private readonly HttpClient _httpClient;
        private readonly bool _isConfigured;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _whatsAppBaseUrl = "https://whatsapp.morasalaty.net/rest";
        private readonly string? _whatsAppToken;

        public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure HttpClient timeout and settings
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Try multiple configuration sources for old API
            _apiToken = configuration["MORASALATY_API_TOKEN"] ??
                        Environment.GetEnvironmentVariable("MORASALATY_API_TOKEN") ??
                        configuration.GetConnectionString("MORASALATY_API_TOKEN");

            // NEW: Configuration for WhatsApp REST API
            _whatsAppToken = configuration["MORASALATY_WHATSAPP_TOKEN"] ??
                             Environment.GetEnvironmentVariable("MORASALATY_WHATSAPP_TOKEN");

            _isConfigured = !string.IsNullOrEmpty(_apiToken);

            if (_isConfigured)
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "DynamicForm-API/1.0");
                _logger.LogInformation("WhatsApp service configured successfully");
            }
            else
            {
                _logger.LogWarning("WhatsApp service not configured - MORASALATY_API_TOKEN not found. WhatsApp notifications will be disabled.");
            }
        }

        public string ValidateAndFormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number is required");
            }

            // Remove all non-digit characters
            var cleanNumber = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Validate Saudi Arabia numbers
            if (cleanNumber.StartsWith("966")) // KSA
            {
                if (cleanNumber.Length != 12 || !cleanNumber.Substring(3, 1).Equals("5"))
                {
                    throw new ArgumentException("Invalid KSA phone number format. Must be 966 followed by 5xxxxxxxx");
                }

                return cleanNumber;
            }
            // Validate Egypt numbers  
            else if (cleanNumber.StartsWith("20")) // Egypt
            {
                if (cleanNumber.Length != 12 || !cleanNumber.Substring(2, 1).Equals("1"))
                {
                    throw new ArgumentException("Invalid Egypt phone number format. Must be 20 followed by 1xxxxxxxxx");
                }

                return cleanNumber;
            }
            // Handle KSA without country code
            else if (cleanNumber.StartsWith("5") && cleanNumber.Length == 9)
            {
                return "966" + cleanNumber;
            }
            // Handle Egypt without country code
            else if (cleanNumber.StartsWith("01") && cleanNumber.Length == 11)
            {
                return "20" + cleanNumber;
            }
            // Handle Egypt mobile without leading zero
            else if (cleanNumber.StartsWith("1") && cleanNumber.Length == 10)
            {
                return "20" + cleanNumber;
            }

            throw new ArgumentException("Unsupported phone number format. Please use KSA format (966-5xxxxxxxx) or Egypt format (20-1xxxxxxxxx)");
        }

        public async Task<bool> CreateSubscriberAsync(string phoneNumber, string fullName)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("WhatsApp service not configured. Skipping subscriber creation for {Phone}", phoneNumber);

                return false;
            }

            try
            {
                var formattedPhone = ValidateAndFormatPhoneNumber(phoneNumber);

                // Split full name for first/last name
                var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.FirstOrDefault() ?? "Unknown";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                var subscriberData = new
                {
                    first_name = firstName,
                    last_name = lastName,
                    name = fullName,
                    phone = formattedPhone,
                    gender = "male" // Default for now, can be enhanced later
                };

                var json = JsonSerializer.Serialize(subscriberData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating subscriber for phone: {Phone}, name: {Name}", formattedPhone, fullName);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var response = await _httpClient.PostAsync($"{_baseUrl}/subscriber/create", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Subscriber created successfully for {Phone}", formattedPhone);

                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to create subscriber for {Phone}. Status: {Status}, Response: {Response}",
                        formattedPhone, response.StatusCode, responseContent);

                    return false;
                }
            } catch (OperationCanceledException)
            {
                _logger.LogError("Timeout while creating subscriber for {Phone}", phoneNumber);

                return false;
            } catch (ObjectDisposedException)
            {
                _logger.LogError("HttpClient disposed while creating subscriber for {Phone}", phoneNumber);

                return false;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscriber for {Phone}", phoneNumber);

                return false;
            }
        }

        public async Task<bool> SendApprovalMessageAsync(string phoneNumber, Dictionary<string, string> templateParams)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("WhatsApp service not configured. Skipping message send for {Phone}", phoneNumber);

                return false;
            }

            try
            {
                var formattedPhone = ValidateAndFormatPhoneNumber(phoneNumber);

                var templateMessage = new
                {
                    user_id = formattedPhone,
                    create_if_not_found = "yes",
                    content = new
                    {
                        @namespace = "676e0a58_1340_4060_a74f_3248368335fa",
                        name = "m_4_10_1006",
                        lang = "ar",
                        @params = new Dictionary<string, object>
                        {
                            ["BODY_{{1}}"] = templateParams.GetValueOrDefault("BODY_1", ""),
                            ["BODY_{{2}}"] = templateParams.GetValueOrDefault("BODY_2", ""),
                            ["BODY_{{3}}"] = templateParams.GetValueOrDefault("BODY_3", ""),
                            ["BODY_{{4}}"] = templateParams.GetValueOrDefault("BODY_4", ""),
                            ["BODY_{{5}}"] = templateParams.GetValueOrDefault("BODY_5", ""),
                            ["BODY_{{6}}"] = templateParams.GetValueOrDefault("BODY_6", ""),
                            ["BODY_{{7}}"] = templateParams.GetValueOrDefault("BODY_7", ""),
                            ["BODY_{{8}}"] = templateParams.GetValueOrDefault("BODY_8", ""),
                            ["QUICK_REPLY_1"] = "f146755s2591243"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(templateMessage, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending WhatsApp template to {Phone} with data: {Json}", formattedPhone, json);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var response = await _httpClient.PostAsync($"{_baseUrl}/subscriber/send-whatsapp-template-by-user-id", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message sent successfully to {Phone}", formattedPhone);

                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send WhatsApp message to {Phone}. Status: {Status}, Response: {Response}",
                        formattedPhone, response.StatusCode, responseContent);

                    return false;
                }
            } catch (OperationCanceledException)
            {
                _logger.LogError("Timeout while sending WhatsApp message to {Phone}", phoneNumber);

                return false;
            } catch (ObjectDisposedException)
            {
                _logger.LogError("HttpClient disposed while sending WhatsApp message to {Phone}", phoneNumber);

                return false;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phoneNumber);

                return false;
            }
        }

        public async Task<bool> SendTemplateMessageAsync(string phoneNumber, List<string> parameters)
        {
            try
            {
                var formattedPhone = ValidateAndFormatPhoneNumber(phoneNumber);

                // Ensure we have exactly 8 parameters as required by the template
                var templateParams = new List<string>(parameters);
                while (templateParams.Count < 8) templateParams.Add(""); // Add empty strings if parameters are missing

                if (templateParams.Count > 8)
                {
                    templateParams = templateParams.Take(8).ToList(); // Trim to 8 parameters
                }

                var requestBody = new
                {
                    Token = _whatsAppToken,
                    Mobile = formattedPhone,
                    TemplateId = "0a953059-e7de-4cd8-a4e4-7d12dba9f14e",
                    HeaderUrl = "",
                    ParamsType = "8",
                    Params = templateParams,
                    Schedule = "", // Empty for immediate sending
                    ObjectId = "",
                    OrderId = "",
                    Param1 = ""
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Keep property names as-is
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending WhatsApp template to {Phone} via REST API with {ParamCount} parameters",
                    formattedPhone, templateParams.Count);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var response = await _httpClient.PostAsync($"{_whatsAppBaseUrl}/sendtemplate", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp template message sent successfully to {Phone}. Response: {Response}",
                        formattedPhone, responseContent);

                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send WhatsApp template to {Phone}. Status: {Status}, Response: {Response}",
                        formattedPhone, response.StatusCode, responseContent);

                    return false;
                }
            } catch (OperationCanceledException)
            {
                _logger.LogError("Timeout while sending WhatsApp template to {Phone}", phoneNumber);

                return false;
            } catch (ObjectDisposedException)
            {
                _logger.LogError("HttpClient disposed while sending WhatsApp template to {Phone}", phoneNumber);

                return false;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp template to {Phone}", phoneNumber);

                return false;
            }
        }

        // ✅ NEW: Send rejection notification via WhatsApp REST API
        public async Task<bool> SendRejectionMessageAsync(string phoneNumber, string rejectionReason)
        {
            if (!_isConfigured)
            {
                _logger.LogWarning("WhatsApp service not configured. Skipping rejection message send for {Phone}", phoneNumber);

                return false;
            }

            try
            {
                var formattedPhone = ValidateAndFormatPhoneNumber(phoneNumber);

                var requestBody = new
                {
                    Token = _whatsAppToken,
                    Mobile = formattedPhone,
                    TemplateId = "6d9fdb63-4fdc-420e-9d97-09846175baf0",
                    ParamsType = "0",
                    Params = new List<string>(),
                    Schedule = "",
                    ObjectId = "123456",
                    OrderId = "102565",
                    Param1 = "any value"
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending WhatsApp rejection message to {Phone}. Reason: {Reason}",
                    formattedPhone, rejectionReason);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var response = await _httpClient.PostAsync($"{_whatsAppBaseUrl}/sendtemplate", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp rejection message sent successfully to {Phone}. Response: {Response}",
                        formattedPhone, responseContent);

                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send WhatsApp rejection message to {Phone}. Status: {Status}, Response: {Response}",
                        formattedPhone, response.StatusCode, responseContent);

                    return false;
                }
            } catch (OperationCanceledException)
            {
                _logger.LogError("Timeout while sending WhatsApp rejection message to {Phone}", phoneNumber);

                return false;
            } catch (ObjectDisposedException)
            {
                _logger.LogError("HttpClient disposed while sending WhatsApp rejection message to {Phone}", phoneNumber);

                return false;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp rejection message to {Phone}", phoneNumber);

                return false;
            }
        }
    }
}