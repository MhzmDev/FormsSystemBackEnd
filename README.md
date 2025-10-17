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

## 2. Get Active Form
**GET** `/api/forms/active`

### Purpose
Get the currently active form (since only one form can be active at a time)

### Response
{
  "success": true,
  "message": "تم جلب النموذج النشط بنجاح",
  "data": {
    "formId": 1,
    "name": "نموذج البيانات الأساسية",
    "description": "نموذج جمع البيانات الأساسية مع الحقول الإلزامية",
    "isActive": true,
    "createdDate": "2024-01-01T00:00:00Z",
    "fields": [
      {
        "fieldId": 1,
        "fieldName": "id",
        "fieldType": "text",
        "label": "المعرف",
        "isRequired": true,
        "displayOrder": 1
      },
      {
        "fieldId": 2,
        "fieldName": "referenceNo",
        "fieldType": "text",
        "label": "رقم المرجع",
        "isRequired": true,
        "displayOrder": 2
      },
      // ... other mandatory and custom fields
    ]
  }
}

---

## 3. Create New Form
**POST** `/api/forms`

### Purpose
Create a new form with custom fields. Mandatory fields are automatically added and cannot be customized.

### Request
{
  "name": "نموذج طلب وظيفة",
  "description": "نموذج لتقديم طلب وظيفة جديدة",
  "fields": [
    {
      "fieldName": "position",
      "fieldType": "dropdown",
      "label": "المنصب المطلوب",
      "isRequired": true,
      "options": ["مطور برمجيات", "مصمم واجهات", "مدير مشروع"],
      "displayOrder": 9
    },
    {
      "fieldName": "experience",
      "fieldType": "text",
      "label": "سنوات الخبرة",
      "isRequired": false,
      "displayOrder": 10
    }
  ]
}

### Response
{
  "success": true,
  "message": "تم إنشاء النموذج بنجاح. تم إلغاء تفعيل النماذج السابقة.",
  "data": {
    "formId": 2,
    "name": "نموذج طلب وظيفة",
    "description": "نموذج لتقديم طلب وظيفة جديدة",
    "isActive": true,
    "createdDate": "2024-01-15T10:30:00Z",
    "fields": [
      // Mandatory fields (1-8) + custom fields (9+)
    ]
  }
}

---

## 4. Update Existing Form
**PUT** `/api/forms/{id}`

### Purpose
Update form information and custom fields. **Note:** Mandatory fields cannot be modified.

### Request
{
  "name": "نموذج البيانات المحدث",
  "description": "نموذج محدث لجمع البيانات",
  "fields": [
    {
      "fieldName": "position",
      "fieldType": "dropdown",
      "label": "المنصب المطلوب المحدث",
      "isRequired": true,
      "options": ["مطور برمجيات", "مصمم واجهات", "مدير مشروع", "محلل أنظمة"]
    }
  ]
}

### Important Notes:
- Mandatory fields (id, referenceNo, customerName, phoneNumber, salary, monthlySpent, status, creationDate) **cannot be modified**
- Custom fields with same `fieldName` will be updated
- Custom fields not included will be marked as inactive
- New custom fields will be added
- **Previous submissions keep their original labels**

---

## 5. Delete Form
**DELETE** `/api/forms/{id}`

### Purpose
Soft delete a form (marks as inactive, preserves data)

### Response
{
  "success": true,
  "message": "تم حذف النموذج بنجاح"
}

---

# 📤 FORM SUBMISSION

## 6. Submit Form Data
**POST** `/api/forms/{id}/submit`

### Purpose
Submit user data for a specific form. Mandatory fields are auto-generated if not provided.

### Request
{
  "submittedBy": "أحمد محمد علي",
  "values": {
    "customerName": "أحمد محمد علي السعيد",
    "phoneNumber": "+966501234567",
    "salary": "15000",
    "monthlySpent": "8000",
    "status": "جديد",
    // Custom fields
    "position": "مطور برمجيات",
    "experience": "5"
  }
}

### Auto-Generated Fields:
- `id`: Auto-generated from submission ID
- `referenceNo`: Auto-generated as "REF-YYYYMMDD-######" if not provided
- `creationDate`: Auto-generated as current date if not provided

### Response
{
  "success": true,
  "message": "تم إرسال البيانات بنجاح",
  "data": {
    "submissionId": 123,
    "formName": "نموذج البيانات الأساسية",
    "submittedDate": "2024-01-15T14:30:00Z",
    "status": "مُرسل",
    "submittedBy": "أحمد محمد علي",
    "values": [
      {
        "fieldName": "id",
        "label": "المعرف",
        "value": "123",
        "fieldType": "text"
      },
      {
        "fieldName": "referenceNo",
        "label": "رقم المرجع",
        "value": "REF-20240115-000123",
        "fieldType": "text"
      },
      // ... other fields
    ]
  }
}

---

# 📋 SUBMISSION MANAGEMENT

## 7. Get All Submissions (with Mandatory Fields)
**GET** `/api/submissions`

### Purpose
Get paginated list of all form submissions with mandatory fields for quick access

### Query Parameters:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10)
- `fromDate`: Filter submissions from date (optional)
- `toDate`: Filter submissions to date (optional)
- `status`: Filter by status (optional)

### Response
{
  "success": true,
  "message": "تم جلب البيانات بنجاح",
  "data": {
    "items": [
      {
        "submissionId": 123,
        "formId": 1,
        "formName": "نموذج البيانات الأساسية",
        "submittedDate": "2024-01-15T14:30:00Z",
        "status": "مُرسل",
        "submittedBy": "أحمد محمد علي",
        "preview": "أحمد محمد علي السعيد - المرجع: REF-20240115-000123",
        
        // Mandatory fields for quick access
        "id": "123",
        "referenceNo": "REF-20240115-000123",
        "customerName": "أحمد محمد علي السعيد",
        "phoneNumber": "+966501234567",
        "salary": "15000",
        "monthlySpent": "8000",
        "formStatus": "جديد",
        "creationDate": "2024-01-15"
      }
    ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}

## 8. Get Submission Details
**GET** `/api/submissions/{id}`

### Purpose
Get detailed submission data including all custom fields

### Response
{
  "success": true,
  "message": "تم جلب البيانات بنجاح",
  "data": {
    "submissionId": 123,
    "formName": "نموذج البيانات الأساسية",
    "submittedDate": "2024-01-15T14:30:00Z",
    "status": "مُرسل",
    "submittedBy": "أحمد محمد علي",
    "values": [
      // All fields (mandatory + custom) in display order
    ]
  }
}