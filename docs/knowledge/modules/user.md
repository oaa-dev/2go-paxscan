# User Module

Stack: Laravel 13 (`paxscan-online/backend/`). All paths below are relative to `paxscan-online/backend/`.

Admin-portal users and desktop-hub accounts. Auth is Sanctum personal access tokens with abilities; gates require both role and ability.

## Model
- **Path**: app/Models/User.php
- **Table**: users (+ `role` column)
- **Fillable** (`#[Fillable]`): name, email, password, role; **Hidden**: password, remember_token
- **Casts**: email_verified_at datetime, password hashed, role → UserRoleEnum
- **Traits**: HasApiTokens, HasFactory, Notifiable

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Controller | app/Http/Controllers/Api/V1/AuthController.php | login, logout, me |
| Service | app/Services/AuthService.php | login (admin only, ability `tasks.manage`, 8 h expiry), logout, issueHubToken (`hub.sync`, rotates by name) |
| Repository | app/Repositories/UserRepository.php | findByEmail, createToken, deleteTokensNamed, deleteCurrentToken (TransientToken-safe) |
| Repository (contract) | app/Repositories/Contracts/UserRepositoryInterface.php | |
| Enum | app/Enums/UserRoleEnum.php | Admin, Hub |
| Enum | app/Enums/TokenAbilityEnum.php | tasks.manage, hub.sync |
| FormRequest | app/Http/Requests/Auth/LoginRequest.php | email, password, device_name |
| Resource | app/Http/Resources/UserResource.php | id, name, email, role |
| Provider | app/Providers/AppServiceProvider.php | gates `tasks.manage` / `hub.sync`; rate limiter `login` (5/min per email+IP) |
| Command | app/Console/Commands/IssueHubTokenCommand.php | `php artisan paxscan:hub-token {email=hub@paxscan.test}` |
| Exceptions | bootstrap/app.php | API errors rendered in the envelope (401/403/404/422/429) |

## Routes
| Method | URI | Action | Middleware |
|--------|-----|--------|------------|
| POST | /api/v1/auth/login | AuthController@login | throttle:login |
| POST | /api/v1/auth/logout | AuthController@logout | auth:sanctum |
| GET | /api/v1/auth/me | AuthController@me | auth:sanctum |

## Database
| Type | File |
|------|------|
| Migration | database/migrations/0001_01_01_000000_create_users_table.php |
| Migration | database/migrations/2026_10_15_020000_add_role_to_users_table.php |
| Migration | database/migrations/2026_10_15_014727_create_personal_access_tokens_table.php |
| Factory | database/factories/UserFactory.php (state: hub) |
| Seeder | database/seeders/DatabaseSeeder.php (admin@paxscan.test / password; hub@paxscan.test) |

## Tests
| Type | File |
|------|------|
| Feature | tests/Feature/Auth/AuthTest.php |
