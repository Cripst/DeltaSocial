using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeltaSocial.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> UserRoles { get; set; } = new List<string>();
        public List<string> AllRoles { get; set; } = new List<string>();
        public List<string> SelectedRoles { get; set; } = new List<string>();
        public bool IsVisitor { get; set; }
    }

    public class CreateModeratorViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Parola trebuie să aibă între {2} și {1} caractere.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmă parola")]
        [Compare("Password", ErrorMessage = "Parolele nu coincid.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}