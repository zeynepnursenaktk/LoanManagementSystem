# Loan Management System

Bireysel kredi başvurusu, taksitlendirme ve tahsilat süreçlerini uçtan uca yöneten tam yığın (full-stack) bir uygulamadır. Backend ASP.NET Core 8 Web API + Entity Framework Core (SQL Server), frontend ise React + TypeScript + Vite + MUI üzerine kuruludur. Dış sistem entegrasyonları (kredi bürosu ve ödeme ağ geçidi) sandbox/mock istemcilerle simüle edilir.

> Detaylı teknik kaynaklar için `docs/` klasörüne bakın:
> - [ER Diyagramı](docs/ER_DIAGRAM.md) — müşteri–kredi–taksit–ödeme ilişkileri
> - [API Endpoint Listesi](docs/API_ENDPOINTS.md) — tüm uç noktalar, roller, örnek yanıtlar
> - [Kredi Oluşturma → Taksit Üretme Akışı](docs/LOAN_CREATION_FLOW.md) — sıralı/akış diyagramları
> - [API Entegrasyon Dokümanı](API_INTEGRATION_DOCUMENTATION.md) — frontend↔backend kontrat detayı

---

## İçindekiler

- [Özellikler](#özellikler)
- [Mimari](#mimari)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Klasör Yapısı](#klasör-yapısı)
- [Ön Koşullar](#ön-koşullar)
- [Kurulum](#kurulum)
  - [1. Repoyu Klonla](#1-repoyu-klonla)
  - [2. Backend (.NET 8 API)](#2-backend-net-8-api)
  - [3. Frontend (React + Vite)](#3-frontend-react--vite)
- [Çalışma Akışı (Hızlı Başlangıç)](#çalışma-akışı-hızlı-başlangıç)
- [Varsayılan Hesaplar](#varsayılan-hesaplar)
- [Kredi Hesaplama Formülü](#kredi-hesaplama-formülü)
- [Dış Servis Mock'ları](#dış-servis-mockları)
- [Testler](#testler)
- [Sorun Giderme](#sorun-giderme)

---

## Özellikler

- **Kimlik doğrulama:** JWT tabanlı login/register; rol bazlı yetkilendirme (`Admin`, `Customer`).
- **Müşteri yönetimi:** Admin için tam CRUD, müşteri için kendi profilini görüntüleme/güncelleme; soft-delete + restore.
- **Kredi başvurusu:** İhtiyaç / Eğitim / Taşıt kredisi; kredi skoru kontrolü (mock Findeks), otomatik taksit planı.
- **Taksit yönetimi:** Otomatik taksitlendirme, vade tarihleri, gecikmiş taksit toplu güncelleme (`update-overdue`).
- **Ödeme:** Sıralı taksit ödeme, idempotency-key destekli mock ödeme ağ geçidi (Stripe Sandbox uyumlu decline kodları).
- **Tüm taksitler bitince** kredi otomatik `Closed` durumuna geçer.
- **Soft-delete:** Aktif kredisi olan müşteri silinemez; arşivlenen müşteri "Aktife Çevir" ile geri getirilebilir.
- **Swagger UI:** Geliştirme modunda `http://localhost:5213/swagger`.

---

## Mimari

Backend, n-katmanlı (Onion benzeri) bir yapı izler:

```
┌──────────────────────────────────────────────────────────┐
│  LoanManagement.API           (Controllers, Middleware)  │
│      ▲                                                   │
│      │ DI                                                │
│  LoanManagement.Business      (Services, ExternalSvc)    │
│      ▲                                                   │
│      │                                                   │
│  LoanManagement.DataAccess    (EF Core, DbContext)       │
│      ▲                                                   │
│      │                                                   │
│  LoanManagement.Entities      (Models, DTOs, Enums)      │
└──────────────────────────────────────────────────────────┘
                  ▲
                  │ HTTP / JSON (Bearer JWT)
                  │
        ┌─────────┴─────────┐
        │  Frontend (React) │
        └───────────────────┘
```

- **External services** (`ExternalCreditScoreService`, `ExternalPaymentGatewayService`) `HttpClientFactory` + Polly retry politikaları ile çağrılır; geliştirme ortamında `*SandboxDelegatingHandler` üzerinden cevaplar mock'lanır.
- **GlobalExceptionHandlerMiddleware** tüm yakalanmamış hataları tek tip JSON zarfına çevirir.

---

## Teknoloji Yığını

### Backend
- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core 8 (SQL Server provider)
- JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Polly (`Microsoft.Extensions.Http.Polly`) — retry / circuit breaker
- Swashbuckle / Swagger
- xUnit + Moq (test projesi)

### Frontend
- React 18 + TypeScript
- Vite 5 (dev / build)
- Material UI v6 + `@mui/x-data-grid`, `@mui/x-date-pickers`
- React Router v6
- React Hook Form + Zod
- Axios (Bearer interceptor)
- react-toastify

### Altyapı
- SQL Server (LocalDB veya tam sürüm)
- EF Core Migrations

---

## Klasör Yapısı

```
LoanManagementSystem/
├── backend/
│   ├── LoanManagement.API/              # Web API (Controllers, Program.cs, appsettings)
│   ├── LoanManagement.Business/         # İş kuralları (Services), dış servis istemcileri
│   ├── LoanManagement.DataAccess/       # LoanDbContext, EF Core Migrations
│   ├── LoanManagement.Entities/         # Models, DTOs, Enums, Validation attributes
│   ├── LoanManagement.Tests/            # xUnit testleri (Controllers, Fixtures, TestData)
│   └── LoanManagement.slnx
├── frontend/
│   ├── src/
│   │   ├── api/                         # axios instance + interceptors
│   │   ├── components/                  # layout, ProtectedRoute, ConfirmDialog…
│   │   ├── context/                     # AuthContext
│   │   ├── hooks/                       # useAuth, vb.
│   │   ├── pages/                       # Login, Register, Dashboard, Customers, Loans, …
│   │   ├── services/                    # authService, customerService, loanService…
│   │   ├── types/                       # DTO tip tanımları
│   │   └── utils/                       # formatters, zod schemas
│   ├── index.html
│   ├── package.json
│   └── vite.config.ts
├── docs/
│   ├── ER_DIAGRAM.md
│   ├── API_ENDPOINTS.md
│   └── LOAN_CREATION_FLOW.md
├── API_INTEGRATION_DOCUMENTATION.md
└── README.md   ← (bu dosya)
```

---

## Ön Koşullar

| Bileşen | Sürüm |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **8.0** veya üzeri |
| [Node.js](https://nodejs.org/) | **18.x** veya **20.x** |
| npm | 9+ (Node ile birlikte gelir) |
| SQL Server | LocalDB / Express / Developer |
| `dotnet-ef` aracı | `dotnet tool install --global dotnet-ef` |

---

## Kurulum

### 1. Repoyu Klonla

```bash
git clone <repo-url> LoanManagementSystem
cd LoanManagementSystem
```

### 2. Backend (.NET 8 API)

#### 2.1. Bağlantı dizesini ayarla

`backend/LoanManagement.API/appsettings.json` içindeki `DefaultConnection`'ı kendi SQL Server kurulumunuza göre düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LoanDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

> LocalDB için: `Server=(localdb)\\MSSQLLocalDB;Database=LoanDb;Trusted_Connection=True;`

#### 2.2. Bağımlılıkları yükle

```bash
cd backend
dotnet restore
```

#### 2.3. Veritabanını oluştur (EF Core Migrations)

```bash
dotnet ef database update --project LoanManagement.DataAccess --startup-project LoanManagement.API
```

#### 2.4. API'yi çalıştır

```bash
cd LoanManagement.API
dotnet run
```

Varsayılan olarak:
- HTTP: `http://localhost:5213`
- HTTPS: `https://localhost:7135`
- Swagger UI: `http://localhost:5213/swagger`

İlk açılışta `AdminUserSeedService` veritabanında admin kullanıcı yoksa otomatik bir tane oluşturur.

### 3. Frontend (React + Vite)

```bash
cd frontend
npm install
```

`.env` dosyasını oluştur (`.env.example` üzerinden):

```env
VITE_API_BASE_URL=http://localhost:5213
```

Geliştirme sunucusunu başlat:

```bash
npm run dev
# → http://localhost:5173
```

Üretim derlemesi:

```bash
npm run build
npm run preview
```

---

## Çalışma Akışı (Hızlı Başlangıç)

1. Backend'i çalıştır (`dotnet run` — port `5213`).
2. Frontend'i çalıştır (`npm run dev` — port `5173`).
3. Tarayıcıdan `http://localhost:5173` adresine git.
4. **Admin** olarak giriş yap (`admin / Admin123!`) ya da **Customer** kaydı oluştur (`/register`).
5. Müşteri panelinden:
   - **Yeni Kredi Başvurusu** → Tutar, vade, kar oranı (%), kredi türü seç.
   - Sistem önce mock Findeks'ten skor sorgular (≥ 600 olmalı), ardından taksit planını otomatik üretir.
   - **Taksitlerim** ekranından sıradaki taksiti öde.
6. Admin panelinden tüm müşteri/kredi/ödemeleri görüntüle, gecikmiş taksitleri toplu güncelle.

---

## Varsayılan Hesaplar

Sistem ilk açılışta `appsettings.json > Seed` bölümüne göre admin kullanıcısı oluşturur:

| Rol | Kullanıcı Adı | Şifre |
|---|---|---|
| Admin | `admin` | `Admin123!` |

> `Seed:AdminUsername`, `Seed:AdminPassword`, `Seed:AdminFullName` anahtarlarını `appsettings.json`'a ekleyerek özelleştirebilirsiniz.

Müşteri hesabı, `/api/Auth/register` veya frontend'deki **Kayıt Ol** ekranı üzerinden oluşturulur. Kayıt sırasında hem `Users` hem `Customers` tablosuna birlikte yazılır ve JWT içine `customer_id` claim'i konur.

---

## Kredi Hesaplama Formülü

```
ToplamKar       = AnaPara × (YıllıkKarYüzdesi / 100) × (VadeAy / 12)
ToplamGeriÖdeme = AnaPara + ToplamKar
AylıkTaksit     = ToplamGeriÖdeme / VadeAy        (yuvarlanır, 2 ondalık)
SonTaksit       = ToplamGeriÖdeme − AylıkTaksit × (VadeAy − 1)   (kuruş farkı düzeltme)
```

Örnek: 60.000 ₺ ana para, 12 ay vade, yıllık %24 kar oranı
- ToplamKar = 60.000 × 0,24 × (12/12) = 14.400 ₺
- ToplamGeriÖdeme = 74.400 ₺
- AylıkTaksit = 6.200 ₺
- 12 adet taksit, vade tarihleri `StartDate + n ay`.

Detaylar: [docs/LOAN_CREATION_FLOW.md](docs/LOAN_CREATION_FLOW.md)

---

## Dış Servis Mock'ları

`appsettings.json > ExternalServices` bölümü iki dış sağlayıcıyı yapılandırır:

| Sağlayıcı | Açıklama | UseSandboxHandler |
|---|---|---|
| **PaymentGateway** (Stripe Sandbox) | `IExternalPaymentGatewayService` üzerinden taksit tahsilatı | `true` → `StripeSandboxDelegatingHandler` yanıtları mock'lar |
| **CreditBureau** (Findeks Mock) | `IExternalCreditScoreService` üzerinden skor sorgusu | `true` → `CreditBureauSandboxDelegatingHandler` yanıtları mock'lar |

Her ikisi de `HttpClientFactory` + Polly **exponential backoff retry** politikası ile çağrılır (varsayılan `RetryCount: 2`). Gerçek bir sağlayıcıya geçmek için ilgili `BaseUrl` / `ApiKey` değerlerini güncelleyin ve `UseSandboxHandler: false` yapın.

---

## Testler

Backend tarafında xUnit projesi mevcut:

```bash
cd backend/LoanManagement.Tests
dotnet test
```

Çalıştırılan test sınıfları (`Controllers/`):
- `CustomersControllerTests`
- `LoansControllerTests`

Test verileri `TestData/` altında, fixture'lar `Fixtures/` altındadır.

---

## Sorun Giderme

| Belirti | Olası Sebep / Çözüm |
|---|---|
| `dotnet ef` komutu bulunamıyor | `dotnet tool install --global dotnet-ef` çalıştırın, ardından yeni bir terminal açın. |
| API başlangıçta SQL bağlantı hatası | `appsettings.json > ConnectionStrings:DefaultConnection` dizesini kendi SQL Server kurulumunuza göre güncelleyin. |
| Frontend → API CORS hatası | `appsettings.json > Cors:AllowedOrigins` içine frontend URL'inizi ekleyin (örn. `http://localhost:5173`). |
| 401 Unauthorized — token boş | Frontend `.env` içindeki `VITE_API_BASE_URL` doğru mu? Login sonrası `localStorage.token` kontrol edin. |
| Kredi başvurusu "Yetersiz skor" | Mock bürodan dönen skor < 600. `ExternalCreditScoreService`/sandbox handler'da skor mantığını gözden geçirin. |
| Ödeme 402 dönüyor | Stripe sandbox handler `insufficient_funds`, `card_declined` gibi simüle decline cevapları üretebilir. Yeniden deneyin veya idempotency-key'i kontrol edin. |

---

## Lisans

İç eğitim / staj projesi. Tüm hakları saklıdır.
