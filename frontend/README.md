# Loan Management — Frontend

React + TypeScript + Vite + MUI tabanlı, .NET 8 Loan Management API'sine entegre uçtan uca ön yüz.

## Stack

- **React 18 + TypeScript**
- **Vite** (dev / build)
- **Material UI v6** (+ `@mui/x-data-grid`)
- **React Router v6**
- **React Hook Form + Zod** (form validasyonu)
- **Axios** (Bearer token interceptor)
- **react-toastify** (bildirim)

## Klasör Yapısı

```
src/
  api/              axios instance + interceptors (Bearer + 401 handler)
  components/       layout, shared UI, ProtectedRoute, ConfirmDialog
  context/          AuthContext
  hooks/            useAuth, useForm helpers
  pages/            Login, Register, Dashboard, Customers, Loans, Installments, Payments, ...
  services/         endpoint başına servis dosyaları (authService.ts, customerService.ts, ...)
  types/            DTO tip tanımları (API_INTEGRATION_DOCUMENTATION.md ile birebir)
  utils/            formatters (currency, date, enum), zod schemas
```

## Kullanım

```bash
# Bağımlılıkları kur
npm install

# Geliştirme sunucusu
npm run dev
# → http://localhost:5173

# Type-check
npm run type-check

# Üretim build
npm run build
```

## Backend Bağlantısı

`.env` içinde:
```
VITE_API_BASE_URL=http://localhost:5213
```

Backend Swagger UI: `http://localhost:5213/swagger`

### Varsayılan Admin

```
username: admin
password: Admin123!
```

## Roller

- **Admin**: Tüm müşterileri / kredileri / ödemeleri görür, müşteri CRUD, gecikmiş taksitleri toplu güncelleme.
- **Customer**: Kendi profili, kredi başvurusu, taksit görüntüleme, ödeme.
