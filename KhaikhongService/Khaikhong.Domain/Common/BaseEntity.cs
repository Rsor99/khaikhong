namespace Khaikhong.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;

        public void Touch() => UpdatedAt = DateTime.UtcNow;

        public void Deactivate()
        {
            IsActive = false;
            Touch();
        }

        public void Activate()
        {
            IsActive = true;
            Touch();
        }
    }
}