namespace DesktopController;

/// <summary>
/// Stores user details collected after login
/// </summary>
public class UserSessionDetails
{
    public string SystemUserName { get; set; } = "";
    public string Department { get; set; } = "";
    public string OfficeLocation { get; set; } = "";

    private static UserSessionDetails? _instance;
    public static UserSessionDetails Instance => _instance ??= new UserSessionDetails();

    public static void Set(string systemUserName, string department, string officeLocation)
    {
        Instance.SystemUserName = systemUserName;
        Instance.Department = department;
        Instance.OfficeLocation = officeLocation;
    }
}
