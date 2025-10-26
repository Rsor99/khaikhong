using Khaikhong.Domain.Common;
using Khaikhong.Domain.Enums;

namespace Khaikhong.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public string FirstName { get; private set; } = default!;
        public string LastName { get; private set; } = default!;
        public UserRole Role { get; private set; } = UserRole.USER;

        public static User Create(string email, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("First name is required", nameof(firstName));
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException("Last name is required", nameof(lastName));
            }

            return new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.USER
            };
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash is required", nameof(passwordHash));
            }

            PasswordHash = passwordHash;
            Touch();
        }
    }
}
