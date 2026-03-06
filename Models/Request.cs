using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Request
    {
        // PK compusa (Id, SenderId, ReceiverId)
        [Key]
        public int Id { get; set; }

        // FK catre User (cel ce a trimis cererea)
        public string SenderId { get; set; }

        // FK catre User (cel ce a primit cererea)
        public string ReceiverId { get; set; }

        // status-ul cererii de urmarire
        // contul receiver-ului este public -> "accepted" din prima 
        // contul receiver-ului este privat -> "pending" initial apoi
        //                                     "accepted" sau "refused"
        public string Status { get; set; }



        // PROPRIETATEA DE NAVIGATIE

        // un request este trimis de un user
        public virtual ApplicationUser? Sender { get; set; }

        // un request este trimis unui user
        public virtual ApplicationUser? Receiver { get; set; }

    }
}
