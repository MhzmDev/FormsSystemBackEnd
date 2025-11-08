using DynamicForm.Models.DTOs.WhatsApp;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicForm.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly string _apiToken;
        private readonly string _baseUrl = "https://crm.morasalaty.net/api";
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiToken = configuration["MORASALATY_API_TOKEN"] ??
                        throw new InvalidOperationException("MORASALATY_API_TOKEN environment variable is required");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public string ValidateAndFormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number is required");
            }

            // Remove all non-digit characters
            var cleanNumber = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Handle different country codes for testing
            if (cleanNumber.StartsWith("966")) // KSA
            {
                if (cleanNumber.Length != 12)
                {
                    throw new ArgumentException("Invalid KSA phone number format");
                }

                return cleanNumber;
            }
            else if (cleanNumber.StartsWith("20")) // Egypt (for testing)
            {
                if (cleanNumber.Length < 12 || cleanNumber.Length > 13)
                {
                    throw new ArgumentException("Invalid Egypt phone number format");
                }

                return cleanNumber;
            }
            else if (cleanNumber.StartsWith("5") && cleanNumber.Length == 9) // KSA without country code
            {
                return "966" + cleanNumber;
            }
            else if (cleanNumber.StartsWith("01") && cleanNumber.Length == 11) // Egypt without country code
            {
                return "20" + cleanNumber;
            }

            throw new ArgumentException("Unsupported phone number format. Please use KSA (+966) or Egypt (+20) format");
        }

        public async Task<bool> CreateSubscriberAsync(string phoneNumber, string fullName)
        {
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

                var json = JsonSerializer.Serialize(subscriberData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating subscriber for phone: {Phone}, name: {Name}", formattedPhone, fullName);

                var response = await _httpClient.PostAsync($"{_baseUrl}/subscriber/create", content);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscriber for {Phone}", phoneNumber);

                return false;
            }
        }

        public async Task<bool> SendApprovalMessageAsync(string phoneNumber, Dictionary<string, string> templateParams)
        {
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
                        @params = new
                        {
                            BODY_1 = templateParams.GetValueOrDefault("BODY_1", ""),
                            BODY_2 = templateParams.GetValueOrDefault("BODY_2", ""),
                            BODY_3 = templateParams.GetValueOrDefault("BODY_3", ""),
                            BODY_4 = templateParams.GetValueOrDefault("BODY_4", ""),
                            BODY_5 = templateParams.GetValueOrDefault("BODY_5", ""),
                            BODY_6 = templateParams.GetValueOrDefault("BODY_6", ""),
                            BODY_7 = templateParams.GetValueOrDefault("BODY_7", ""),
                            BODY_8 = templateParams.GetValueOrDefault("BODY_8", ""),
                            QUICK_REPLY_1 = "f146755s2591243"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(templateMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending WhatsApp template to {Phone}", formattedPhone);

                var response = await _httpClient.PostAsync($"{_baseUrl}/subscriber/send-whatsapp-template-by-user-id", content);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phoneNumber);

                return false;
            }
        }
    }
}