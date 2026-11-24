// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TraineeTracker.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Bitte E-Mail-Adresse eingeben.")]
    [EmailAddress(ErrorMessage = "Bitte eine gültige E-Mail-Adresse eingeben.")]
    [Display(Name = "E-Mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Bitte Passwort eingeben.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string Password { get; set; } = "";
}
