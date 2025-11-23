namespace LMSBackend.API.Configuration
{
    public class ThreeDLabOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ServiceUserEmail { get; set; } = string.Empty;
        public string ServiceUserPassword { get; set; } = string.Empty;
        public string ServiceUserName { get; set; } = "Integración 3DLAB";
        public string ServiceUserRut { get; set; } = "99999999-9";
        public string ServiceUserRole { get; set; } = "Alumno";
    }
}
