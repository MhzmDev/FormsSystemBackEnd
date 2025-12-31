using DynamicForm.BLL.Contracts;
using DynamicForm.DAL.Models.Entities;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IWhatsAppService _whatsAppService;

    public NotificationService(IWhatsAppService whatsAppService, ILogger<NotificationService> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task SendApprovalNotificationAsync(FormSubmission submission, List<FormSubmissionValue> values)
    {
        try
        {
            var phoneValue = ExtractFieldValue(values, "phone", "mobile", "هاتف", "جوال");

            if (phoneValue == null)
            {
                _logger.LogWarning("No phone number found for submission {SubmissionId}", submission.SubmissionId);

                return;
            }

            //var templateParams = BuildTemplateParameters(submission, values);
            var templateParams = new List<string>
            {
                submission.SubmissionId.ToString(), // Param 1
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "fullName")?.FieldValue ?? "غير محدد", // Param 2
                phoneValue, // Param 3
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "nationalId")?.FieldValue ?? "غير محدد", // Param 4
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "birthDate")?.FieldValue ?? "غير محدد", // Param 5
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "monthlySalary")?.FieldValue ?? "غير محدد", // Param 6
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "monthlyCommitments")?.FieldValue ?? "غير محدد", // Param 7
                values.FirstOrDefault(v => v.FieldNameAtSubmission == "ServiceDuration")?.FieldValue ?? "جديد" // Param 8
            };

            var fullName = ExtractFieldValue(values, "name", "fullname", "اسم") ?? "عميل محزم";

            await _whatsAppService.CreateSubscriberAsync(phoneValue, fullName);

            //await _whatsAppService.SendApprovalMessageAsync(phoneValue, templateParams);
            var messageSent = await _whatsAppService.SendTemplateMessageAsync(phoneValue, templateParams);

            if (messageSent)
            {
                _logger.LogInformation("WhatsApp approval notification sent successfully for submission {SubmissionId} to {Phone}",
                    submission.SubmissionId, phoneValue);
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp approval notification for submission {SubmissionId} to {Phone}",
                    submission.SubmissionId, phoneValue);
            }
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for submission {SubmissionId}", submission.SubmissionId);
        }
    }

    // ✅ NEW: Send rejection notification
    public async Task SendRejectionNotificationAsync(FormSubmission submission, List<FormSubmissionValue> values)
    {
        try
        {
            var phoneValue = ExtractFieldValue(values, "phone", "mobile", "هاتف", "جوال");

            if (phoneValue == null)
            {
                _logger.LogWarning("No phone number found for rejected submission {SubmissionId}", submission.SubmissionId);

                return;
            }

            var fullName = ExtractFieldValue(values, "name", "fullname", "اسم") ?? "عميل محزم";

            // Create subscriber if not exists
            await _whatsAppService.CreateSubscriberAsync(phoneValue, fullName);

            // Send rejection message with reason
            var rejectionReason = submission.RejectionReason ?? "تم رفض طلبك";
            var messageSent = await _whatsAppService.SendRejectionMessageAsync(phoneValue, rejectionReason);

            if (messageSent)
            {
                _logger.LogInformation("WhatsApp rejection notification sent successfully for submission {SubmissionId} to {Phone}",
                    submission.SubmissionId, phoneValue);
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp rejection notification for submission {SubmissionId} to {Phone}",
                    submission.SubmissionId, phoneValue);
            }
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection notification for submission {SubmissionId}", submission.SubmissionId);
        }
    }

    private string? ExtractFieldValue(List<FormSubmissionValue> values, params string[] searchTerms)
    {
        return values.FirstOrDefault(v => searchTerms.Any(term =>
            v.FieldNameAtSubmission.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.LabelAtSubmission.Contains(term, StringComparison.OrdinalIgnoreCase)))?.FieldValue;
    }

    private Dictionary<string, string> BuildTemplateParameters(FormSubmission submission,
        List<FormSubmissionValue> values)
    {
        return new Dictionary<string, string>
        {
            ["BODY_1"] = submission.SubmissionId.ToString(),
            ["BODY_2"] = ExtractFieldValue(values, "name", "fullname", "اسم") ?? "غير محدد",
            ["BODY_3"] = ExtractFieldValue(values, "phone", "mobile", "هاتف", "جوال") ?? "غير محدد",
            ["BODY_4"] = ExtractFieldValue(values, "nationalid", "هوية") ?? "غير محدد",
            ["BODY_5"] = ExtractFieldValue(values, "birth", "ميلاد") ?? "غير محدد",
            ["BODY_6"] = ExtractFieldValue(values, "salary", "راتب") ?? "غير محدد",
            ["BODY_7"] = ExtractFieldValue(values, "commitment", "التزام") ?? "غير محدد",
            ["BODY_8"] = ExtractFieldValue(values, "serviceduration", "مدة خدمة") ?? "جديد"
        };
    }
}