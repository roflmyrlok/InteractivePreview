# API Documentation

This document describes the HTTP surface exposed by the InteractiveMap backend (`UserService`, `LocationService`, `ReviewService`) and how the **App-iOS.2** client maps onto it. It is written directly from the current source of truth — the controllers under `backend/*/API/Controllers` and the DTOs under `backend/*/Application/DTOs` — plus the Swift services under `App-iOS.2/InteractiveMap/InteractiveMap/`.

> **Transport:** The backend is **HTTP only** (no TLS). The iOS app's `Info.plist` carries an `NSAllowsArbitraryLoads` ATS exception so plain-HTTP traffic is allowed. Do **not** switch the client to `https://` — the server does not terminate TLS.

## Table of Contents

- [Deployment & Base URLs](#deployment--base-urls)
- [Authentication](#authentication)
- [User Service API](#user-service-api)
- [Location Service API](#location-service-api)
- [Review Service API](#review-service-api)
- [Health Endpoints](#health-endpoints)
- [Error Handling](#error-handling)
- [App-iOS.2 Integration Guide](#app-ios2-integration-guide)

## Deployment & Base URLs

The three services are separate ASP.NET Core projects. Each exposes its own port when run locally, and in the deployed environment they sit behind an nginx reverse proxy on a single host so all `/api/...` routes resolve from one origin.

| Service          | Dev port (`launchSettings.json` → `http` profile) | Route prefix    |
|------------------|---------------------------------------------------|-----------------|
| UserService      | `http://localhost:5280`                           | `/api/auth`, `/api/users` |
| LocationService  | `http://localhost:5261`                           | `/api/locations` |
| ReviewService    | `http://localhost:5260`                           | `/api/reviews` |

Deployed base URL (AWS EC2, used by the iOS app's compiled-in default):

```
http://ec2-63-177-81-123.eu-central-1.compute.amazonaws.com
```

Behind that host, nginx routes `/api/auth` and `/api/users` to UserService, `/api/locations` to LocationService, and `/api/reviews` to ReviewService. All examples below use paths relative to the shared origin.

## Authentication

Most write endpoints and user-specific read endpoints require a JWT. To authenticate:

1. `POST /api/auth/login` with `{ username, password }`.
2. Receive `{ "token": "<jwt>" }`.
3. Send subsequent requests with `Authorization: Bearer <jwt>`.

### JWT claims

Tokens are signed with HS256 and expire 1 hour after issue (UTC). Claims included:

- `sub` — User ID (GUID). Also mirrored to `ClaimTypes.NameIdentifier`.
- `email` — User email.
- `username` — Custom claim.
- `jti` — Unique token identifier.
- `exp` — Expiration (UTC epoch seconds).
- `role` — One of `Regular`, `Admin`, `SuperAdmin` (issued via `ClaimTypes.Role`).

Both the UserService (`BaseAuthenticatedController`) and ReviewService (`JwtHelper`) look up the user ID from multiple claim types as a defensive measure (`sub`, `ClaimTypes.NameIdentifier`, `nameid`, `user_id`, `userId`, etc.), so any reasonable encoding of the subject works.

## User Service API

Route prefix: `/api`

### Login

```
POST /api/auth/login
Content-Type: application/json
```

Request body (`LoginRequestDto`):

```json
{
  "username": "johndoe",
  "password": "P@ssw0rd123!"
}
```

Response `200 OK`:

```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
```

Errors: `401 Unauthorized` — bad credentials or user not found; `500` — unexpected error.

### List all users

```
GET /api/users
Authorization: Bearer <jwt>       (role: Admin or SuperAdmin)
```

Response `200 OK`: array of `UserDto` (see shape below).

### Get current user

Two equivalent routes are exposed — the iOS app uses `/me`:

```
GET /api/users/me
GET /api/users/current
Authorization: Bearer <jwt>
```

Response `200 OK`: `UserDto`.

### Get user by ID

```
GET /api/users/{id}
Authorization: Bearer <jwt>
```

A non-admin caller may only request their own ID (`403 Forbidden` otherwise).

### Get user by email / username

```
GET /api/users/by-email/{email}
GET /api/users/by-username/{username}
Authorization: Bearer <jwt>       (role: Admin or SuperAdmin)
```

### Register (create user)

```
POST /api/users
Content-Type: application/json
```

Anonymous. Request body (`CreateUserDto`):

```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "P@ssw0rd123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": 0
}
```

`role` is the integer value of the `UserRole` enum: `0 = Regular`, `1 = Admin`, `2 = SuperAdmin`. If omitted the server defaults to `Regular`.

Response `201 Created` with the created `UserDto` and a `Location` header pointing at `/api/users/{id}`.

### Update user

```
PUT /api/users
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body (`UpdateUserDto`):

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "John",
  "lastName": "Smith",
  "email": "john.smith@example.com",
  "role": 0
}
```

Non-admins can only update their own `id`. Response `200 OK`: updated `UserDto`.

### Change password

Both verbs are accepted so iOS clients can POST:

```
POST /api/users/change-password
PUT  /api/users/change-password
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body (`ChangePasswordDto`):

```json
{
  "currentPassword": "oldP@ss1",
  "newPassword":     "newP@ssword1",
  "confirmNewPassword": "newP@ssword1"
}
```

`newPassword` must be ≥ 8 chars; `confirmNewPassword` must equal `newPassword`. Response `200 OK`: `{ "message": "Password changed successfully" }`.

### Delete own account

Both verbs are accepted:

```
DELETE /api/users/delete-account
POST   /api/users/delete-account
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body (`DeleteAccountDto`):

```json
{ "currentPassword": "P@ssw0rd123!" }
```

Response `204 No Content`.

### Delete another user (admin)

```
DELETE /api/users/{id}
Authorization: Bearer <jwt>       (role: Admin or SuperAdmin)
```

Users cannot delete themselves via this endpoint — use `/delete-account` instead. Response `204 No Content`.

### UserDto shape

```json
{
  "id":            "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username":      "johndoe",
  "email":         "john@example.com",
  "firstName":     "John",
  "lastName":      "Doe",
  "role":          0,
  "createdAt":     "2025-03-15T14:30:00Z",
  "lastLoginDate": "2025-03-19T09:45:00Z"
}
```

`role` is serialized as its integer value; `lastLoginDate` is nullable.

## Location Service API

Route prefix: `/api/locations`. The controller uses CQRS (MediatR) — all queries/commands are dispatched through an `IMediator`.

### List all locations

```
GET /api/locations
```

Anonymous. Response `200 OK`: array of `LocationDto`.

### Get location by ID

```
GET /api/locations/{id}
```

Anonymous. Response `200 OK`: `LocationDto`. Returns `404` if the location does not exist.

### Validate location exists

```
GET /api/locations/validate/{id}
```

Anonymous. Always returns `200 OK` with a boolean:

```json
{ "exists": true }
```

### Find nearby locations

```
GET /api/locations/nearby?latitude={lat}&longitude={lon}&radiusKm={km}
```

Anonymous. `radiusKm` defaults to `10` on the server if omitted. Response `200 OK`: array of `LocationDto`.

### Create location

```
POST /api/locations
Content-Type: application/json
```

> The current controller does **not** apply `[Authorize]` to `Create`, so this endpoint is anonymous on the server. Treat that as an implementation detail that may change; clients should still be prepared to send a bearer token.

Body (`CreateLocationCommand`):

```json
{
  "latitude": 40.748817,
  "longitude": -73.985428,
  "address": "350 Fifth Avenue, New York, NY 10118",
  "details": [
    { "propertyName": "type",     "propertyValue": "building" },
    { "propertyName": "yearBuilt","propertyValue": "1931" }
  ]
}
```

Response `201 Created` with a `Location` header pointing at `/api/locations/{id}` and a body of `{ "id": "<new-guid>" }`.

> **No update/delete endpoints exist** for locations or location details in the current controller. Earlier versions of this doc listed `PUT /api/locations`, `POST /api/locations/{id}/details`, `PUT /api/locations/{id}/details/{detailId}`, `DELETE /api/locations/{id}/details/{detailId}`, `DELETE /api/locations/{id}`, and `GET /api/locations/by-property` — those are **not implemented** in the current `LocationsController`.

### LocationDto shape

```json
{
  "id":        "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "latitude":  40.785091,
  "longitude": -73.968285,
  "address":   "Central Park, New York, NY",
  "createdAt": "2025-03-10T08:15:30Z",
  "updatedAt": null,
  "details": [
    {
      "id":            "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "propertyName":  "type",
      "propertyValue": "park"
    }
  ]
}
```

> The DTO has only `id`, `latitude`, `longitude`, `address`, `createdAt`, `updatedAt`, and `details`. There are **no** `name`, `city`, `state`, `country`, or `postalCode` fields — any such data should be encoded into `address` or as entries in `details`.

## Review Service API

Route prefix: `/api/reviews`.

### List all reviews

```
GET /api/reviews
```

Anonymous. Response `200 OK`: array of `ReviewDto`.

### Get review by ID

```
GET /api/reviews/{id}
```

Anonymous. Response `200 OK`: `ReviewDto`.

### Get reviews by user

```
GET /api/reviews/by-user/{userId}
```

Anonymous. Response `200 OK`: array of `ReviewDto`.

### Get reviews by location

```
GET /api/reviews/by-location/{locationId}
```

Anonymous. Response `200 OK`: array of `ReviewDto`.

### Get average rating for a location

```
GET /api/reviews/average-rating/{locationId}
```

Anonymous. Response `200 OK`:

```json
{
  "locationId":    "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "averageRating": 4.7
}
```

### Stream a review image

```
GET /api/reviews/images/{reviewId}/{fileName}
```

Anonymous. Returns the binary image with the stored `Content-Type`. `404` if the object is not in S3.

### Create review — multipart (with images)

```
POST /api/reviews
Authorization: Bearer <jwt>
Content-Type: multipart/form-data
```

Form fields (`CreateReviewWithImagesDto`):

- `locationId` — GUID string
- `rating` — integer (1–5)
- `content` — string
- `images` — zero or more file parts; field name `images`

The server creates the review first, then (if any `images` were included) uploads them to S3 and patches the review with the resulting URLs. If image upload fails, the review is still created.

Response `201 Created` with `ReviewDto` and a `Location` header pointing at `/api/reviews/{id}`.

### Create review — JSON (no images)

```
POST /api/reviews/json
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body (`CreateReviewDto`):

```json
{
  "locationId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "rating": 4,
  "content": "Great place!",
  "imageUrls": []
}
```

`imageUrls` defaults to an empty list on the server and may be omitted. Response `201 Created`: `ReviewDto`.

### Upload more images to an existing review

```
POST /api/reviews/upload-images/{reviewId}
Authorization: Bearer <jwt>
Content-Type: multipart/form-data
```

Form field: `images` (one or more files). The caller must own the review (`403 Forbidden` otherwise). New image URLs are appended to the existing `imageUrls`.

Response `200 OK`:

```json
{
  "message":   "Images uploaded successfully",
  "imageUrls": ["/api/reviews/images/<reviewId>/<file>.jpg"],
  "review":    { /* updated ReviewDto */ }
}
```

### Update review

```
PUT /api/reviews
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body (`UpdateReviewDto`):

```json
{
  "id":        "7fa85f64-5717-4562-b3fc-2c963f66afad",
  "rating":    5,
  "content":   "Updated review content",
  "imageUrls": ["/api/reviews/images/.../a.jpg"]
}
```

Caller must own the review. Response `200 OK`: updated `ReviewDto`.

### Delete review

```
DELETE /api/reviews/{id}
Authorization: Bearer <jwt>
```

Caller must own the review. Associated S3 images are deleted best-effort. Response `204 No Content`.

### Delete a single review image

```
DELETE /api/reviews/image
Authorization: Bearer <jwt>
Content-Type: application/json
```

Body:

```json
{
  "reviewId": "7fa85f64-5717-4562-b3fc-2c963f66afad",
  "imageUrl": "/api/reviews/images/<reviewId>/<file>.jpg"
}
```

Caller must own the review. Response `200 OK`:

```json
{ "message": "Image deleted successfully", "deleted": true }
```

### ReviewDto shape

```json
{
  "id":         "7fa85f64-5717-4562-b3fc-2c963f66afad",
  "userId":     "1fa85f64-5717-4562-b3fc-2c963f66afa1",
  "locationId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "rating":     5,
  "content":    "Great place to visit!",
  "createdAt":  "2025-03-20T15:20:30.123456Z",
  "updatedAt":  null,
  "imageUrls":  ["/api/reviews/images/<reviewId>/<file>.jpg"]
}
```

Image URLs are relative paths rooted at the shared origin; the iOS `NetworkManager.downloadImage` prepends `APIConstants.baseURL` when they start with `/api/reviews/images/`.

## Health Endpoints

Each service exposes a health probe outside the `/api` namespace:

```
GET /health
```

Response `200 OK`:

```json
{ "status": "healthy", "timestamp": "2026-04-18T12:00:00Z" }
```

The route prefix is `/health` (not `/api/health`) because the controller is annotated `[Route("[controller]")]`.

## Error Handling

Unsuccessful responses carry a JSON body with at least a `message` field:

```json
{ "message": "You can only access your own user information" }
```

Validation failures from `[ApiController]` model-binding (e.g. a malformed `ChangePasswordDto`) follow the ASP.NET Core ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "NewPassword": ["The field NewPassword must be a string or array type with a minimum length of '8'."]
  }
}
```

### HTTP status codes in use

- `200 OK` — success with a body
- `201 Created` — resource created (with `Location` header)
- `204 No Content` — success with no body
- `400 Bad Request` — validation or domain-rule failure
- `401 Unauthorized` — missing / invalid / expired token
- `403 Forbidden` — authenticated but not permitted (role or ownership check)
- `404 Not Found` — resource does not exist
- `500 Internal Server Error` — unhandled exception

## App-iOS.2 Integration Guide

This section documents the iOS client at `App-iOS.2/InteractiveMap/InteractiveMap/`.

### Base URL resolution (`Utilities/APIConstants.swift`)

`APIConstants.baseURL` is resolved at runtime:

1. If a non-empty override has been stored under `UserDefaults` key `custom_api_base_url`, use it.
2. Otherwise use the compiled-in default `http://ec2-63-177-81-123.eu-central-1.compute.amazonaws.com`.

Any trailing slash is stripped. Service URLs are computed from `baseURL`:

```swift
userServiceURL     → baseURL + "/api/users"
authServiceURL     → baseURL + "/api/auth"
locationServiceURL → baseURL + "/api/locations"
reviewServiceURL   → baseURL + "/api/reviews"
```

The override can be set from the Developer Settings screen via `APIConstants.setCustomBaseURL(_:)`. A valid URL must parse with a scheme and host; otherwise the call returns `false` and makes no change. `APIConstants.resetToDefault()` clears the override. On mutation, `APIConstants.baseURLDidChangeNotification` is posted so dependent views can refresh.

The type carries a doc comment reminding editors that the backend is HTTP-only and that `NSAllowsArbitraryLoads` in `Info.plist` permits the plain-HTTP traffic.

### ATS configuration (`Info.plist`)

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```

Also declared: `NSPhotoLibraryUsageDescription`, `NSCameraUsageDescription` for the review-image flow.

### Token storage (`Utilities/TokenManager.swift`)

The JWT is stored in the iOS Keychain under `auth_token` via `KeychainSwift`. On read, `TokenManager` decodes the payload and clears the token if `exp` is in the past. `isAuthenticated` is simply `getToken() != nil`.

### Network layer (`Utilities/NetworkManager.swift`)

A single Alamofire-based `NetworkManager.shared.request(_:method:parameters:headers:authenticated:completion:)` handles every call. Notable behaviors:

- GET requests use `URLEncoding.default`; everything else uses `JSONEncoding.default`.
- When `authenticated: true`, a `Bearer <token>` header is attached; if no token is available, the request fails immediately with a 401 error and the app posts `NSNotification.Name("AuthenticationFailed")`.
- A custom `dateDecodingStrategy` tries multiple ISO-8601 variants (with/without fractional seconds, with/without trailing `Z`) plus `ISO8601DateFormatter` fallbacks, so the various date encodings coming out of the backend services all parse.
- `204 No Content` responses decode to the `EmptyResponse` helper struct when `T == EmptyResponse`; otherwise they surface as an error.
- `downloadImage(from:)` accepts either `/api/reviews/images/...` relative paths, fully-qualified `http(s)://` URLs, or bare relative paths, normalizing all three against `APIConstants.baseURL`.

### Service → endpoint map

| Swift call                                                                  | HTTP                                              |
|-----------------------------------------------------------------------------|---------------------------------------------------|
| `AuthService.login`                                                         | `POST /api/auth/login` (JSON)                     |
| `AuthService.register`                                                      | `POST /api/users` (JSON)                          |
| `AuthService.logout`                                                        | (local only — clears keychain)                    |
| `UserService.getCurrentUser`                                                | `GET /api/users/me` (auth)                        |
| `UserService.changePassword`                                                | `POST /api/users/change-password` (auth, JSON)    |
| `UserService.deleteAccount`                                                 | `DELETE /api/users/delete-account` (auth, JSON)   |
| `LocationService.getLocations`                                              | `GET /api/locations`                              |
| `LocationService.getLocation(id:)`                                          | `GET /api/locations/{id}`                         |
| `LocationService.getNearbyLocations(latitude:longitude:radiusKm:)`          | `GET /api/locations/nearby?latitude&longitude&radiusKm` |
| `ReviewService.getReviewsForLocation(locationId:)`                          | `GET /api/reviews/by-location/{locationId}`       |
| `ReviewService.createReview(locationId:rating:content:)`                    | `POST /api/reviews/json` (auth, JSON)             |
| `ReviewService.createReviewWithImages(request:)`                            | `POST /api/reviews` (auth, multipart/form-data)   |

The `/nearby` call encodes its numeric parameters via Alamofire's parameter dictionary (not hand-rolled query strings) to avoid locale-specific decimal separators. The `/json` review-creation call sets `Content-Type: application/json` explicitly so the body survives nginx forwarding unchanged.

### Model ↔ DTO mapping

| Swift type                   | Backend DTO                                       | Notes |
|------------------------------|---------------------------------------------------|-------|
| `User` (`Models/User.swift`) | `UserDto`                                         | `role` decoded as `Int` (0/1/2); `lastLoginDate` optional string |
| `Location` (`Models/Location.swift`) | `LocationDto`                             | Matches exactly; `updatedAt` optional, `details` non-optional array |
| `LocationDetail`             | `LocationDetailDto`                               | Matches exactly |
| `Review` (`Models/Review.swift`) | `ReviewDto`                                   | Swift parses `createdAt`/`updatedAt` as `Date`; `imageUrls` non-optional array |
| `LoginRequest` / `LoginResponse` | `LoginRequestDto` / `{ token }`               | |
| `CreateReviewRequest` (`Codable`) | `CreateReviewDto`                            | Used by `/api/reviews/json`; `imageUrls` omitted (server defaults to []) |
| `CreateReviewWithImagesRequest` (not `Codable`) | `CreateReviewWithImagesDto`    | Built into a multipart body in `ReviewService.swift` |
| `ChangePasswordRequest`      | `ChangePasswordDto`                               | Same three fields |
| `DeleteAccountRequest`       | `DeleteAccountDto`                                | `currentPassword` only |
| `RegisterResponse`           | `UserDto` (subset)                                | Returned by `POST /api/users` |
| `EmptyResponse`              | —                                                 | Sentinel for `204 No Content` |

### Caching (`CacheManager`)

The `LocationService` and `ReviewService` Swift classes do cache-first reads: they return the cached value immediately (if present) and kick off a background network refresh that updates the cache. Nearby-location network failures fall back to cached locations within the requested radius using a local great-circle distance calculation.
