# Permission System Migration Guide

## Overview
This guide documents the migration from the old attribute-based permission system to the new policy-based permission strategy pattern.

## Key Changes

### 1. Old Attribute System → New Attribute System

| Old Attribute | New Attribute | Description |
|--------------|---------------|-------------|
| `[RequireAdmin]` | `[AdminPermission]` or `[HasPermission("admin:*")]` | Admin role requirement |
| `[RequireAuditReader]` | `[AuditReaderPermission]` | Audit reader permission |
| `[RequirePermission("permission")]` | `[HasPermission("permission")]` | Specific permission requirement |
| `[RequireProjectPermission("permission")]` | `[HasPermission("project:permission", allowOwner: true)]` | Project-specific permission |

### 2. Permission Strategy Pattern

The new system introduces a strategy pattern for permission checking:

```csharp
// Old way - manual permission checking
var currentUserRole = GetCurrentUserRole();
if (!string.Equals(currentUserRole, "admin", StringComparison.OrdinalIgnoreCase))
{
    // Handle non-admin case
}

// New way - using permission context
if (!await _permissionContext.HasPermissionAsync("audit:admin"))
{
    // Handle insufficient permissions
}
```

### 3. Controller Base Class

Controllers can now inherit from `BasePermissionController` for built-in permission utilities:

```csharp
public class MyController : BasePermissionController
{
    public MyController(IPermissionContext permissionContext, ILogger<MyController> logger) 
        : base(permissionContext, logger)
    {
    }

    public async Task<IActionResult> MyAction()
    {
        // Built-in permission checking
        await RequirePermissionAsync("my:permission");
        
        // Use utility properties
        var userId = CurrentUserId;
        var roles = CurrentUserRoles;
        
        return SuccessResponse(data, "Operation completed");
    }
}
```

## Migration Steps

### Step 1: Update Service Registration

In `Program.cs` or `Startup.cs`:

```csharp
// Add this line
services.AddFamilyTreePermissionSystem(configuration);
```

In the middleware pipeline:

```csharp
// Add this line after authentication but before authorization
app.UseFamilyTreePermissionSystem(env);
```

### Step 2: Update Controller Attributes

Replace old attributes with new ones:

```csharp
// Before
[RequireAdmin]
public async Task<IActionResult> AdminAction() { }

// After
[AdminPermission]
public async Task<IActionResult> AdminAction() { }
```

### Step 3: Update Permission Checking Logic

Replace manual permission checks:

```csharp
// Before
private string GetCurrentUserRole()
{
    return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
}

// After - inject IPermissionContext
private readonly IPermissionContext _permissionContext;

// Use permission context
var hasPermission = await _permissionContext.HasPermissionAsync("permission");
```

### Step 4: Migrate to BasePermissionController (Optional)

For enhanced functionality, inherit from `BasePermissionController`:

```csharp
// Before
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;
    
    public MyController(ILogger<MyController> logger)
    {
        _logger = logger;
    }
}

// After
public class MyController : BasePermissionController
{
    public MyController(IPermissionContext permissionContext, ILogger<MyController> logger) 
        : base(permissionContext, logger)
    {
    }
}
```

## Available Permission Attributes

### Basic Permission Attributes

```csharp
[HasPermission("permission")]                    // Requires specific permission
[HasPermission("permission", allowOwner: true)] // Allows resource owner access
[HasAnyPermission("perm1", "perm2")]            // Requires any of the permissions
[HasAllPermissions("perm1", "perm2")]           // Requires all permissions
[RequireResourceOwner("resourceType")]          // Requires resource ownership
[RequireRoleHierarchy("minimumRole")]          // Requires minimum role level
```

### Specialized Attributes

```csharp
[AuditPermission("read")]          // Audit system permissions
[AdminPermission]                  // Admin role requirement
[SuperAdminPermission]             // Super admin role requirement
[AuditReaderPermission]            // Audit reader permissions
```

## Permission Strings

### Standard Format
Permissions follow the format: `resource:action`

Examples:
- `user:create`, `user:read`, `user:update`, `user:delete`
- `project:create`, `project:read`, `project:update`, `project:delete`, `project:manage_members`
- `audit:read`, `audit:export`, `audit:admin`, `audit:create`, `audit:cleanup`
- `file:upload`, `file:download`, `file:delete`, `file:manage`

### Wildcard Permissions
- `user:*` - All user permissions
- `*` - All permissions (super admin)

## Advanced Features

### 1. Dynamic Permission Policies

Configure dynamic policies in `appsettings.json`:

```json
{
  "Authorization": {
    "DynamicPolicies": [
      {
        "Permission": "custom:permission",
        "AllowOwner": true,
        "ResourceType": "custom"
      }
    ]
  }
}
```

### 2. Permission Context Usage

```csharp
// Check permissions programmatically
var canRead = await _permissionContext.HasPermissionAsync("audit:read");
var canManage = await _permissionContext.HasAnyPermissionAsync("audit:admin", "role:super_admin");

// Set resource context
_permissionContext.SetResource("project", projectId, ownerId);

// Check resource ownership
var isOwner = await _permissionContext.IsResourceOwnerAsync("project", projectId);
```

### 3. Exception Handling

The new system provides built-in exception handling:

```csharp
protected async Task<IActionResult> MyAction()
{
    return await ExecuteWithValidationAsync(async () =>
    {
        await RequirePermissionAsync("my:permission");
        // Your business logic here
        return result;
    }, "MyAction");
}
```

## Testing the Migration

### 1. Unit Tests

Test permission checking logic:

```csharp
[Test]
public async Task Should_Allow_Admin_To_Access_Audit_Data()
{
    // Arrange
    var mockContext = new Mock<IPermissionContext>();
    mockContext.Setup(x => x.HasPermissionAsync("audit:read")).ReturnsAsync(true);
    
    // Act & Assert
    var result = await controller.GetAuditData();
    Assert.IsInstanceOf<OkObjectResult>(result);
}
```

### 2. Integration Tests

Test end-to-end authorization:

```csharp
[Test]
public async Task Should_Return_403_For_Unauthorized_User()
{
    // Arrange
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
    
    // Act
    var response = await client.GetAsync("/api/audit/admin-data");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Performance Considerations

### 1. Caching
The new system includes built-in permission caching with 5-minute expiration. No additional configuration required.

### 2. Database Optimization
Ensure proper indexing on permission-related tables:

```sql
CREATE INDEX idx_user_roles ON user_roles(user_id, role_id);
CREATE INDEX idx_role_permissions ON role_permissions(role_id, permission_id);
CREATE INDEX idx_user_permissions ON user_permissions(user_id, permission_id);
```

## Troubleshooting

### Common Issues

1. **Service Not Registered**
   ```
   Error: Unable to resolve service for type 'IPermissionContext'
   Solution: Ensure AddFamilyTreePermissionSystem() is called in Program.cs
   ```

2. **Policy Not Found**
   ```
   Error: The AuthorizationPolicy named 'Permission_audit_read' was not found
   Solution: Ensure authorization policies are built in startup
   ```

3. **Permission Always Denied**
   ```
   Solution: Check user role assignment and permission configuration
   ```

### Debug Tips

1. Enable authorization logging in development:
   ```csharp
   services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
   ```

2. Check permission cache:
   ```csharp
   // Clear cache if needed
   _cache.Remove($"user_permissions_{userId}");
   ```

## Rollback Plan

If issues arise, you can temporarily rollback by:

1. Comment out the new service registration
2. Revert controller attribute changes
3. Restore old permission checking methods

The old and new systems can coexist during migration.

## Benefits of the New System

1. **Centralized Logic**: All permission logic in one place
2. **Reduced Duplication**: No more repeated permission checking code
3. **Flexible Policies**: Easy to add new permission patterns
4. **Better Testing**: Mockable permission context
5. **Performance**: Built-in caching and optimization
6. **Maintainability**: Clear separation of concerns
7. **Scalability**: Supports complex permission hierarchies

## Next Steps

After successful migration:

1. Remove old permission checking methods
2. Clean up unused attributes
3. Add comprehensive permission tests
4. Document custom permissions
5. Consider implementing audit logging for permission changes