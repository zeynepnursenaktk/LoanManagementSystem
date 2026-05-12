# Loan Management System — API Entegrasyon Belgesi (API Contract)

> Bu belge, **Backend Developer** rolü tarafından `.NET 8` Controller / Service / DTO / Enum kaynak kodları analiz edilerek üretilmiştir.
> Frontend Developer bu sözleşmeyi referans alarak `types/`, `services/` ve UI katmanlarını üretmelidir.
>
> **Sürüm: v1.4** — Admin müşteri yönetimi artık **soft-delete edilmiş kayıtları da listeleyip aktife çevirebiliyor**. `GET /api/Customers` `?includeDeleted=true` parametresi ile silinmiş müşterileri de döner; yeni `POST /api/Customers/{id}/restore` endpoint'i pasif müşterileri yeniden aktifleştirir. `CustomerListDto`/`CustomerResponseDto` artık `isDeleted` ve `deletedAtUtc` alanlarını içerir.
>
> **Sürüm: v1.3** — Müşteri kayıt akışı için **bankacılık-uyumlu sıkı validasyonlar** devreye alındı: gerçek TCKN checksum kontrolü, Türkiye GSM telefon kuralı, kuvvetli şifre politikası ve sunucu tarafı normalize (trim + lowercase email + telefon normalizasyonu). Detaylar için bkz. [§ Validasyon Sözleşmesi](#validasyon-sözleşmesi).
>
> **Sürüm: v1.2** — Sandbox Credit Bureau artık müşterinin **gerçek kredi/taksit geçmişine** göre dinamik skor üretir ve "neden bu skor?" sorusuna yanıt veren `factors[]` döner. Bkz. [§ Değişiklik Kayıt Notları](#değişiklik-kayıt-notları).
>
> **Sürüm: v1.1** — Mock Payment Gateway ve Mock Credit Score servisleri **dışsal sandbox sağlayıcılarına** (Stripe-uyumlu Payment Intents API ve Findeks-uyumlu Credit Bureau API) taşınmıştır.

## Genel Bilgiler

| Alan | Değer |
| --- | --- |
| Backend Framework | ASP.NET Core (.NET 8) |
| Auth | JWT Bearer (24 saat) |
| Base URL (HTTP) | `http://localhost:5213` |
| Base URL (HTTPS) | `https://localhost:7135` |
| Swagger UI | `{BASE_URL}/swagger` (yalnızca Development) |
| CORS Allowed Origins | `http://localhost:5173`, `http://127.0.0.1:5173`, `http://localhost:4173`, `http://localhost:3000` |
| Roller | `Admin`, `Customer` |
| Admin seed | `username: admin` / `password: Admin123!` |
| Date format | ISO-8601 (`2026-05-12T10:30:00Z`) |
| Number format | `decimal` → JSON `number` (kuruş hassasiyetiyle, max 2 ondalık) |

### JWT Claim Sözleşmesi
| Claim | Değer |
| --- | --- |
| `ClaimTypes.Name` | `username` |
| `ClaimTypes.GivenName` | `fullName` |
| `ClaimTypes.Role` | `Admin` veya `Customer` |
| `customer_id` (custom) | Müşteri kimliği (yalnızca `Customer` rolünde dolu) |

### Authorization Header
```
Authorization: Bearer <JWT_TOKEN>
```

### Yetki Modeli (özet)
| Endpoint Grubu | Anonim | Customer | Admin |
| --- | --- | --- | --- |
| `POST /api/Auth/register` | ✅ | – | – |
| `POST /api/Auth/login` | ✅ | – | – |
| `GET /api/Customers` | ❌ | ❌ | ✅ |
| `GET /api/Customers/{id}` | ❌ | yalnızca kendi `customer_id` | ✅ |
| `POST /api/Customers` | ❌ | ❌ | ✅ |
| `PUT /api/Customers/{id}` | ❌ | yalnızca kendi `customer_id` | ✅ |
| `DELETE /api/Customers/{id}` | ❌ | ❌ | ✅ |
| `POST /api/Customers/{id}/restore` | ❌ | ❌ | ✅ |
| `GET /api/Customers/{id}/summary` | ❌ | yalnızca kendi | ✅ |
| `POST /api/Loans` | ❌ | `CustomerId` zorla kendi id'sine eşitlenir | ✅ |
| `GET /api/Loans` | ❌ | yalnızca kendi kredileri | ✅ tümünü görür |
| `GET /api/Loans/{id}` | ❌ | yalnızca kendine ait kredi | ✅ |
| `GET /api/Loans/by-customer/{customerId}` | ❌ | yalnızca kendi | ✅ |
| `GET /api/Installments/by-loan/{loanId}` | ❌ | sahip olduğu kredi | ✅ |
| `GET /api/Installments/{id}` | ❌ | sahip olduğu taksit | ✅ |
| `GET /api/Installments/unpaid/by-customer/{customerId}` | ❌ | yalnızca kendi | ✅ |
| `GET /api/Installments/overdue/by-customer/{customerId}` | ❌ | yalnızca kendi | ✅ |
| `POST /api/Installments/update-overdue` | ❌ | ❌ | ✅ |
| `POST /api/Payments` | ❌ | yalnızca kendi kredisine | ✅ |
| `GET /api/Payments` | ❌ | ❌ | ✅ |
| `GET /api/Payments/by-customer/{customerId}` | ❌ | yalnızca kendi | ✅ |
| `GET /api/CreditScores/{customerId}` | ❌ | yalnızca kendi | ✅ |

---

## Enum Sözleşmesi (C# → TS dönüşümü)

> Endpoint payload'larında **request tarafında numerik (int)**, **response tarafında string** olarak döner.
> Aşağıdaki dönüşüm Frontend `types/enums.ts` dosyası için referanstır.

### `LoanType` (request: int)
```csharp
public enum LoanType { Personal = 0, Education = 1, Vehicle = 2 }
```
Response'da `LoanTypeName` alanı **lokalize string** olarak gelir: `"İhtiyaç Kredisi"`, `"Eğitim Kredisi"`, `"Araç Kredisi"`.

### `LoanStatus` (response: string)
```csharp
public enum LoanStatus { Active = 1, Closed = 2 }
```
Response'da `Status: "Active" | "Closed"`.

### `InstallmentStatus` (response: string)
```csharp
public enum InstallmentStatus { Unpaid = 1, Paid = 2, Overdue = 3 }
```
Response'da `Status: "Unpaid" | "Paid" | "Overdue"`.

---

## Tip Eşleme Tablosu (C# → TypeScript)

| C# Tipi | TypeScript Tipi | Not |
| --- | --- | --- |
| `int`, `decimal`, `double`, `float` | `number` | – |
| `string` | `string` | – |
| `bool` | `boolean` | – |
| `DateTime`, `DateTime?` | `string` (ISO) ↔ `Date` runtime | Backend her zaman ISO string döner |
| `Guid` | `string` | (Bu projede kullanılmıyor – ID'ler `int`) |
| `enum` (request) | `number` (enum) | `LoanType` int olarak gider |
| `enum` (response) | `string` literal union | API string döner |
| `List<T>` | `T[]` | – |
| `T?` (nullable) | `T | null` veya `T | undefined` | – |

---

## 1) AuthController — `/api/Auth`

> Tüm endpoint'ler `[AllowAnonymous]`.

### 1.1 `POST /api/Auth/register`
Self-servis kayıt: hem `User` (auth) hem `Customer` (CRM) kaydı oluşturulur. Otomatik `Customer` rolü atanır.

**Request — `RegisterRequestDto`**
```jsonc
{
  "username": "ahmet.yilmaz",      // string, 3-64 char, zorunlu, unique
  "password": "secret123",          // string, min 4 char, zorunlu
  "firstName": "Ahmet",             // string, 2-100 char, zorunlu
  "lastName": "Yılmaz",             // string, 2-100 char, zorunlu
  "identityNumber": "12345678901",  // string, 11 digit numeric, zorunlu, unique
  "email": "ahmet@example.com",    // string, email format, zorunlu, unique
  "phoneNumber": "+905551234567"    // string?, geçerli telefon, opsiyonel
}
```

**Response — `200 OK` — `LoginResponseDto`**
```jsonc
{
  "token": "eyJhbGciOi...",          // string (JWT)
  "username": "ahmet.yilmaz",        // string
  "fullName": "Ahmet Yılmaz",        // string
  "role": "Customer",                // "Admin" | "Customer"
  "customerId": 42,                  // number | null (Admin için null)
  "expiresAt": "2026-05-13T10:00:00Z" // ISO datetime
}
```

**Hata Yanıtları**
| Status | Senaryo | Body |
| --- | --- | --- |
| `400 Bad Request` | Validasyon | `{ "message": "...", "errors": [...] }` |
| `409 Conflict` | Username / TC / Email zaten var | `{ "message": "..." }` |

---

### 1.2 `POST /api/Auth/login`

**Request — `LoginRequestDto`**
```jsonc
{
  "username": "admin",
  "password": "Admin123!"
}
```

**Response — `200 OK`** — `LoginResponseDto` (yukarıdaki ile aynı).

**Hata Yanıtları**
| Status | Senaryo |
| --- | --- |
| `401 Unauthorized` | Geçersiz kullanıcı adı veya şifre |

---

## 2) CustomersController — `/api/Customers`

> Tüm endpoint'ler `[Authorize]`. Aşağıdaki tabloda Admin ve Customer ayrımları belirtilmiştir.

### 2.1 `GET /api/Customers` — **Admin only**
Müşteri özet listesi.

**Query Parametreleri**

| İsim | Tür | Default | Açıklama |
| --- | --- | --- | --- |
| `includeDeleted` | `bool` | `false` | `true` ise soft-delete edilmiş müşteriler de listeye dahil edilir. Admin "Müşteri Yönetimi" ekranı tüm kayıtları (aktif + pasif) ve "Aktife Çevir" akışını desteklemek için bu parametreyi **her zaman `true`** göndermelidir. |

**Örnek istek**
```
GET /api/Customers?includeDeleted=true
Authorization: Bearer <admin_jwt>
```

**Response — `200 OK` — `CustomerListDto[]`**
```jsonc
[
  {
    "id": 1,
    "firstName": "Ahmet",
    "lastName": "Yılmaz",
    "email": "ahmet@example.com",
    "phoneNumber": "+905551234567",
    "isDeleted": false,            // false → "Aktif" badge
    "deletedAtUtc": null,
    "totalLoans": 2,
    "loans": [ /* LoanSummaryDto[] (aşağıda) */ ]
  },
  {
    "id": 8,
    "firstName": "Mehmet",
    "lastName": "Kaya",
    "email": "mehmet@example.com",
    "phoneNumber": "+905559998877",
    "isDeleted": true,                                    // true → "Pasif/Silinmiş" badge
    "deletedAtUtc": "2026-05-10T08:42:11.000Z",
    "totalLoans": 0,
    "loans": []
  }
]
```

> **Frontend kuralı**: Tablodaki "İşlemler" kolonu `isDeleted`'a göre dinamik:
> - `isDeleted == false` → "Sil" butonu görünür.
> - `isDeleted == true`  → "Sil" butonu yerine **"Aktife Çevir"** butonu görünür ve `POST /api/Customers/{id}/restore` çağrılır.

`LoanSummaryDto`:
```jsonc
{
  "loanId": 10,
  "loanTypeName": "İhtiyaç Kredisi",
  "status": "Active",
  "amount": 50000.00,
  "tenor": 12,
  "startDate": "2026-05-12T00:00:00Z",
  "totalInstallments": 12,
  "paidInstallments": 3,
  "unpaidInstallments": 8,
  "overdueInstallments": 1,
  "remainingDebt": 44000.00
}
```

### 2.2 `GET /api/Customers/{id}` — Admin veya kendisi
Route param: `id: int (>0)`.

**Response — `200 OK` — `CustomerResponseDto`** (CustomerListDto + `identityNumber`)
```jsonc
{
  "id": 1,
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "email": "ahmet@example.com",
  "phoneNumber": "+905551234567",
  "identityNumber": "12345678950",
  "isDeleted": false,
  "deletedAtUtc": null,
  "totalLoans": 2,
  "loans": [ /* LoanSummaryDto[] */ ]
}
```

| Status | Senaryo |
| --- | --- |
| `400` | Geçersiz id |
| `403` | Başka müşterinin kaydına erişim |
| `404` | Bulunamadı |

### 2.3 `POST /api/Customers` — **Admin only**

**Request — `CreateCustomerDto`**
```jsonc
{
  "firstName": "Ayşe",        // string, 2-100, zorunlu
  "lastName": "Demir",        // string, 2-100, zorunlu
  "identityNumber": "98765432101", // string, 11 hane numeric, unique
  "email": "ayse@example.com",
  "phoneNumber": "+905557654321"   // string?
}
```

**Response — `201 Created`**
```jsonc
{ "id": 7, "message": "Müşteri başarıyla oluşturuldu." }
```
Location header: `/api/Customers/7`.

| Status | Senaryo |
| --- | --- |
| `400` | Validasyon |
| `409` | TC / Email zaten var |

### 2.4 `PUT /api/Customers/{id}` — Admin veya kendisi

**Request — `UpdateCustomerDto`**
```jsonc
{
  "email": "yeni@example.com",
  "phoneNumber": "+905551112233"
}
```

**Response — `200 OK`**
```jsonc
{ "message": "Müşteri başarıyla güncellendi." }
```

### 2.5 `DELETE /api/Customers/{id}` — **Admin only**
Soft delete. Aktif kredisi olan müşteri silinemez (`400`). Müşteri fiziksel olarak silinmez; `IsDeleted=true` ve `DeletedAtUtc` doldurulur. `GET /api/Customers?includeDeleted=true` ile pasif liste içerisinde görünmeye devam eder.

**Response — `200 OK`**
```jsonc
{ "message": "Müşteri silindi (kayıt arşivlendi)." }
```

### 2.6 `POST /api/Customers/{id}/restore` — **Admin only** *(yeni — v1.4)*

Soft-delete edilmiş bir müşteriyi **yeniden aktifleştirir** (`IsDeleted=false`, `DeletedAtUtc=null`). Yalnızca `Admin` rolü çağırabilir. Müşterinin kredileri korunur; restore sonrası kredi listeleme query filter'ı tekrar görünür hâle getirir.

| Durum | Anlam |
| --- | --- |
| `200 OK` | Restore başarılı. Body: `{ "message": "Müşteri yeniden aktifleştirildi." }` |
| `400 Bad Request` | `id <= 0` |
| `403 Forbidden` | Admin değil |
| `404 Not Found` | Müşteri bulunamadı **veya** zaten aktif |
| `500 Internal Server Error` | Beklenmeyen hata |

**Örnek istek**
```
POST /api/Customers/8/restore
Authorization: Bearer <admin_jwt>
```

**Başarılı yanıt**
```jsonc
{ "message": "Müşteri yeniden aktifleştirildi." }
```

**Bulunamadı / zaten aktif**
```jsonc
{ "message": "Müşteri bulunamadı veya zaten aktif." }
```

### 2.7 `GET /api/Customers/{id}/summary` — Admin veya kendisi

**Response — `200 OK` — `CustomerSummaryDto`**
```jsonc
{
  "customerId": 1,
  "fullName": "Ahmet Yılmaz",
  "totalLoans": 2,
  "activeLoans": 1,
  "closedLoans": 1,
  "totalDebt": 44000.00,
  "totalPaid": 18000.00,
  "totalInstallments": 24,
  "paidInstallments": 15,
  "unpaidInstallments": 8,
  "overdueInstallments": 1,
  "loans": [
    {
      "loanId": 10,
      "loanTypeName": "İhtiyaç Kredisi",
      "status": "Active",
      "amount": 50000.00,
      "totalPayable": 58000.00,
      "totalPaid": 14000.00,
      "remainingDebt": 44000.00,
      "totalInstallments": 12,
      "paidInstallments": 3,
      "unpaidInstallments": 8,
      "overdueInstallments": 1
    }
  ]
}
```

---

## 3) LoansController — `/api/Loans`

### 3.1 `POST /api/Loans` — Authenticated
Customer rolü için backend `customerId`'yi otomatik olarak token'daki `customer_id`'ye eşitler (body'deki değer override edilir).

**Request — `LoanRequestDto`**
```jsonc
{
  "customerId": 1,        // int >0 (Customer için backend override eder, yine de gönder)
  "amount": 50000,        // decimal (>0, ≤ 1.000.000.000)
  "tenor": 12,            // int [1..120] ay
  "profitRate": 24,       // decimal yıllık yüzde (örn 24 = %24), [0..100]
  "startDate": "2026-06-01T00:00:00Z",
  "loanType": 0           // 0=Personal, 1=Education, 2=Vehicle
}
```

**Response — `201 Created` — `LoanResponseDto`**
```jsonc
{
  "id": 25,
  "customerId": 1,
  "customerFullName": "Ahmet Yılmaz",
  "loanTypeName": "İhtiyaç Kredisi",
  "amount": 50000.00,
  "tenor": 12,
  "profitRate": 24.0,          // yüzde olarak döner
  "totalPayable": 62000.00,
  "startDate": "2026-06-01T00:00:00Z",
  "status": "Active",
  "installments": [
    {
      "id": 301,
      "loanId": 25,
      "installmentNumber": 1,
      "amount": 5166.67,
      "dueDate": "2026-07-01T00:00:00Z",
      "status": "Unpaid",
      "isPaid": false,
      "paidAmount": null,
      "paymentDate": null
    }
  ]
}
```
| Status | Senaryo |
| --- | --- |
| `400` | Validasyon, kredi skoru yetersiz (<600), geçersiz LoanType vb. |
| `404` | Müşteri bulunamadı |

### 3.2 `GET /api/Loans`
Admin için **tüm krediler**, Customer için yalnızca **kendi kredileri** (otomatik filtre).

**Response:** `LoanResponseDto[]`

### 3.3 `GET /api/Loans/{id}`
Tek kredi detayı. `LoanResponseDto`.

### 3.4 `GET /api/Loans/by-customer/{customerId}`
Belirli müşterinin tüm kredileri. Customer yalnızca kendi `customerId`'sini sorgulayabilir.

**Response:** `LoanResponseDto[]`

---

## 4) InstallmentsController — `/api/Installments`

### 4.1 `GET /api/Installments/by-loan/{loanId}` — Authenticated
Müşteri yalnızca kendi kredisinin taksitlerini görebilir.

**Response — `InstallmentDto[]`**
```jsonc
[
  {
    "id": 301,
    "loanId": 25,
    "installmentNumber": 1,
    "amount": 5166.67,
    "dueDate": "2026-07-01T00:00:00Z",
    "status": "Unpaid",
    "isPaid": false,
    "paidAmount": null,
    "paymentDate": null
  }
]
```

### 4.2 `GET /api/Installments/{id}`
Tek taksit detayı — `InstallmentDto`.

### 4.3 `GET /api/Installments/unpaid/by-customer/{customerId}`
**Response — `UnpaidInstallmentDto[]`**
```jsonc
[
  {
    "id": 302,
    "loanId": 25,
    "installmentNumber": 2,
    "amount": 5166.67,
    "dueDate": "2026-08-01T00:00:00Z"
  }
]
```

### 4.4 `GET /api/Installments/overdue/by-customer/{customerId}`
Aynı şema (`UnpaidInstallmentDto[]`), ama yalnızca `Status = Overdue` olanlar.

### 4.5 `POST /api/Installments/update-overdue` — **Admin only**
Vadesi geçmiş tüm taksitleri toplu olarak `Overdue` durumuna alır.

**Response — `200 OK`**
```jsonc
{ "message": "5 taksit gecikmiş olarak güncellendi.", "updatedCount": 5 }
```

---

## 5) PaymentsController — `/api/Payments`

### 5.1 `POST /api/Payments` — Authenticated
Belirtilen krediye sıradaki (en küçük numaralı, ödenmemiş) taksiti otomatik öder. Aynı taksit iki kez ödenemez. Tüm taksitler ödendiğinde kredi `Closed` olur.

> **v1.1 — Dış servis entegrasyonu**: Bu endpoint artık Stripe-uyumlu dış sandbox payment gateway'i çağırır. Sağlayıcı reddederse `402 Payment Required` döner ve body'de `declineCode` bulunur. Frontend, kullanıcıya **spesifik mesajı** göstermek için bu kodu mapleyerek toast/dialog'ta sunmalıdır.

**Request — `PaymentRequestDto`**
```jsonc
{ "loanId": 25 }   // int >0
```

**Response — `200 OK` — `PaymentResponseDto` (v1.1 — `status`, `declineCode`, `providerName`, `providerReference`, `processedAtUtc` eklendi)**
```jsonc
{
  "status": "Succeeded",                         // "Succeeded" | "Declined" | "Failed"
  "declineCode": null,                            // başarılıda null
  "providerName": "Stripe Sandbox",
  "providerReference": "pi_sandbox_abc123def456", // dış sağlayıcının PaymentIntent ID'si
  "message": "Kredi #25 — Taksit #1 başarıyla ödendi.",
  "paymentId": 501,
  "customerId": 1,
  "customerName": "Ahmet Yılmaz",
  "loanId": 25,
  "loanTypeName": "İhtiyaç Kredisi",
  "installmentId": 301,
  "installmentNumber": 1,
  "paidAmount": 5166.67,
  "paymentDate": "2026-07-01T12:34:56Z",
  "processedAtUtc": "2026-07-01T12:34:56Z",
  "isLoanClosed": false
}
```

**Response — `402 Payment Required` — Ödeme Sağlayıcısı Reddetti (v1.1)**
```jsonc
{
  "statusCode": 402,
  "message": "Kart bakiyesi/limiti yetersiz.",
  "details": {
    "status": "Declined",
    "declineCode": "insufficient_funds",
    "providerName": "Stripe Sandbox"
  }
}
```

**Decline Kodları Tablosu (`declineCode`)**

| Kod | HTTP | Anlamı | UI Mesajı (Tr) |
| --- | --- | --- | --- |
| `insufficient_funds` | 402 | Kart limiti/bakiyesi yetersiz | "Kart limiti yetersiz. Lütfen başka bir ödeme yöntemi deneyin." |
| `card_declined` | 402 | Kart bankaca reddedildi | "Ödeme bankanız tarafından reddedildi." |
| `expired_card` | 402 | Kart süresi geçmiş | "Kartın son kullanma tarihi geçmiş." |
| `incorrect_cvc` | 402 | CVC yanlış | "CVC bilgisi hatalı." |
| `fraudulent` | 402 | Şüpheli işlem | "İşlem güvenlik nedeniyle reddedildi." |
| `invalid_amount` | 400 | Geçersiz tutar | "Ödeme tutarı geçersiz." |
| `processing_error` | 500/402 | Sağlayıcı hatası | "Ödeme sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyin." |
| `gateway_timeout` | 504 | Ağ/timeout | "Ödeme sağlayıcısına ulaşılamadı. Lütfen biraz sonra tekrar deneyin." |

**Diğer Hata Kodları**

| Status | Senaryo |
| --- | --- |
| `400` | Ödenecek taksit kalmadı, validation |
| `402` | Dış sağlayıcı reddi (yukarı tabloya bak) |
| `403` | Başka müşterinin kredisine ödeme denemesi |
| `404` | Kredi bulunamadı |

**Frontend Loading/Status Semantiği (v1.1)**

- İstek atılırken modal'a `isProcessing=true` set edilir; "Ödemeniz işleniyor, lütfen bekleyin..." göster.
- Dış sağlayıcı çağrısı tipik olarak **200–800ms** sürer (sandbox'ta ~180ms simüle edilir; production'da Stripe ~300ms).
- HTTP 200 → success modal (`status=Succeeded`, `providerReference` göster).
- HTTP 402 → error modal'da decline mesajını kullan; "Tekrar Dene" butonu sun.
- HTTP 504/503 → "Sağlayıcıya ulaşılamadı" + otomatik retry önerisi.
- Idempotency: Aynı `loanId + installmentNumber` çiftine yapılan tekrar denemeler aynı `Idempotency-Key` ile gönderilir (backend'de oluşturulur); çift ödeme oluşmaz.

### 5.2 `GET /api/Payments` — **Admin only**
Tüm ödeme kayıtları. `PaymentResponseDto[]`.

### 5.3 `GET /api/Payments/by-customer/{customerId}`
Müşterinin ödeme geçmişi. `PaymentResponseDto[]`.

---

## 6) CreditScoresController — `/api/CreditScores`

### 6.1 `GET /api/CreditScores/{customerId}` (v1.1 — Findeks Mock entegrasyonu)
Dış kredi bürosundan (Findeks Mock) çağrı yapar. Customer yalnızca kendi skorunu sorgulayabilir.

**Response — `200 OK` (v1.2 — `factors[]` eklendi, dinamik skor)**
```jsonc
{
  "customerId": 1,
  "creditScore": 1175,
  "riskLevel": "Low",                            // "VeryLow" | "Low" | "Medium" | "High" | "VeryHigh"
  "providerName": "Findeks Mock",
  "providerReference": "FNDX-20260512-000001",
  "queriedAtUtc": "2026-05-12T18:01:23.456Z",
  "isEligible": true,                            // creditScore >= 600
  "factors": [                                   // v1.2 — skorun nasıl oluştuğu (sandbox'ta her zaman dolu)
    { "code": "base", "label": "Başlangıç skoru", "delta": 1000 },
    { "code": "closed_loan_bonus", "label": "1 kapatılmış kredi", "delta": 150 },
    { "code": "on_time_payment_bonus", "label": "1 ödenmiş taksit", "delta": 25 }
  ]
}
```

> `factors[].delta` toplamı `creditScore` ile eşittir (clamp uygulanmışsa `clamp` adında bir faktör de listede yer alır). Production sağlayıcısı `factors` sağlamayabilir; o durumda dizi boş döner — UI fallback gösterimini düşünmelidir.

**Risk Seviyesi Tablosu**

| Risk | Skor Aralığı | UI Renk Önerisi |
| --- | --- | --- |
| `VeryLow` | ≥ 1500 | yeşil |
| `Low` | 1100–1499 | açık yeşil |
| `Medium` | 800–1099 | turuncu |
| `High` | 600–799 | koyu turuncu |
| `VeryHigh` | < 600 | kırmızı (kredi reddi) |

**Hata Yanıtları**

| Status | Senaryo | Body |
| --- | --- | --- |
| `400` | Geçersiz id | `{ "message": "Geçersiz müşteri ID..." }` |
| `403` | Başka müşterinin skoru | `{ "message": "Yalnızca kendi kredi skorunuzu..." }` |
| `404` | Müşteri yok | `{ "message": "Customer with id N not found." }` |
| `503` | Dış sağlayıcı erişilemiyor | `{ "providerError": true, "message": "Kredi skoru sağlayıcısına ulaşılamadı..." }` |

> Yan endpoint `GET /api/CreditScores/{customerId}/score` v1.0 ile geriye dönük uyumluluk amacıyla bırakıldı (yalnızca `{ customerId, creditScore }` döner).

---

## Standart Hata Yanıt Şeması

`GlobalExceptionHandlerMiddleware` tarafından üretilen unified error response:

```jsonc
{
  "statusCode": 400,
  "message": "Giriş verisi validasyonu başarısız.",
  "details": {
    "Amount": ["Kredi tutarı 0'dan büyük olmalıdır."]
  }
}
```

Controller-içi hatalar (yakalanmış) için daha düz şema kullanılır:
```jsonc
{ "message": "Müşteri bulunamadı." }
```
veya validation:
```jsonc
{ "message": "Giriş verisi validasyonu başarısız.", "errors": ["..."] }
```

Frontend, **`message`** alanını her zaman kontrol etmeli; `errors` / `details` opsiyoneldir.

---

## Frontend Entegrasyon Notları (Backend → Frontend)

1. **Bearer Token**: Login/Register sonrası `token` alanını localStorage (veya sessionStorage) içine al, her axios isteğine `Authorization: Bearer ${token}` ekle.
2. **Token expiry**: `expiresAt` alanını saklayıp süresi dolanda logout yap; 401 response gelirse token'ı temizle ve login sayfasına yönlendir.
3. **Role-based UI**: `role === 'Admin'` ise tüm müşteri/ödeme listeleri görünsün; `Customer` ise sadece kendi paneli.
4. **Customer scoping**: Müşteri rolünde `customerId` token'dan okunur; çoğu endpoint için Frontend hard-coded gönderse de backend zaten override eder, ama tutarlılık için login response'undaki `customerId`'yi state'te tutup kullan.
5. **Enum çevirileri**: `loanType` request'inde numerik gönder, response'ta `loanTypeName` zaten lokalize string gelir; UI'da bunu doğrudan göster.
6. **Decimal hassasiyet**: Tutarlar `number` ama 2 ondalık beklenir; `Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' })` ile formatla.
7. **Tarih**: API ISO string döner. UI'da `new Date(isoString)` ile parse edip `dd.MM.yyyy` göster.
8. **CORS**: Backend `http://localhost:5173` (Vite default) için zaten açık. Frontend `VITE_API_BASE_URL=http://localhost:5213` ile başlatılmalı.
9. **Çift ödeme koruması**: `POST /api/Payments` idempotent değildir; UI'da çift tıklamayı `isLoading` ile engelle ve onay modalı göster.

---

## Dış Servisler (v1.1)

### Mimari
- `IExternalPaymentGatewayClient` — Stripe-uyumlu `POST /v1/payment_intents` sözleşmesi. Tipli `HttpClient` (HttpClientFactory + Polly retry).
- `IExternalCreditBureauClient` — Findeks-uyumlu `GET /api/v1/credit-score/{customerId}` sözleşmesi.
- `UseSandboxHandler=true` iken `DelegatingHandler` ile internet bağımsız deterministic yanıt üretilir; production'da false ile gerçek Stripe Sandbox'a gider.

### Sandbox Senaryo Tetikleyicileri (test amaçlı)
Backend `StripeSandboxDelegatingHandler`, ödeme tutarının **kuruş kısmının son iki hanesine** göre senaryo seçer:

| Tutar (kuruş % 100) | HTTP | declineCode |
| --- | --- | --- |
| 01 | 402 | `insufficient_funds` |
| 02 | 402 | `card_declined` |
| 03 | 402 | `expired_card` |
| 04 | 504 | `gateway_timeout` |
| 05 | 500 | `processing_error` |
| 00, 06..99 | 200 | succeeded |
| 1.000.000 TRY üzeri | 402 | `fraudulent` |

> Test için: 100.05 TRY → processing_error, 250.04 TRY → gateway_timeout. Frontend ekran testleri bu kurala göre senaryo üretebilir.

### Konfigürasyon (`appsettings.json`)
```jsonc
"ExternalServices": {
  "PaymentGateway": {
    "ProviderName": "Stripe Sandbox",
    "BaseUrl": "https://api.stripe.com/v1/",
    "ApiKey": "sk_test_LoanMsSandboxKey",
    "Currency": "TRY",
    "TimeoutSeconds": 10,
    "UseSandboxHandler": true,    // false yapınca gerçek HTTP'ye çıkar
    "RetryCount": 2
  },
  "CreditBureau": {
    "ProviderName": "Findeks Mock",
    "BaseUrl": "https://findeks-mock.example.com/api/v1/",
    "ApiKey": "fk_test_LoanMsSandboxKey",
    "TimeoutSeconds": 8,
    "UseSandboxHandler": true,
    "RetryCount": 2
  }
}
```

---

## Validasyon Sözleşmesi

Müşteri kayıt (`POST /api/Auth/register`) ve müşteri oluşturma (`POST /api/Customers`) için backend, **DTO seviyesinde DataAnnotation** + **servis seviyesinde defensive kontrol** + **normalizasyon** üçlüsünü uygular. Frontend bu kurallarla birebir aynı şemayı Zod refinement'ları üzerinden tekrarlar (`frontend/src/utils/validation.ts`).

### Alan bazlı kurallar

| Alan | Kural | Hata Mesajı (örnek) |
| --- | --- | --- |
| `username` | 3–64 karakter; `a-zA-Z0-9._-`; benzersiz | "Kullanıcı adı yalnızca harf, sayı, nokta, alt çizgi ve tire içerebilir." |
| `password` | 8–128 karakter; **en az 1 büyük + 1 küçük + 1 rakam** | "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir." |
| `firstName`, `lastName` | 2–100 karakter; **Unicode harf** (Türkçe karakter dahil), boşluk, kesme işareti `'`, tire `-`; ilk karakter harf olmalı | "Ad yalnızca harf, boşluk, kesme işareti ve tire içerebilir." |
| `identityNumber` | **Türkiye TCKN algoritması**: 11 hane + ilk hane ≠ 0 + `d10` ve `d11` checksum doğruluğu | "Geçerli bir T.C. Kimlik Numarası giriniz (11 haneli, ilk hane 0 olamaz, kontrol haneleri doğru olmalı)." |
| `email` | RFC 5322 + max 255 + benzersiz; sunucuda `Trim().ToLowerInvariant()` ile saklanır | "Geçerli bir e-posta adresi girin." |
| `phoneNumber` | **Türkiye GSM**: opsiyonel `+90` / `90` / `0` ön eki + `5XXXXXXXXX` (toplam 10 hane gövde); boşluk/tire/parantez/nokta tolere edilir. Boş bırakılabilir. | "Geçerli bir Türkiye GSM numarası girin. Örn: 0532 123 45 67 veya +90 532 123 45 67." |

### TCKN doğrulama algoritması (referans uygulaması)

```
Veriler: d1 d2 ... d11 (her biri 0..9)
1. d1..d11 11 hane, hepsi rakam olmalı, d1 ≠ 0
2. d10 = ((d1+d3+d5+d7+d9) * 7 − (d2+d4+d6+d8)) mod 10
3. d11 = (d1+d2+...+d10) mod 10
```

Backend uygulaması: `LoanManagement.Entities.Validation.TurkishIdentityNumberValidator`
Frontend uygulaması: `frontend/src/utils/validation.ts → isValidTurkishIdentityNumber()`

### Telefon normalizasyonu

Frontend bir telefonu olduğu gibi (kullanıcının yazdığı formatta) gönderebilir; backend:
1. Boşluk, tire, parantez, nokta gibi karakterleri temizler.
2. Pattern `^(\+90|90|0)?5[0-9]{9}$` ile eşler.
3. Geçerli ise normalize hâli (örn. `+905321234567`) DB'ye yazılır; geçersizse `ArgumentException` atılır.

### Hata formatı

DTO validasyon başarısız olduğunda controller `400 Bad Request` döner:

```json
{
  "message": "Giriş verisi validasyonu başarısız.",
  "errors": [
    "Geçerli bir T.C. Kimlik Numarası giriniz (11 haneli, ilk hane 0 olamaz, kontrol haneleri doğru olmalı).",
    "Geçerli bir Türkiye GSM numarası girin. Örn: 0532 123 45 67 veya +90 532 123 45 67."
  ]
}
```

Servis seviyesi defensive kontrolden geçemeyen istekler `400 Bad Request` + `ArgumentException.Message` döner. Benzersizlik (TCKN/Email/Username çakışması) ise `409 Conflict` döner.

---

## Değişiklik Kayıt Notları

### v1.4 — 2026-05-12 (Soft-delete Görünürlüğü + Restore)
- **`GET /api/Customers`** artık `?includeDeleted=true` query parametresi alır (varsayılan `false`, geriye-uyumlu). Admin paneli her zaman `true` göndererek aktif + pasif tüm müşterileri listeler.
- **`CustomerListDto` ve `CustomerResponseDto`** iki yeni alan kazandı:
  - `isDeleted: boolean` — durum badge'i için (`false` = "Aktif", `true` = "Pasif/Silinmiş").
  - `deletedAtUtc: string | null` — silinme zamanı (ISO-8601, UTC).
- **Yeni endpoint**: `POST /api/Customers/{id}/restore` — soft-delete edilmiş bir müşteriyi tekrar aktife çevirir (`IsDeleted=false`, `DeletedAtUtc=null`). 404 → bulunamadı veya zaten aktif. Yalnızca `Admin`.
- **Servis katmanı**: `CustomerService.GetCustomersAsync(bool includeDeleted)` parametresi global query filter'ı `IgnoreQueryFilters()` ile bypass eder. `RestoreCustomerAsync` aynı şekilde silinmiş satırlara erişip flag'leri sıfırlar.
- **Test**: 13 yeni unit test (6 controller + 7 servis/in-memory DB). Toplam test sayısı **199** (Başarılı: 199, Başarısız: 0).

### v1.3 — 2026-05-12 (Sıkı Kayıt Validasyonları)
- **TCKN**: Sadece "11 haneli rakam" yerine tam **NVI algoritması** (ilk hane ≠ 0 + `d10` ve `d11` checksum hanesi) zorunlu.
- **Telefon**: Built-in `[Phone]` kaldırıldı, yerine **Türkiye GSM**'e özel `[TurkishPhoneNumber]` attribute kondu (mobil 5xx; +90/90/0 ön eki kabul; boşluk/tire/parantez/nokta tolere edilir).
- **Şifre**: Minimum 4 → **8 karakter** ve büyük/küçük/rakam zorunluluğu.
- **Kullanıcı adı**: `^[a-zA-Z0-9._-]{3,64}$` kuralı; boşluk/özel karakter reddedilir.
- **Ad / Soyad**: Unicode harf + apostrof + tire kuralı; rakam/sembol reddedilir.
- **Normalizasyon**: `AuthService.RegisterAsync` ve `CustomerService.CreateCustomerAsync/UpdateCustomerAsync` artık alanları sunucu tarafında trim eder, e-postayı küçük harfe çevirir, telefonu kanonik forma getirir. E-posta benzersizliği case-insensitive yakalanır.
- **Defensive layer**: Controller bypass edilse bile servis katmanı yine DTO içeriğini doğrular ve hatalı girdide `ArgumentException` atar; controller bunu `400 Bad Request` olarak iletir.
- **Yeni testler**: 86 yeni unit test (validator'lar + DTO DataAnnotation chain + `AuthService.RegisterAsync` davranış testleri). Toplam test sayısı **186** (Başarılı: 186, Başarısız: 0).

### v1.2 — 2026-05-12 (Sandbox Dinamik Skor)
- **Sandbox kredi bürosu** (`CreditBureauSandboxDelegatingHandler`) artık `LoanDbContext`'ten müşterinin gerçek kredi/taksit geçmişini okur ve dinamik skor üretir. Sandbox dışında üretim sağlayıcısı bağlanırsa onun ham yanıtı korunur.
- **Skor formülü** (clamp `[300, 1900]`):
  - Başlangıç: **+1000**
  - Kapatılmış her kredi: **+150**
  - Aktif her kredi: **−50**
  - Ödenmiş her taksit: **+25** (üst sınır **+500**)
  - Vadesi geçmiş (Unpaid + DueDate < now) her taksit: **−10** (compounding)
  - En az bir gecikmiş taksit varsa ek: **−300**
  - Hiç kredi geçmişi yoksa: **−100**
- **Yeni response alanı**: `factors[]` — her birinin `code`, `label`, `delta` alanı vardır; toplamı `creditScore`'a eşittir.
- **Tetikleyici**: müşteri ödeme yaparsa, krediyi kapatırsa, gecikmiş taksiti varsa — sonraki sorguda skor anında değişir. Frontend `CreditScoreCard` üzerindeki "Yenile" ya da sayfayı yeniden açma yeterlidir.

### v1.1 — 2026-05-12
- **Payment**: `MockPaymentGatewayService` → `ExternalPaymentGatewayService` (Stripe-uyumlu HttpClient).
- **CreditScore**: `MockCreditScoreService` → `ExternalCreditScoreService` (Findeks-uyumlu HttpClient).
- **PaymentResponseDto**: `status`, `declineCode`, `providerName`, `providerReference`, `processedAtUtc` alanları eklendi.
- **CreditScores GET**: `riskLevel`, `providerName`, `providerReference`, `queriedAtUtc`, `isEligible` alanları eklendi.
- **Yeni HTTP yanıt**: `402 Payment Required` — Ödeme sağlayıcısı reddi.
- **Yeni HTTP yanıt**: `503 Service Unavailable` — Kredi skoru sağlayıcısı down.
- Polly retry (transient errors için 2 deneme, exponential backoff) eklendi.

### v1.0 — 2026-05-12
- İlk sürüm (Backend Developer çıktısı, Project Owner onayı).
