using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Comment
    {
        // PK
        [Key]
        public int Id { get; set; }

        // continutul comentariului
        public string Content { get; set; }

        // data postarii comentariului
        public DateTime Date { get; set; } = DateTime.Now;

        // FK catre post (la ce postare a fost lasat comentariul
        public int PostId { get; set; }

        // FK catre user (cine a lasat comentariul)
        public string? UserId { get; set; }


        // PROPRIETATEA DE NAVIGATIE

        // un comentariu este lasat de un user
        public virtual ApplicationUser? User {  get; set; }

        // un comentariu apartine unei postari
        public virtual Post? Post { get; set; }


    }
}
