namespace MothersonBoxManagement.Security;

public static class AppRoles
{
    public const string Operator = "Operator";
    public const string Supervisor = "Supervisor";
    public const string SupervisorFr = "Superviseur";
    public const string Administrator = "Administrator";
    public const string AdminFr = "Admin";

    public static class PolicyNames
    {
        public const string SupervisorOrAdmin = "SupervisorOrAdmin";
    }
}
