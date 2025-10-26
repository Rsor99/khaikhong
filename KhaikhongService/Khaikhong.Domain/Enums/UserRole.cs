using System.ComponentModel;

namespace Khaikhong.Domain.Enums
{
    public enum UserRole
    {
        [Description("Unknown")]
        UNKNOWN = 0,
        [Description("User")]
        USER = 1,
        [Description("Admin")]
        ADMIN = 2
    }
}