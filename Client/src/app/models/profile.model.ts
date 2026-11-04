import { UserRole } from './user-role.model';
import { VerificationStatus } from './admin.model';

export interface Profile {
  userId: string;
  email: string;
  isEmailVerified: boolean;
  fullName: string;
  role: UserRole;
  createdAt: string;
  yearLevel: string | null;
  linkCode: string | null;
  enrolledSubjects: string[] | null;
  bio: string | null;
  hourlyRate: number | null;
  yearsExperience: number | null;
  verificationStatus: VerificationStatus | null;
  tutorSubjects: string[] | null;
}

export interface UpdatePersonalInfoPayload {
  fullName: string;
}

export interface UpdateRoleInfoPayload {
  fullName: string;
  bio?: string;
  hourlyRate?: number;
  yearLevel?: string;
}
