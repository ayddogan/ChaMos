using System.ComponentModel.DataAnnotations;

namespace ToDoListApi.Dtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola zorunludur.")]
        [MinLength(6, ErrorMessage = "Parola en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = string.Empty;
    }
}