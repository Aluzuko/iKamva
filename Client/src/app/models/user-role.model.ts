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
  gradientClass: string;
}

export const ROLE_OPTIONS: RoleOption[] = [
  {
    role: UserRole.Student,
    label: 'Student',
    description: 'Browse tutors, book sessions, view recordings',
    icon: 'bi-book-half',
    gradientClass: 'role-gradient-student',
  },
  {
    role: UserRole.Parent,
    label: 'Parent',
    description: 'Monitor children, manage payments, link accounts',
    icon: 'bi-people-fill',
    gradientClass: 'role-gradient-parent',
  },
  {
    role: UserRole.Tutor,
    label: 'Tutor',
    description: 'Manage schedule, track earnings, view feedback',
    icon: 'bi-mortarboard-fill',
    gradientClass: 'role-gradient-tutor',
  },
  {
    role: UserRole.Admin,
    label: 'Admin',
    description: 'Oversee platform, verify tutors, resolve disputes',
    icon: 'bi-gear-fill',
    gradientClass: 'role-gradient-admin',
  },
];
