export interface AuthState {
  token: string | null;
  role: string | null;
}

const TOKEN_KEY = 'admin_token';
const ROLE_KEY = 'admin_role';

export function getAuth(): AuthState {
  return {
    token: sessionStorage.getItem(TOKEN_KEY),
    role: sessionStorage.getItem(ROLE_KEY),
  };
}

export function setAuth(token: string, role: string) {
  sessionStorage.setItem(TOKEN_KEY, token);
  sessionStorage.setItem(ROLE_KEY, role);
}

export function clearAuth() {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(ROLE_KEY);
}

export function isAdminRole(role: string | null): boolean {
  return role === 'Admin' || role === 'SuperAdmin';
}

function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload));
  } catch {
    return null;
  }
}

export function extractRoleFromToken(token: string): string | null {
  const payload = parseJwtPayload(token);
  if (!payload) return null;
  // ASP.NET Core puts the role in the standard claim type URI
  const roleKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  return (payload[roleKey] as string) ?? (payload['role'] as string) ?? null;
}
