const NAMEID_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const EMAIL_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
const NAME_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const ROLE_KEY = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export function jwtDecode(
  token: string,
  email?: string,
  role?: string
): {
  id: number;
  email: string;
  fullName: string;
  role: 'User' | 'Admin';
  isEmailVerified: boolean;
  createdAt: string;
} | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''));
    return {
      id: Number(payload[NAMEID_KEY] ?? payload.sub ?? payload.nameidentifier ?? 0),
      email: email || payload[EMAIL_KEY] || payload.email || '',
      fullName: payload[NAME_KEY] || payload.unique_name || payload.name || '',
      role:
        (role || payload[ROLE_KEY] || payload.role) === 'Admin'
          ? ('Admin' as const)
          : ('User' as const),
      isEmailVerified: true,
      createdAt: '',
    };
  } catch {
    return null;
  }
}
