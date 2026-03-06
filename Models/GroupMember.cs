using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class GroupMember
    {
        // PK compusa (Id, UserId, GroupId)
        [Key]
        public int Id { get; set; }

        // FK catre User (ce user face parte din grup)
        public string? UserId { get; set; }

        // FK catre Group (din ce grup face parte userul)
        public int? GroupId { get; set; }

        // status join la grup
        // true = membru acceptat, false = cerere in asteptare (pending)
        public bool IsAccepted { get; set; } = false;


        // PROPRIETATEA DE NAVIGATIE

        // un user face parte din mai multe grupuri
        public virtual ApplicationUser? User { get; set; }

        // intr-un grup sunt mai multi useri
        public virtual Group? Group { get; set; }
    }
}
