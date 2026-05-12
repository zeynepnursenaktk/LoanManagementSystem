# API Endpoint Listesi — Loan Management System

> **Base URL (Development):** `http://localhost:5213`
> **Swagger UI:** `http://localhost:5213/swagger`
> **Auth:** `Authorization: Bearer <jwt>` (Login dışındaki tüm endpoint'ler)
> **JWT Claim'leri:** `sub`, `name`, `role` (`Admin` | `Customer`), `customer_id` (yalnızca Customer)

Kontrolcüye göre tüm endpoint'lerin özet listesidir. Detaylı request/response şemaları için [API_INTEGRATION_DOCUMENTATION.md](../API_INTEGRATION_DOCUMENTATION.md) dosyasına bakın.

---

## 1. Auth — `/api/Auth` (`[AllowAnonymous]`)

| Method | Path | Body | 200 / 201 | Diğer | Açıklama |
|---|---|---|---|---|---|
| `POST` | `/api/Auth/register` | `RegisterRequestDto` | `200 OK` → `{ user, customerId, message }` | `400` validasyon, `409` çakışan kullanıcı/kimlik | Self-servis kayıt. `Users` + `Customers` aynı anda oluşturulur; rol = `Customer`. |
| `POST` | `/api/Auth/login`    | `LoginRequestDto`    | `200 OK` → `LoginResponseDto { token, username, fullName, role, customerId, expiresAt }` | `401` hatalı kimlik | JWT döner. |

---

## 2. Customers — `/api/Customers` (`[Authorize]`)

> **Yetki kuralı:** Admin tüm müşterileri yönetir. Müşteri yalnızca `claim.customer_id == route.id` olan kaydı görebilir/güncelleyebilir (aksi halde `403`).

| Method | Path | Yetki | Body / Query | Yanıt | Açıklama |
|---|---|---|---|---|---|
| `GET`    | `/api/Customers` | **Admin** | `?includeDeleted=bool` | `200 OK` → `CustomerListDto[]` | Tüm müşterileri listeler; `includeDeleted=true` → arşivlenenler de gelir. |
| `GET`    | `/api/Customers/{id:int}` | Admin / Owner | — | `200` veya `404` | Müşteri detayı. |
| `POST`   | `/api/Customers` | **Admin** | `CreateCustomerDto` | `201 Created` → `{ id, message }` | Admin müşteri ekler (self-register yerine). |
| `PUT`    | `/api/Customers/{id:int}` | Admin / Owner | `UpdateCustomerDto` | `200 OK` veya `404` | E-posta ve telefon günceller. |
| `DELETE` | `/api/Customers/{id:int}` | **Admin** | — | `200 OK` veya `404` / `400` | Soft-delete. Aktif kredisi varsa `400`. |
| `POST`   | `/api/Customers/{id:int}/restore` | **Admin** | — | `200 OK` veya `404` | Soft-delete edilmiş müşteriyi geri açar. |
| `GET`    | `/api/Customers/{id:int}/summary` | Admin / Owner | — | `200` → `CustomerSummaryDto` | Toplam borç, kalan taksit sayısı, kapanmış kredi adedi vb. özet. |

---

## 3. Loans — `/api/Loans` (`[Authorize]`)

> **Yetki kuralı:** Admin tüm kredilere erişir. Müşteri yalnızca kendi `customer_id`'sine ait kredileri görür; başvurularda `CustomerId` JWT'den otomatik atanır.

| Method | Path | Yetki | Body | Yanıt | Açıklama |
|---|---|---|---|---|---|
| `POST` | `/api/Loans` | Admin / Customer | `LoanRequestDto` (`Amount`, `Tenor 1-120`, `ProfitRate 0-100` %, `StartDate`, `LoanType`) | `201 Created` → `LoanResponseDto` (taksit planı dahil) | Kredi oluşturur ve taksitleri **otomatik** üretir. Hatalar: `400` validasyon, `404` müşteri yok, `400` skor < 600. |
| `GET`  | `/api/Loans` | Admin / Customer | — | `200` → `LoanResponseDto[]` | Admin → hepsi, Customer → kendi kredileri. |
| `GET`  | `/api/Loans/{id:int}` | Admin / Owner | — | `200` veya `404` / `403` | Belirli krediyi taksit + ödeme bilgisiyle döner. |
| `GET`  | `/api/Loans/by-customer/{customerId:int}` | Admin / Owner | — | `200` → `LoanResponseDto[]` | Müşterinin tüm kredileri. |

### `LoanRequestDto` örneği

```json
{
  "customerId": 12,
  "amount": 60000,
  "tenor": 12,
  "profitRate": 24,
  "startDate": "2026-06-01T00:00:00",
  "loanType": 0
}
```

`loanType`: `0` Personal (İhtiyaç), `1` Education (Eğitim), `2` Vehicle (Taşıt)

---

## 4. Installments — `/api/Installments` (`[Authorize]`)

| Method | Path | Yetki | Yanıt | Açıklama |
|---|---|---|---|---|
| `GET`  | `/api/Installments/by-loan/{loanId:int}` | Admin / Owner | `InstallmentDto[]` | Krediye ait tüm taksitler. |
| `GET`  | `/api/Installments/{id:int}` | Admin / Owner | `InstallmentDto` veya `404` | Tek taksit detayı. |
| `GET`  | `/api/Installments/unpaid/by-customer/{customerId:int}` | Admin / Owner | `UnpaidInstallmentDto[]` | Müşterinin ödenmemiş taksitleri. |
| `GET`  | `/api/Installments/overdue/by-customer/{customerId:int}` | Admin / Owner | `UnpaidInstallmentDto[]` | Müşterinin gecikmiş taksitleri. |
| `POST` | `/api/Installments/update-overdue` | **Admin** | `{ message, updatedCount }` | `DueDate < now` ve `Unpaid` olan tüm taksitleri toplu `Overdue` yapar. |

---

## 5. Payments — `/api/Payments` (`[Authorize]`)

| Method | Path | Yetki | Body | Yanıt | Açıklama |
|---|---|---|---|---|---|
| `POST` | `/api/Payments` | Admin / Customer | `PaymentRequestDto { loanId }` | `200` → `PaymentResponseDto` | Krediye ait **sıradaki ödenmemiş taksiti** öder. Hatalar: `402 Payment Required` decline, `404` kredi yok, `400` ödenecek taksit yok, `403` yetkisiz. |
| `GET`  | `/api/Payments` | **Admin** | — | `PaymentResponseDto[]` | Tüm ödemeler. |
| `GET`  | `/api/Payments/by-customer/{customerId:int}` | Admin / Owner | — | `PaymentResponseDto[]` | Müşterinin ödeme geçmişi. |

### `402 Payment Required` örnek yanıt (Stripe-uyumlu)

```json
{
  "status": "Declined",
  "declineCode": "insufficient_funds",
  "providerName": "Stripe Sandbox",
  "message": "Kart bakiyesi yetersiz."
}
```

Olası `declineCode` değerleri: `insufficient_funds`, `card_declined`, `expired_card`, `processing_error`, `do_not_honor`.

---

## 6. Credit Scores — `/api/CreditScores` (`[Authorize]`)

| Method | Path | Yetki | Yanıt | Açıklama |
|---|---|---|---|---|
| `GET` | `/api/CreditScores/{customerId:int}` | Admin / Owner | `200` → detaylı skor zarfı | Mock Findeks bürosundan detaylı skor + risk seviyesi + faktörler. `503` → sağlayıcıya ulaşılamadı. |
| `GET` | `/api/CreditScores/{customerId:int}/score` | Admin / Owner | `200` → `{ customerId, creditScore }` | Geriye dönük uyumluluk için yalnızca sayısal skor. |

### Detaylı yanıt örneği

```json
{
  "customerId": 12,
  "creditScore": 720,
  "riskLevel": "Low",
  "providerName": "Findeks Mock",
  "providerReference": "fk_ref_8e1f9c…",
  "queriedAtUtc": "2026-05-12T19:30:00Z",
  "isEligible": true,
  "factors": [
    { "code": "PAYMENT_HISTORY", "label": "Ödeme Geçmişi", "delta": 30 },
    { "code": "UTILIZATION", "label": "Kullanım Oranı", "delta": -10 }
  ]
}
```

> Krediye başvurmak için skor **≥ 600** olmalıdır (`LoanService.CreateLoanAsync`).

---

## 7. Ortak Hata Zarfı

`GlobalExceptionHandlerMiddleware` tüm yakalanmamış hataları aşağıdaki şemaya çevirir:

```json
{
  "message": "Hata açıklaması",
  "errors": ["isteğe bağlı validasyon mesajı listesi"],
  "details": "yalnızca development'ta görünebilir"
}
```

### HTTP durum kodu özeti

| Kod | Anlam | Örnek |
|---|---|---|
| `200` / `201` | Başarı | Standart işlem. |
| `400` | Bad Request | Validasyon, geçersiz ID, iş kuralı ihlali. |
| `401` | Unauthorized | Token yok / geçersiz / süresi dolmuş. |
| `402` | Payment Required | Ödeme reddedildi (Stripe-uyumlu). |
| `403` | Forbidden | Rol veya `customer_id` uyuşmadı. |
| `404` | Not Found | Kayıt yok. |
| `409` | Conflict | Aynı kullanıcı adı / TC / e-posta. |
| `500` | Internal Server Error | Beklenmeyen sunucu hatası. |
| `503` | Service Unavailable | Dış servis (Findeks / Stripe) erişilemez. |

---

## 8. Yetki Matrisi (Hızlı Bakış)

| Endpoint | Anonymous | Customer | Admin |
|---|:-:|:-:|:-:|
| `POST /api/Auth/register` | ✓ | ✓ | ✓ |
| `POST /api/Auth/login`    | ✓ | ✓ | ✓ |
| `GET /api/Customers`      |   |   | ✓ |
| `GET /api/Customers/{id}` |   | ✓ (kendi) | ✓ |
| `POST /api/Customers`     |   |   | ✓ |
| `PUT /api/Customers/{id}` |   | ✓ (kendi) | ✓ |
| `DELETE /api/Customers/{id}` |   |   | ✓ |
| `POST /api/Customers/{id}/restore` |   |   | ✓ |
| `GET /api/Customers/{id}/summary` |   | ✓ (kendi) | ✓ |
| `POST /api/Loans`         |   | ✓ | ✓ |
| `GET /api/Loans`          |   | ✓ (kendi) | ✓ |
| `GET /api/Loans/{id}`     |   | ✓ (kendi) | ✓ |
| `GET /api/Loans/by-customer/{customerId}` |   | ✓ (kendi) | ✓ |
| `GET /api/Installments/...` |   | ✓ (kendi) | ✓ |
| `POST /api/Installments/update-overdue` |   |   | ✓ |
| `POST /api/Payments`      |   | ✓ | ✓ |
| `GET /api/Payments`       |   |   | ✓ |
| `GET /api/Payments/by-customer/{customerId}` |   | ✓ (kendi) | ✓ |
| `GET /api/CreditScores/{customerId}` |   | ✓ (kendi) | ✓ |

> "Kendi" = `claim.customer_id == route.customerId` veya kaydın sahibinin müşterisi olmak.

---

## 9. İlgili Kaynaklar

- Controller'lar: `backend/LoanManagement.API/Controllers/`
- DTO'lar: `backend/LoanManagement.Entities/DTOs/`
- Servis arayüzleri: `backend/LoanManagement.Business/Abstract/`
- Tam entegrasyon kontratı: [`API_INTEGRATION_DOCUMENTATION.md`](../API_INTEGRATION_DOCUMENTATION.md)
