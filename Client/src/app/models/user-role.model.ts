export enum UserRole {
  Student = 'student',
  Parent = 'parent',
  Tutor = 'tutor',
  Admin = 'admin',
}

export interface RoleOption {
  role: UserRole;
  label: string;
  description: string;
  icon: string;
}

export const ROLE_OPTIONS: RoleOption[] = [
  {
    role: UserRole.Student,
    label: 'Student',
    description: 'Book and attend lessons',
    icon: 'bi-book',
  },
  {
    role: UserRole.Parent,
    label: 'Parent',
    description: 'Follow and pay for your children',
    icon: 'bi-people',
  },
  {
    role: UserRole.Tutor,
    label: 'Tutor',
    description: 'Teach after verification',
    icon: 'bi-mortarboard',
  },
  {
    role: UserRole.Admin,
    label: 'Admin',
    description: 'Run the platform',
    icon: 'bi-shield-check',
  },
];

export const SIGNUP_ROLES: RoleOption[] = ROLE_OPTIONS.filter((r) => r.role !== UserRole.Admin);

export function roleLabel(role: UserRole | null | undefined): string {
  return ROLE_OPTIONS.find((r) => r.role === role)?.label ?? '';
}
