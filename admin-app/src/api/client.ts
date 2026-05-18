import axios from 'axios';
import { getAuth, clearAuth } from '../store/auth';

const api = axios.create({
  baseURL: '/api',
});

api.interceptors.request.use((config) => {
  const { token } = getAuth();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      clearAuth();
      window.location.href = '/admin/';
    }
    return Promise.reject(err);
  }
);

// ── Auth ──────────────────────────────────────────────────────────────────────
export interface LoginResponse {
  token: string;
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/auth/login', { username, password });
  return data;
}

// ── Oblasts ───────────────────────────────────────────────────────────────────
export interface OblastSummary {
  id: string;
  code: string;
  name: string;
  nameUk: string;
  isOccupied: boolean;
  hromadaCount: number;
  activeSourceCount: number;
}

export interface HromadaSummary {
  id: string;
  name: string;
  nameUk: string;
  slug: string;
  activeSourceCount: number;
  pendingSourceCount: number;
}

export interface OblastDetail {
  id: string;
  code: string;
  name: string;
  nameUk: string;
  isOccupied: boolean;
  rowVersion: number;
  hromadas: HromadaSummary[];
}

export async function getOblasts(): Promise<OblastSummary[]> {
  const { data } = await api.get<OblastSummary[]>('/oblasts');
  return data;
}

export async function getOblast(code: string): Promise<OblastDetail> {
  const { data } = await api.get<OblastDetail>(`/oblasts/${code}`);
  return data;
}

// ── Hromadas ──────────────────────────────────────────────────────────────────
export interface DataSource {
  id: string;
  scopeType: number;
  scopeId: string;
  url: string;
  description: string;
  status: number; // 0=Pending, 1=Active, 2=Rejected
  origin: number; // 0=Manual, 1=AiDiscovered
  createdAt: string;
  updatedAt: string | null;
  rowVersion: number;
}

export interface HromadaDetail {
  id: string;
  oblastId: string;
  oblastName: string;
  name: string;
  nameUk: string;
  slug: string;
  rowVersion: number;
  sources: DataSource[];
}

export async function getHromada(id: string): Promise<HromadaDetail> {
  const { data } = await api.get<HromadaDetail>(`/hromadas/${id}`);
  return data;
}

// ── Sources ───────────────────────────────────────────────────────────────────
export async function addSource(
  scopeId: string,
  url: string,
  description: string
): Promise<void> {
  await api.post('/sources', { scopeType: 1, scopeId, url, description });
}

export async function approveSource(id: string, rowVersion: number): Promise<void> {
  await api.post(`/sources/${id}/approve?rowVersion=${rowVersion}`);
}

export async function rejectSource(id: string, rowVersion: number): Promise<void> {
  await api.post(`/sources/${id}/reject?rowVersion=${rowVersion}`);
}

// ── Discovery ─────────────────────────────────────────────────────────────────
export interface DiscoveryRun {
  id: string;
  hromadaId: string;
  startedAt: string;
  completedAt: string | null;
  candidatesFound: number;
  candidatesInserted: number;
  error: string | null;
}

export async function runDiscovery(hromadaId: string): Promise<DiscoveryRun> {
  const { data } = await api.post<DiscoveryRun>(`/discovery/hromada/${hromadaId}`);
  return data;
}

export async function getDiscoveryRuns(hromadaId: string): Promise<DiscoveryRun[]> {
  const { data } = await api.get<DiscoveryRun[]>(`/discovery/runs?hromadaId=${hromadaId}`);
  return data;
}
