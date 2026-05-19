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
      id: Number(payload.sub || payload.nameidentifier || 0),
      email: email || payload.email || '',
      fullName: payload.unique_name || payload.name || '',
      role:
        (role || payload.role) === 'Admin'
          ? ('Admin' as const)
          : ('User' as const),
      isEmailVerified: true,
      createdAt: '',
    };
  } catch {
    return null;
  }
}
