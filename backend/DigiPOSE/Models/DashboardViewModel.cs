namespace DigiPOSE.Models
{
    public class ModuleTelemetryInfo
    {
        public string ModuleId { get; set; } = null!;
        public string ModuleName { get; set; } = null!;
        public int TableCount { get; set; }
        public int TotalRecordCount { get; set; }
        public List<TableTelemetryInfo> Tables { get; set; } = new();
    }

    public class TableTelemetryInfo
    {
        public string TableName { get; set; } = null!;
        public string ControllerName { get; set; } = null!;
        public int RecordCount { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }

    public class DashboardViewModel
    {
        public int TotalTables { get; set; }
        public int TotalSystemRecords { get; set; }
        public List<ModuleTelemetryInfo> Modules { get; set; } = new();
    }
}
