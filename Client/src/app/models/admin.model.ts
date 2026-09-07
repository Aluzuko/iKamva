import { BookingStatus, InvoiceStatus } from './parent.model';

export type VerificationStatus = 'pending' | 'approved' | 'rejected';
export type DisputeStatus = 'open' | 'resolved' | 'rejected';
export type DocumentType = 'idDocument' | 'qualification' | 'backgroundCheck';

export interface RecentBooking {
  bookingId: string;
  subjectName: string;
  studentName: string;
  tutorName: string;
  scheduledAt: string;
  price: number;
  status: BookingStatus;
}

export interface AdminDashboard {
  totalRevenue: number;
  revenueChangePercent: number;
  totalBookings: number;
  bookingsChangePercent: number;
  activeTutors: number;
  activeStudents: number;
  upcomingBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  recentBookings: RecentBooking[];
}

export interface VerificationDocument {
  documentId: string;
  documentType: DocumentType;
  fileName: string;
  fileUrl: string;
  uploadedAt: string;
}

export interface TutorListItem {
  tutorId: string;
  fullName: string;
  email: string;
  hourlyRate: number;
  yearsExperience: number;
  verificationStatus: VerificationStatus;
  isActive: boolean;
  subjects: string[];
  completedBookings: number;
}

export interface TutorDetail {
  tutorId: string;
  fullName: string;
  email: string;
  bio: string | null;
  hourlyRate: number;
  yearsExperience: number;
  verificationStatus: VerificationStatus;
  rejectionReason: string | null;
  subjects: string[];
  documents: VerificationDocument[];
}

export interface CreateTutorPayload {
  email: string;
  fullName: string;
  password: string;
  hourlyRate: number;
  yearsExperience: number;
  bio?: string;
  subjectIds: string[];
}

export type ManagedUserRole = 'student' | 'parent';

export interface UserAccount {
  userId: string;
  fullName: string;
  email: string;
  role: ManagedUserRole;
  isActive: boolean;
  createdAt: string;
}

export interface SubjectPricing {
  subjectId: string;
  name: string;
  defaultHourlyRate: number;
  commissionPercent: number;
  tutorCount: number;
}

export interface AdminBooking {
  bookingId: string;
  subjectName: string;
  studentName: string;
  tutorName: string;
  scheduledAt: string;
  price: number;
  status: BookingStatus;
}

export interface AdminInvoice {
  invoiceId: string;
  parentName: string;
  studentName: string;
  periodDescription: string;
  amount: number;
  status: InvoiceStatus;
}

export interface Dispute {
  disputeId: string;
  raisedByName: string;
  subject: string;
  description: string;
  status: DisputeStatus;
  resolution: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface MonthlyPoint {
  label: string;
  value: number;
}

export interface Analytics {
  revenueByMonth: MonthlyPoint[];
  bookingsByMonth: MonthlyPoint[];
  cancellationsByMonth: MonthlyPoint[];
}
