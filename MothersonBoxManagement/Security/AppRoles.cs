namespace MothersonBoxManagement.Security;

public static class AppRoles
{
    public const string Operator = "Operator";
    public const string Supervisor = "Supervisor";
    public const string SupervisorFr = "Superviseur";
    public const string Administrator = "Administrator";
    public const string AdminFr = "Admin";
    public const string AdministratorOnly = $"{AdminFr},{Administrator}";
    public const string SupervisorOrAdministrator = $"{SupervisorFr},{AdminFr},{Supervisor},{Administrator}";
}
