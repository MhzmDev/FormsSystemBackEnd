﻿using Microsoft.EntityFrameworkCore;
using DynamicForm.Data;
using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using System.Text.Json;

namespace DynamicForm.Services
{
    public class FormService : IFormService
    {
        private readonly ApplicationDbContext _context;

        public FormService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FormDto?> GetFormByIdAsync(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null) return null;

            return new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new FormFieldDto
                {
                    FieldId = f.FieldId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? 
                        JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            };
        }

        public async Task<IEnumerable<FormDto>> GetAllFormsAsync()
        {
            var forms = await _context.Forms
                .Where(f => f.IsActive)
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            return forms.Select(form => new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new FormFieldDto
                {
                    FieldId = f.FieldId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? 
                        JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            });
        }

        public async Task<FormDto> CreateFormAsync(CreateFormDto createFormDto)
        {
            var form = new Form
            {
                Name = createFormDto.Name,
                Description = createFormDto.Description,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "المستخدم"
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            foreach (var fieldDto in createFormDto.Fields)
            {
                var field = new FormField
                {
                    FormId = form.FormId,
                    FieldName = fieldDto.FieldName,
                    FieldType = fieldDto.FieldType,
                    Label = fieldDto.Label,
                    IsRequired = fieldDto.IsRequired,
                    DisplayOrder = fieldDto.DisplayOrder,
                    Options = fieldDto.Options != null ? JsonSerializer.Serialize(fieldDto.Options) : null
                };

                _context.FormFields.Add(field);
            }

            await _context.SaveChangesAsync();
            return await GetFormByIdAsync(form.FormId) ?? throw new InvalidOperationException("فشل في إنشاء النموذج");
        }

        public async Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var form = await _context.Forms
                    .Include(f => f.FormFields)
                    .FirstOrDefaultAsync(f => f.FormId == formId);

                if (form == null) return null;

                form.Name = updateFormDto.Name;
                form.Description = updateFormDto.Description;
                form.ModifiedDate = DateTime.UtcNow;

                var existingFields = form.FormFields.ToList();

                foreach (var existingField in existingFields)
                {
                    var updatedField = updateFormDto.Fields.FirstOrDefault(f => f.FieldName == existingField.FieldName);
                    
                    if (updatedField != null)
                    {
                        existingField.Label = updatedField.Label;
                        existingField.FieldType = updatedField.FieldType;
                        existingField.IsRequired = updatedField.IsRequired;
                        existingField.DisplayOrder = updatedField.DisplayOrder;
                        existingField.Options = updatedField.Options != null ? JsonSerializer.Serialize(updatedField.Options) : null;
                        existingField.IsActive = true;
                    }
                    else
                    {
                        existingField.IsActive = false;
                    }
                }

                var existingFieldNames = existingFields.Select(f => f.FieldName).ToHashSet();
                var newFields = updateFormDto.Fields.Where(f => !existingFieldNames.Contains(f.FieldName));

                foreach (var newFieldDto in newFields)
                {
                    var newField = new FormField
                    {
                        FormId = form.FormId,
                        FieldName = newFieldDto.FieldName,
                        FieldType = newFieldDto.FieldType,
                        Label = newFieldDto.Label,
                        IsRequired = newFieldDto.IsRequired,
                        DisplayOrder = newFieldDto.DisplayOrder,
                        Options = newFieldDto.Options != null ? JsonSerializer.Serialize(newFieldDto.Options) : null,
                        IsActive = true
                    };

                    _context.FormFields.Add(newField);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetFormByIdAsync(formId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteFormAsync(int formId)
        {
            var form = await _context.Forms.FindAsync(formId);
            if (form == null) return false;

            form.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
                throw new ArgumentException("النموذج غير موجود");

            var requiredFields = form.FormFields.Where(f => f.IsRequired && f.IsActive).ToList();
            var missingFields = requiredFields
                .Where(f => !submitFormDto.Values.ContainsKey(f.FieldName) || 
                           string.IsNullOrWhiteSpace(submitFormDto.Values[f.FieldName]))
                .Select(f => f.Label)
                .ToList();

            if (missingFields.Any())
                throw new ArgumentException($"الحقول التالية مطلوبة: {string.Join(", ", missingFields)}");

            var submission = new FormSubmission
            {
                FormId = formId,
                SubmittedBy = submitFormDto.SubmittedBy,
                SubmittedDate = DateTime.UtcNow,
                Status = "مُرسل"
            };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            foreach (var value in submitFormDto.Values)
            {
                var field = form.FormFields.FirstOrDefault(f => f.FieldName == value.Key);
                if (field != null)
                {
                    var submissionValue = new FormSubmissionValue
                    {
                        SubmissionId = submission.SubmissionId,
                        FieldId = field.FieldId,
                        FieldValue = value.Value,
                        FieldNameAtSubmission = field.FieldName,
                        FieldTypeAtSubmission = field.FieldType,
                        LabelAtSubmission = field.Label,
                        OptionsAtSubmission = field.Options,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.FormSubmissionValues.Add(submissionValue);
                }
            }

            await _context.SaveChangesAsync();

            var submissionService = new SubmissionService(_context);
            return await submissionService.GetSubmissionByIdAsync(submission.SubmissionId) ?? 
                   throw new InvalidOperationException("فشل في حفظ البيانات");
        }
    }
}