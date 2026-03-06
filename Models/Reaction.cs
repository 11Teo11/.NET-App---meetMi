using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Reaction
    {
        // PK compusa (Id, UserId, PostId)
        [Key]
        public int Id { get; set; }

        // FK catre user (cine a lasat reactia)
        public string UserId { get; set; }

        // FK catre post (la ce postare a fost lasata reactia)
        public int PostId { get; set; }


        // tipul de reactie
        public string ReactionType { get; set; } = "Like";



        // PROPRIETATEA DE NAVIGATIE

        // o reactie este lasata de un user
        public virtual ApplicationUser? User { get; set; }

        // o reactie este lasata la o postare
        public virtual Post? Post { get; set; }
    }
}
