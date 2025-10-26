
namespace Khaikhong.Domain.Common
{
    public class AuditableEntity : BaseEntity
    {
        public Guid? CreatedBy { get; private set; }
        public Guid? UpdatedBy { get; private set; }

        public void SetCreatedBy(Guid userId)
        {
            CreatedBy = userId;
            UpdatedBy = userId;
        }

        public void SetUpdatedBy(Guid userId)
        {
            UpdatedBy = userId;
            Touch();
        }
    }
}