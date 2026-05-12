import { apiClient } from "@/api/axiosClient";
import type {
  CreateCustomerDto,
  CreateCustomerResponse,
  CustomerListDto,
  CustomerResponseDto,
  CustomerSummaryDto,
  MessageResponse,
  UpdateCustomerDto,
} from "@/types";

const BASE = "/api/Customers";

export const customerService = {
  /**
   * Müşteri listesini getirir.
   *
   * @param includeDeleted `true` ise soft-delete edilmiş müşteriler de döner.
   *   Admin "Müşteri Yönetimi" ekranı bunu her zaman `true` göndererek aktif +
   *   pasif tüm kayıtları listeler ve "Aktife Çevir" akışını destekler. Default
   *   `false` geriye-uyumluluk için korunur.
   *
   * Backend kontratı: `GET /api/Customers?includeDeleted={bool}` (v1.4+).
   */
  async list(includeDeleted = false): Promise<CustomerListDto[]> {
    const { data } = await apiClient.get<CustomerListDto[]>(BASE, {
      params: { includeDeleted },
    });
    return data;
  },

  async getById(id: number): Promise<CustomerResponseDto> {
    const { data } = await apiClient.get<CustomerResponseDto>(`${BASE}/${id}`);
    return data;
  },

  async create(payload: CreateCustomerDto): Promise<CreateCustomerResponse> {
    const { data } = await apiClient.post<CreateCustomerResponse>(BASE, payload);
    return data;
  },

  async update(id: number, payload: UpdateCustomerDto): Promise<MessageResponse> {
    const { data } = await apiClient.put<MessageResponse>(`${BASE}/${id}`, payload);
    return data;
  },

  async remove(id: number): Promise<MessageResponse> {
    const { data } = await apiClient.delete<MessageResponse>(`${BASE}/${id}`);
    return data;
  },

  /**
   * Soft-delete edilmiş müşteriyi yeniden aktifleştirir.
   *
   * Backend kontratı: `POST /api/Customers/{id}/restore` (v1.4+).
   * - 200: başarılı (message).
   * - 404: bulunamadı veya zaten aktif (axios interceptor `extractErrorMessage` ile yakalanır).
   */
  async restore(id: number): Promise<MessageResponse> {
    const { data } = await apiClient.post<MessageResponse>(`${BASE}/${id}/restore`);
    return data;
  },

  async getSummary(id: number): Promise<CustomerSummaryDto> {
    const { data } = await apiClient.get<CustomerSummaryDto>(`${BASE}/${id}/summary`);
    return data;
  },
};
