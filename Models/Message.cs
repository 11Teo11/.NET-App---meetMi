using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Message
    {
        // PK
        [Key]
        public int Id { get; set; }

        // continutul mesajului
        [Required(ErrorMessage = "Message content is required.")]
        public string Content { get; set; }

        // calea catre imagine/video
        public string? Media { get; set; }

        // data la care a fost trimis
        public DateTime Date { get; set; } = DateTime.Now;

        // FK catre User (cine a lasat mesajul)
        public string UserId { get; set; }

        // FK catre Group (in ce grup a fost lasat mesajul)
        public int GroupId { get; set; }


        // PROPRIETATEA DE NAVIGATIE

        // un mesaj este lasat de un user
        public virtual ApplicationUser? User { get; set; }

        // un mesaj este lasat intr-unn grup
        public virtual Group? Group { get; set; }
    }
}
