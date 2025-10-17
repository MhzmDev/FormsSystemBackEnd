# Test Plan for Mandatory Fields Implementation

## Summary of Changes Made

? **1. Single Active Form Constraint**
- Only one form can be active at a time
- Creating a new form automatically deactivates all existing forms
- Added `GET /api/forms/active` endpoint to get the currently active form

? **2. Mandatory Fields Implementation**
- Every form now automatically includes 8 mandatory fields:
  1. `id` - ?????? (auto-generated)
  2. `referenceNo` - ??? ?????? (auto-generated if not provided)
  3. `customerName` - ??? ?????? (required user input)
  4. `phoneNumber` - ??? ?????? (required user input)
  5. `salary` - ?????? (required user input)
  6. `monthlySpent` - ??????? ?????? (required user input)
  7. `status` - ?????? (dropdown with predefined options)
  8. `creationDate` - ????? ??????? (auto-generated)

? **3. Enhanced Submission Response**
- `FormSubmissionSummaryDto` now includes all mandatory fields as direct properties
- Submissions list endpoint returns mandatory fields for quick access without needing to parse through all fields
- Maintains detailed view with all fields when accessing specific submission

? **4. Database Migration**
- Updated seed data with new mandatory fields structure
- Existing data preserved through proper migration

? **5. Auto-Generation Features**
- `id`: Uses submission ID
- `referenceNo`: Format "REF-YYYYMMDD-######" if not provided
- `creationDate`: Current date if not provided

## Test Cases to Verify

### Test 1: Get Active Form
```bash
GET http://localhost:5253/api/forms/active
```
**Expected:** Should return the default form with 8 mandatory fields

### Test 2: Create New Form (should deactivate previous)
```bash
POST http://localhost:5253/api/forms
{
  "name": "????? ??? ?????",
  "description": "????? ???????",
  "fields": [
    {
      "fieldName": "position",
      "fieldType": "text",
      "label": "??????",
      "isRequired": true,
      "displayOrder": 9
    }
  ]
}
```
**Expected:** Should create form with 8 mandatory fields + 1 custom field

### Test 3: Submit Form Data
```bash
POST http://localhost:5253/api/forms/{id}/submit
{
  "submittedBy": "???? ???",
  "values": {
    "customerName": "???? ??? ????",
    "phoneNumber": "+966501234567",
    "salary": "15000",
    "monthlySpent": "8000",
    "status": "????"
  }
}
```
**Expected:** Should auto-generate id, referenceNo, and creationDate

### Test 4: Get All Submissions
```bash
GET http://localhost:5253/api/submissions?page=1&pageSize=10
```
**Expected:** Should return submissions with mandatory fields as direct properties

### Test 5: Update Form (should not affect mandatory fields)
```bash
PUT http://localhost:5253/api/forms/{id}
{
  "name": "Updated Form Name",
  "description": "Updated description",
  "fields": [
    {
      "fieldName": "id",
      "fieldType": "number",
      "label": "Changed Label",
      "isRequired": false
    }
  ]
}
```
**Expected:** Should NOT modify the mandatory `id` field, only custom fields should be updated

## Key Implementation Points

1. **Mandatory Fields Protection**: Mandatory fields cannot be modified during form updates
2. **Single Active Form**: Creating a new form deactivates all existing forms
3. **Auto-Generation**: System fields are auto-populated when not provided
4. **Backward Compatibility**: Existing submissions maintain their structure
5. **Quick Access**: Mandatory fields are available as direct properties in submission summaries
6. **Field Ordering**: Mandatory fields always appear first (display order 1-8)

## API Endpoints Updated

- `GET /api/forms/active` - New endpoint for active form
- `POST /api/forms` - Enhanced to add mandatory fields and single active constraint
- `PUT /api/forms/{id}` - Enhanced to protect mandatory fields
- `POST /api/forms/{id}/submit` - Enhanced with auto-generation
- `GET /api/submissions` - Enhanced response with mandatory fields

All changes maintain backward compatibility while adding the required functionality.