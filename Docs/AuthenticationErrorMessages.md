# Authentication & Authorization Error Messages

## Overview
This document describes all authentication and authorization error responses in the Dynamic Forms API.

---

## 401 Unauthorized Errors

### 1. **Missing Token**
**Scenario**: User tries to access protected endpoint without providing a token

**Request**:
```http
GET /api/forms
```

**Response**:
```json
{
  "success": false,
  "message": "??? ???????? ?????. ???? ????? ??????",
  "messageEn": "Authentication token is required. Please login",
  "error": "Unauthorized"
}
```

**Status Code**: `401 Unauthorized`

---

### 2. **Expired Access Token**
**Scenario**: Access token has exceeded 60-minute expiry

**Request**:
```http
GET /api/forms
Authorization: Bearer {expired_token}
```

**Response**:
```json
{
  "success": false,
  "message": "انتهاء صلاحيه رمز الوصول. من فضلك قم بتحديث رمزك",
  "messageEn": "Access token has expired. Please refresh your token",
  "error": "Unauthorized"
}
```

**Status Code**: `401 Unauthorized`

**Solution**: Call `/api/user/refresh-token` endpoint with refresh token

---

### 3. **Invalid Token Signature**
**Scenario**: Token has been tampered with or signed with wrong secret

**Response**:
```json
{
  "success": false,
  "message": "رمز التحديث غير صحيح",
  "messageEn": "Invalid token signature",
  "error": "SecurityTokenInvalidSignatureException"
}
```

**Status Code**: `401 Unauthorized`

---

### 4. **Invalid Issuer**
**Scenario**: Token issued by unauthorized source

**Response**:
```json
{
  "success": false,
  "message": "???? ????? ??? ????",
  "messageEn": "Invalid token issuer",
  "error": "SecurityTokenInvalidIssuerException"
}
```

**Status Code**: `401 Unauthorized`

---

### 5. **Invalid Audience**
**Scenario**: Token not intended for this API

**Response**:
```json
{
  "success": false,
  "message": "????? ????? ??? ????",
  "messageEn": "Invalid token audience",
  "error": "SecurityTokenInvalidAudienceException"
}
```

**Status Code**: `401 Unauthorized`

---

### 6. **Generic Invalid Token**
**Scenario**: Token is malformed or otherwise invalid

**Response**:
```json
{
  "success": false,
  "message": "??? ???????? ??? ???? ?? ????? ????????",
  "messageEn": "Invalid or expired authentication token",
  "error": "Unauthorized"
}
```

**Status Code**: `401 Unauthorized`

---

## 403 Forbidden Errors

### 1. **Insufficient Role - Employee trying to access SuperAdmin endpoint**
**Scenario**: Employee user tries to access SuperAdmin-only endpoint (e.g., register new user)

**Request**:
```http
POST /api/user/register
Authorization: Bearer {employee_token}
```

**Response**:
```json
{
  "success": false,
  "message": "????? ??? ?????? ??? 'SuperAdmin'. ???? ??????: 'Employee'",
  "messageEn": "This resource requires 'SuperAdmin' role. Your current role: 'Employee'",
  "requiredRole": "SuperAdmin",
  "currentRole": "Employee",
  "error": "Forbidden"
}
```

**Status Code**: `403 Forbidden`

---

### 2. **Generic Permission Denied**
**Scenario**: User authenticated but lacks required permissions

**Response**:
```json
{
  "success": false,
  "message": "??? ???? ?????? ?????? ??? ??? ??????",
  "messageEn": "You do not have permission to access this resource",
  "error": "Forbidden"
}
```

**Status Code**: `403 Forbidden`

---

## Protected vs Public Endpoints

### Public Endpoints (No Authentication Required)
- ? `GET /api/forms/ActiveForm` - Get active form
- ? `POST /api/forms/{id}/submit` - Submit form data
- ? `POST /api/forms/{id}/submitTest` - Submit form data (test)
- ? `POST /api/user/login` - Login
- ? `POST /api/user/refresh-token` - Refresh token

### Authenticated Endpoints (Any Logged-in User)
- ?? `GET /api/forms` - Get all forms
- ?? `GET /api/forms/{id}` - Get specific form
- ?? `GET /api/submissions` - Get all submissions
- ?? `GET /api/submissions/{id}` - Get specific submission
- ?? `POST /api/user/logout` - Logout
- ?? `GET /api/user/{id}` - Get user details
- ?? `POST /api/user/change-password` - Change own password

### SuperAdmin-Only Endpoints
- ?? `POST /api/user/register` - Register new employee
- ?? `GET /api/user` - Get all users
- ?? `PATCH /api/user/{id}/status` - Update user status
- ?? `DELETE /api/user/{id}` - Soft delete user
- ?? `POST /api/user/{id}/reset-password` - Reset user password
- ?? `POST /api/forms` - Create new form
- ?? `PUT /api/forms/{id}` - Update form
- ?? `DELETE /api/forms/{id}` - Delete form
- ?? `POST /api/forms/{id}/activate` - Activate form

---

## How to Handle Errors in Frontend

### 1. **Handle 401 Errors**
```javascript
try {
  const response = await fetch('/api/forms', {
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });

  if (response.status === 401) {
    const error = await response.json();
    
    if (error.message.includes('????? ??????')) {
      // Token expired, refresh it
      await refreshToken();
    } else {
      // Invalid token, redirect to login
      redirectToLogin();
    }
  }
} catch (error) {
  console.error('Request failed:', error);
}
```

### 2. **Handle 403 Errors**
```javascript
if (response.status === 403) {
  const error = await response.json();
  
  // Show user-friendly message
  showErrorMessage(error.message);
  
  // Optionally redirect to dashboard
  redirectToDashboard();
}
```

### 3. **Automatic Token Refresh**
```javascript
async function refreshToken() {
  const response = await fetch('/api/user/refresh-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      accessToken: oldAccessToken,
      refreshToken: storedRefreshToken
    })
  });

  if (response.ok) {
    const { data } = await response.json();
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    
    // Retry original request
    return retryOriginalRequest();
  } else {
    // Refresh token also expired, redirect to login
    redirectToLogin();
  }
}
```

---

## Testing Error Responses

### Test 401 - Missing Token
```bash
curl -X GET http://localhost:5197/api/forms
```

### Test 401 - Expired Token
```bash
curl -X GET http://localhost:5197/api/forms \
  -H "Authorization: Bearer expired_token_here"
```

### Test 403 - Insufficient Role
```bash
# Login as Employee first
curl -X POST http://localhost:5197/api/user/login \
  -H "Content-Type: application/json" \
  -d '{"username":"employee1","password":"password"}'

# Try to access SuperAdmin endpoint
curl -X POST http://localhost:5197/api/user/register \
  -H "Authorization: Bearer {employee_token}" \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@test.com",...}'
```

---

## Summary

? **Meaningful error messages** in both Arabic and English  
? **Specific error types** (expired token, invalid signature, etc.)  
? **Role information** included in 403 responses  
? **Public endpoints** clearly marked with `[AllowAnonymous]`  
? **Consistent response format** across all errors  
? **Helpful hints** for resolution (e.g., "Please refresh your token")
