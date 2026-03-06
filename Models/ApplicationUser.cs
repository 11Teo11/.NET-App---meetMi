using System.ComponentModel.DataAnnotations;
using ProiectDotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ProiectDotNet.Models
{
    public class ApplicationUser : IdentityUser
    {
        // nume
        [Required(ErrorMessage = "Numele este obligatoriu.")]
        public string LastName { get; set; }

        // prenume
        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        public string FirstName { get; set; }

        // descriere
        [Required(ErrorMessage = "Descrierea este obligatorie.")]
        public string Description { get; set; }

        // cale pfp
        [Required(ErrorMessage = "Profile picture is required.")]
        public string ProfilePicture { get; set; }

        // cale poza fundal
        public string? CoverPicture { get; set; }

        // bilisibiti
        public string Visibility { get; set; } = "public";

        // tipul de personalitate determinat de test
        public string? PersonalityType { get; set; }

        // data la care a fost efectuat ultimul test
        public DateTime? PersonalityTestedAt { get; set; }

        // PROPRIETATEA DE NAVIGATIE

        // un user posteaza mai multe postari
        public virtual ICollection<Post> Posts { get; set; } = [];

        // un user posteaza mai multe comentarii
        public virtual ICollection<Comment> Comments { get; set; } = [];

        // un user lasa mai multe reactii
        public virtual ICollection<Reaction> Reactions { get; set; } = [];

        // un user lasa mai multe mesaje
        public virtual ICollection<Message> Messages { get; set; } = [];

        // cererile de urmarire trimise de acest utilizator
        public virtual ICollection<Request> SentRequests { get; set; } = [];

        // cererile de urmarire primite de acest utilizator
        public virtual ICollection<Request> ReceivedRequests { get; set; } = [];

        // un user creaza mai multe grupuri
        public virtual ICollection<Group> CreatedGroups { get; set; } = [];

        // un user apartine de mai multe grupuri
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = [];

    }
}