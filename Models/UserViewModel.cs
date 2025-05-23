using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeltaSocial.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public List<string> UserRoles { get; set; }
        public List<string> AllRoles { get; set; }
        public List<string> SelectedRoles { get; set; }
        public bool IsVisitor { get; set; }
    }

    public class CreateModeratorViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Parola trebuie să aibă între {2} și {1} caractere.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmă parola")]
        [Compare("Password", ErrorMessage = "Parolele nu coincid.")]
        public string ConfirmPassword { get; set; }
    }
} 