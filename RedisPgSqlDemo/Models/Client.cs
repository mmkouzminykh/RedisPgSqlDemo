using System.ComponentModel.DataAnnotations.Schema;

namespace RedisPgSqlDemo.Models
{
    public class Client
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Для проверки логики маршрутизации
        [NotMapped]
        public int? SectionId { get; private set; }

        public void SetSectionInfo(int section)
        {
            SectionId = section;
        }
    }
}
