# Add SystemModule Entity & Role/Permission Standardizations

We need to introduce a dedicated `SystemModule` table to categorize `Permissions` professionally, generate its CRUD views, ensure DataTables are applied across Role/Permission matrices, and update the Sidebar.

## User Review Required

> [!IMPORTANT]
> The existing `SystemModule` string column in the `Permission` table will be **dropped** and replaced with a Foreign Key (`SystemModuleId`) pointing to the new `SystemModule` table. All seed data will be updated accordingly. If there are any custom permissions you added manually outside of seed data, they will need to be re-assigned to a Module after migration.

## Proposed Changes

### 1. Database Architecture
- **[NEW] `Models/SystemModule.cs`**: Create a new entity with properties `ModuleId`, `ModuleName`, `Icon`, `SortOrder`, and `IsActive`.
- **[MODIFY] `Models/Permission.cs`**: Replace `string? SystemModule` with `int? SystemModuleId` and a Navigation property `public SystemModule? Module { get; set; }`.
- **[MODIFY] `Models/DigiPoseDbContext.cs`**: Add `DbSet<SystemModule> SystemModules`. Ensure mapping conventions in `OnModelCreating`.
- **[MODIFY] `Models/ModelBuilderExtensions.cs`**: Seed 5 base System Modules (System, POS, Warehouse, Catalog, Finance). Update the existing 20 Permissions' seed data to use `SystemModuleId` instead of strings.

### 2. MVC SystemModule Controller & Views
- **[NEW] `Areas/Administrator/Controllers/SystemModulesController.cs`**: Create standard CRUD endpoints.
- **[NEW] `Areas/Administrator/Views/SystemModules/*.cshtml`**: Create the Index, Create, Edit, Details, and Delete views. Apply the `.datatable` class to the `Index.cshtml` table so it inherits the automatic Search, Sort, and Pagination functions. 
- **[MODIFY] `Areas/Administrator/Views/Shared/_Layout.cshtml`**: Add a new Sidebar link to access the `SystemModules` index.

### 3. Role & Permission Standardization
- **[MODIFY] `Areas/Administrator/Views/Permissions/Index.cshtml`**: Add `<table class="table datatable table-bordered table-striped">` to ensure DataTables (Search, Sort, Pagination) automatically applies.
- **[MODIFY] `Areas/Administrator/Views/Roles/Index.cshtml`**: Ensure DataTables logic is fully applied here as well.

## Verification Plan

### Automated Actions
- `dotnet ef migrations add Add_SystemModule_Entity`
- `dotnet database update`
- `dotnet build`

### Manual Verification
- We will request the user to run `dotnet run`.
- Navigate to the new `/Administrator/SystemModules` to verify the CRUD and Datatables functionality.
- Navigate to `/Administrator/Permissions` and `/Administrator/Roles` to verify Search, Sort, and Pagination are operational via the `.datatable` integration.
