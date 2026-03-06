using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using System.ComponentModel.DataAnnotations;

namespace ProiectDotNet.Models
{
    public class Post
    {
        // PK
        [Key]
        public int Id { get; set; }

        // data crearii postarii
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        // continutul text
        public string? Text { get; set; }

        // calea catre imagine/video
        public string? Media { get; set; }

        // FK catre user (cine a facut postarea)
        public string? UserId { get; set; }


        // PROPRIETATEA DE NAVIGATIE

        // o postare este facuta de un user
        public virtual ApplicationUser? User { get; set; }

        // o postare are mai multe comentarii
        public virtual ICollection<Comment> Comments { get; set; } = [];

        // o postare are mai multe reactii
        public virtual ICollection<Reaction> Reactions { get; set; } = [];

        // regula de validare personalizata: textul sau media trebuie sa existe
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Text) && string.IsNullOrWhiteSpace(Media))
            {
                yield return new ValidationResult("A post must contain either text or a media file.", new[] { nameof(Text), nameof(Media) });
            }
        }

    }
}