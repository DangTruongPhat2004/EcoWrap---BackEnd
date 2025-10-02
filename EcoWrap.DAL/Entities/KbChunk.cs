using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoWrap.DAL.Entities
{
    [Table("KbChunks")]
    public class KbChunk
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [MaxLength(300)] public string? Title { get; set; }
        [MaxLength(500)] public string? Source { get; set; }   // link, file path, standard name
        [MaxLength(200)] public string? Tags { get; set; }     // "material;kraft;FSC"
        public string Text { get; set; } = "";
        public float[]? Embedding { get; set; }                // 768-d (text-embedding-004)

        // Điểm tiện: lưu thêm checksum để tránh index trùng
        [MaxLength(64)] public string? Sha256 { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
