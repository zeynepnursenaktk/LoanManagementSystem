using System.ComponentModel.DataAnnotations;
using LoanManagement.Entities.Validation;

namespace LoanManagement.Entities.DTOs;


public class CreateCustomerDto
{
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


/// Müşteri bilgilerini güncellemek için DTO
/// Bankacılık uyumlu validasyonlar içerir
public class UpdateCustomerDto
{

    /// Müşterinin e-posta adresi. Geçerli format, zorunlu, benzersiz olmalı.
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(255, ErrorMessage = "E-posta adresi 255 karakteri geçemez.")]
    public string Email { get; set; } = null!;


    /// Müşterinin telefon numarası. İsteğe bağlı, Türkiye GSM formatında.
    [TurkishPhoneNumber]
    [StringLength(20, ErrorMessage = "Telefon numarası 20 karakteri geçemez.")]
    public string? PhoneNumber { get; set; }
}
