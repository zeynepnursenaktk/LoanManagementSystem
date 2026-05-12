/**
 * Backend doğrulama kuralları ile aynı kaynak. Form şemaları (Zod) burada
 * tanımlı yardımcıları kullanarak backend ile birebir uyumlu kalır.
 */

/**
 * Türkiye Cumhuriyeti Kimlik Numarası doğrulayıcı (NVI algoritması).
 * Kurallar:
 *  - 11 hane, yalnızca rakam.
 *  - İlk hane 0 olamaz.
 *  - d10 = ((d1+d3+d5+d7+d9) * 7 - (d2+d4+d6+d8)) mod 10
 *  - d11 = (d1+...+d10) mod 10
 */
export function isValidTurkishIdentityNumber(value: string | null | undefined): boolean {
  if (!value) return false;
  const s = value.trim();
  if (s.length !== 11) return false;
  if (s[0] === "0") return false;
  if (!/^[0-9]{11}$/.test(s)) return false;

  const d = Array.from(s, (ch) => Number.parseInt(ch, 10));
  const oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
  const evenSum = d[1] + d[3] + d[5] + d[7];
  let d10 = (oddSum * 7 - evenSum) % 10;
  if (d10 < 0) d10 += 10;
  if (d10 !== d[9]) return false;

  const totalSum = d.slice(0, 10).reduce((a, b) => a + b, 0);
  const d11 = totalSum % 10;
  return d11 === d[10];
}

/**
 * Türkiye GSM telefon numarası doğrulayıcı.
 * Kabul edilen formatlar (boşluk/tire/parantez/nokta tolere edilir):
 *   5xx xxx xx xx
 *   05xx xxx xx xx
 *   +90 5xx xxx xx xx
 * Yalnızca cep telefonu (5 ile başlayan operatör kodu).
 */
export function normalizeTurkishPhone(value: string): string {
  let out = "";
  for (const ch of value.trim()) {
    if (ch === "+" && out.length === 0) {
      out += "+";
    } else if (ch >= "0" && ch <= "9") {
      out += ch;
    }
  }
  return out;
}

const TR_PHONE_PATTERN = /^(\+90|90|0)?5[0-9]{9}$/u;

export function isValidTurkishPhone(value: string | null | undefined): boolean {
  if (value === null || value === undefined || value === "") return true; // opsiyonel kabul
  const normalized = normalizeTurkishPhone(value);
  if (normalized === "") return true;
  return TR_PHONE_PATTERN.test(normalized);
}

/**
 * Kullanıcı adı: 3-64 karakter; harf, rakam, nokta, alt çizgi, tire.
 */
export const USERNAME_PATTERN = /^[a-zA-Z0-9._-]{3,64}$/u;

/**
 * Güçlü şifre: 8-128 karakter, en az bir büyük harf, küçük harf ve rakam.
 */
export const STRONG_PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,128}$/u;

/**
 * Ad / Soyad: harf, boşluk, kesme işareti ve tire; ilk karakter harf olmalı.
 * Türkçe karakterler dahil tüm Unicode harfler kabul edilir.
 */
export const NAME_PATTERN = /^\p{L}[\p{L} '\-]{1,99}$/u;
