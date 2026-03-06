using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Group
    {
        // PK
        [Key]
        public int Id { get; set; }

        // numele grupului
        [Required(ErrorMessage = "Group name is required")]
        public string Name { get; set; }

        // descrierea grupului
        [Required(ErrorMessage = "Group description is required")]
        public string Description { get; set; }

        // specifica daca grupul este public sau privat
        // true = public (cerere de join acceptata automat), false = privat (necesita aprobare)
        public bool IsPublic { get; set; } = true;

        // calea catre imaginea de coperta a grupului
        public string? CoverPhoto { get; set; }

        // FK catre User (moderator)
        public string? ModeratorId { get; set; }


        // PROPRIETATEA DE NAVIGATIE

        // un grup este creat de un user (numit moderator al grupului)
        public virtual ApplicationUser? Moderator { get; set; }

        // intr-un grup sunt mai multi useri (numiti membri)
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = [];

        // intr-un grup sunt lasate mai multe mesaje
        public virtual ICollection<Message> Messages { get; set; } = [];
    }
}
