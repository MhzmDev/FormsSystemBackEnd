# Dynamic Forms API Documentation
# واجهة برمجة النماذج الديناميكية - دليل المطور

## Base URL	
https://localhost:5197/api

## Authentication
Currently no authentication required. (TODO: Add JWT authentication)



# 📋 FORMS MANAGEMENT

## 1. Get All Forms
**GET** `/api/forms`

### Purpose
Get list of all active forms

### Request
GET /api/forms

### Response
{ "success": true, "message": "تم جلب النماذج بنجاح", "data": [ { "formId": 1, "name": "نموذج البيانات الشخصية", "description": "نموذج لجمع البيانات الشخصية الأساسية", "isActive": true, "createdDate": "2024-01-01T00:00:00Z", "fields": [ { "fieldId": 1, "fieldName": "fullName", "fieldType": "text", "label": "الاسم الكامل", "isRequired": true, "displayOrder": 1, "options": null } ] } ] }

---

## 2. Get Specific Form
**GET** `/api/forms/{id}`

### Purpose
Get detailed information about a specific form including all its fields

### Request
GET /api/forms/1

### Response
{ "success": true, "message": "تم جلب النموذج بنجاح", "data": { "formId": 1, "name": "نموذج البيانات الشخصية", "description": "نموذج لجمع البيانات الشخصية الأساسية", "isActive": true, "createdDate": "2024-01-01T00:00:00Z", "fields": [ { "fieldId": 5, "fieldName": "nationalIdType", "fieldType": "dropdown", "label": "نوع الهوية", "isRequired": true, "displayOrder": 5, "options": ["بطاقة هوية وطنية", "جواز سفر", "رخصة قيادة", "بطاقة إقامة"] } ] } }

### Field Types Available:
- `text` - نص عادي
- `email` - بريد إلكتروني
- `date` - تاريخ
- `dropdown` - قائمة منسدلة (check `options` array)
- `checkbox` - خانة اختيار

---

## 3. Create New Form
**POST** `/api/forms`

### Purpose
Create a new form with custom fields

### Request
{ "name": "نموذج طلب وظيفة", "description": "نموذج لتقديم طلب وظيفة جديدة", "fields": [ { "fieldName": "applicantName", "fieldType": "text", "label": "اسم المتقدم", "isRequired": true, "displayOrder": 1 }, { "fieldName": "position", "fieldType": "dropdown", "label": "المنصب المطلوب", "isRequired": true, "options": ["مطور برمجيات", "مصمم واجهات", "مدير مشروع"], "displayOrder": 2 } ] }

### Response
{ "success": true, "message": "تم إنشاء النموذج بنجاح", "data": { "formId": 2, "name": "نموذج طلب وظيفة", "description": "نموذج لتقديم طلب وظيفة جديدة", "isActive": true, "createdDate": "2024-01-15T10:30:00Z", "fields": [...] } }

---

## 4. Update Existing Form
**PUT** `/api/forms/{id}`

### Purpose
Update form information and fields. **Note:** This will preserve existing submissions with their original field labels.

### Request
{ "name": "نموذج البيانات الشخصية المحدث", "description": "نموذج محدث لجمع البيانات الشخصية", "fields": [ { "fieldName": "fullName", "fieldType": "text", "label": "الاسم الثلاثي الكامل", // Updated label "isRequired": true, "displayOrder": 1 } ] }

### Important Notes:
- Fields with same `fieldName` will be updated
- Fields not included will be marked as inactive
- New fields will be added
- **Previous submissions keep their original labels**

---

## 5. Delete Form
**DELETE** `/api/forms/{id}`

### Purpose
Soft delete a form (marks as inactive, preserves data)

### Request

### Response
{ "success": true, "message": "تم حذف النموذج بنجاح" }

---

# 📤 FORM SUBMISSION

## 6. Submit Form Data
**POST** `/api/forms/{id}/submit`

### Purpose
Submit user data for a specific form

### Request
{ "submittedBy": "أحمد محمد علي", "values": { "fullName": "أحمد محمد علي السعيد", "age": "28", "birthDate": "1996-03-15", "nationalId": "1234567890", "nationalIdType": "بطاقة هوية وطنية", "phoneNumber": "+966501234567", "email": "ahmed.mohammed@gmail.com", "address": "شارع الملك فهد، حي الملز، الرياض", "governorate": "الرياض", "maritalStatus": "متزوج" } }

### Field Mapping:
The `values` object should contain:
- **Key:** `fieldName` from form definition
- **Value:** User's input as string

### Required Fields:
Check `isRequired: true` in form definition. Missing required fields will return error.

### Response
{ "success": true, "message": "تم إرسال البيانات بنجاح", "data": { "submissionId": 1, "formName": "نموذج البيانات الشخصية", "submittedDate": "2024-01-15T10:30:00Z", "status": "مُرسل", "submittedBy": "أحمد محمد علي", "values": [ { "fieldName": "fullName", "label": "الاسم الكامل", "value": "أحمد محمد علي السعيد", "fieldType": "text" } ] } }

---

# 📋 SUBMISSIONS MANAGEMENT

## 7. Get All Submissions (Admin)
**GET** `/api/submissions`

### Purpose
Get paginated list of all submissions across all forms with filtering

### Request Parameters:

GET /api/submissions?page=1&pageSize=10&fromDate=2024-01-01&toDate=2024-12-31&status=مُرسل

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | int | No | Page number (default: 1) |
| `pageSize` | int | No | Items per page (default: 10, max: 100) |
| `fromDate` | DateTime | No | Filter submissions from this date |
| `toDate` | DateTime | No | Filter submissions until this date |
| `status` | string | No | Filter by status (مُرسل، قيد المراجعة، مكتمل، محذوف) |

### Response
{ "success": true, "message": "تم جلب جميع المرسلات بنجاح", "data": { "items": [ { "submissionId": 1, "formId": 1, "formName": "نموذج البيانات الشخصية", "submittedDate": "2024-01-15T10:30:00Z", "status": "مُرسل", "submittedBy": "أحمد محمد علي", "preview": "أحمد محمد علي السعيد - ahmed.mohammed@gmail.com" } ], "totalCount": 25, "page": 1, "pageSize": 10, "totalPages": 3, "hasNextPage": true, "hasPreviousPage": false } }

---

## 8. Get Specific Submission Details
**GET** `/api/submissions/{id}`

### Purpose
Get complete details of a specific submission

### Request
GET /api/submissions/1

### Response
{ "success": true, "message": "تم جلب المرسلة بنجاح", "data": { "submissionId": 1, "formName": "نموذج البيانات الشخصية", "submittedDate": "2024-01-15T10:30:00Z", "status": "مُرسل", "submittedBy": "أحمد محمد علي", "values": [ { "fieldName": "fullName", "label": "الاسم الكامل", // Original label at submission time "value": "أحمد محمد علي السعيد", "fieldType": "text" } ] } }


---

## 9. Update Submission Status
**PATCH** `/api/submissions/{id}/status`

### Purpose
Update the status of a submission (for admin workflow)

### Request
{ "status": "قيد المراجعة" } // Valid statuses: مُرسل، قيد المراجعة، مكتمل، محذوف

### Available Statuses:
- `مُرسل` - Submitted
- `قيد المراجعة` - Under Review
- `مكتمل` - Completed
- `مرفوض` - Rejected
- `محذوف` - Deleted

### Response
{ "success": true, "message": "تم تحديث حالة المرسلة بنجاح" }

---

## 10. Delete Submission
**DELETE** `/api/submissions/{id}`

### Purpose
Soft delete a submission (changes status to محذوف)

### Request
DELETE /api/submissions/1

### Response
{ "success": true, "message": "تم حذف المرسلة بنجاح" }


---

# 🔄 COMMON PATTERNS

## Error Responses
All endpoints return errors in this format:

## HTTP Status Codes
- `200` - Success
- `201` - Created successfully  
- `400` - Bad request (validation errors)
- `404` - Not found
- `500` - Server error

## Date Format
All dates are in ISO 8601 format: `2024-01-15T10:30:00Z`

## Arabic Support
- All messages and labels support Arabic text
- Use UTF-8 encoding
- Right-to-left (RTL) text is supported

---

# 🚀 FRONTEND IMPLEMENTATION GUIDE

## 1. Display Form to User
// 1. Get form definition const response = await fetch('/api/forms/1'); const { data: form } = await response.json();
// 2. Render form fields dynamically form.fields.forEach(field => { if (field.fieldType === 'dropdown') { // Render select with field.options } else if (field.fieldType === 'text') { // Render input[type="text"] } // etc... });


## 2. Submit Form Data
// Collect form data const formData = { submittedBy: "اسم المستخدم", values: { "fullName": "أحمد محمد", "email": "ahmed@example.com" // ... other fields } };
// Submit const response = await fetch('/api/forms/1/submit', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(formData) });


## 3. Admin Dashboard - List Submissions
// Get submissions with pagination const response = await fetch('/api/submissions?page=1&pageSize=10&status=مُرسل'); const { data } = await response.json();
// Display paginated results console.log(Showing ${data.items.length} of ${data.totalCount} submissions); console.log(Page ${data.page} of ${data.totalPages});

---

# 📁 POSTMAN COLLECTION

You can import this API into Postman by:
1. Export OpenAPI/Swagger JSON from `/swagger/v1/swagger.json`
2. Import the JSON file into Postman
3. All endpoints will be automatically configured with examples