using System.ComponentModel.DataAnnotations;
using LoanManagement.Entities.Validation;

namespace LoanManagement.Entities.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = null!;
}

/// Self-servis kayıt: kullanıcı + CRM müşteri kaydı birlikte oluşturulur; rol Customer ve JWT'de customer_id atanır.
public class RegisterRequestDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-64 karakter olmalıdır.")]
    [RegularExpression(@"^[a-zA-Z0-9._\-]{3,64}$",
        ErrorMessage = "Kullanıcı adı yalnızca harf, sayı, nokta, alt çizgi ve tire içerebilir.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,128}$",
        ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2-100 karakter arasında olmalıdır.")]
    [RegularExpression(@"^[\p{L}][\p{L} '\-]{1,99}$",
        ErrorMessage = "Ad yalnızca harf, boşluk, kesme işareti ve tire içerebilir.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2-100 karakter arasında olmalıdır.")]
    [RegularExpression(@"^[\p{L}][\p{L} '\-]{1,99}$",
        ErrorMessage = "Soyad yalnızca harf, boşluk, kesme işareti ve tire içerebilir.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "T.C. Kimlik Numarası zorunludur.")]
    [TurkishIdentityNumber]
    public string IdentityNumber { get; set; } = null!;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(255, ErrorMessage = "E-posta adresi 255 karakteri geçemez.")]
    public string Email { get; set; } = null!;

    [TurkishPhoneNumber]
    [StringLength(20, ErrorMessage = "Telefon numarası 20 karakteri geçemez.")]
    public string? PhoneNumber { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? CustomerId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
