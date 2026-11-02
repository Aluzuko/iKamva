import { DocumentType } from './admin.model';
import { BookingStatus } from './parent.model';
import { MonthlyPoint } from './admin.model';

export type VerificationState = 'notSubmitted' | 'inReview' | 'approved' | 'rejected';
export type PayoutStatus = 'requested' | 'paid';

export interface SubjectOption {
  subjectId: string;
  name: string;
  defaultHourlyRate: number;
}

export interface TutorDocument {
  documentId: string;
  documentType: DocumentType;
  fileName: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface TutorProfile {
  fullName: string;
  email: string;
  bio: string | null;
  hourlyRate: number | null;
  yearsExperience: number;
  subjectIds: string[];
  verificationState: VerificationState;
  rejectionReason: string | null;
  submittedAt: string | null;
  canEditDocuments: boolean;
  documents: TutorDocument[];
  missingForSubmission: string[];
}

export interface UpdateTutorProfilePayload {
  bio: string | null;
  hourlyRate: number;
  yearsExperience: number;
  subjectIds: string[];
}

export interface AvailabilitySlot {
  dayOfWeek: number;
  startMinute: number;
  endMinute: number;
}

export interface Availability {
  timeZone: string;
  slots: AvailabilitySlot[];
}

export interface TutorLesson {
  bookingId: string;
  studentName: string;
  subjectName: string;
  scheduledAt: string;
  durationMinutes: number;
  price: number;
  earning: number;
  status: BookingStatus;
  notes: string | null;
  recordingUrl: string | null;
  cancellationReason: string | null;
  needsCompletion: boolean;
  rating: number | null;
}

export interface TutorDashboard {
  verificationState: VerificationState;
  rejectionReason: string | null;
  todayLessons: TutorLesson[];
  nextLesson: TutorLesson | null;
  upcomingThisWeek: number;
  completedThisMonth: number;
  earningsThisMonth: number;
  earningsChangePercent: number;
  averageRating: number | null;
  reviewCount: number;
  lessonsNeedingNotes: number;
  lessonsNeedingCompletion: number;
  hasAvailability: boolean;
}

export interface Review {
  reviewId: string;
  studentName: string;
  subjectName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  lessonDate: string;
}

export interface Feedback {
  averageRating: number | null;
  reviewCount: number;
  distribution: number[];
  reviews: Review[];
}

export interface EarningLine {
  bookingId: string;
  scheduledAt: string;
  studentName: string;
  subjectName: string;
  price: number;
  commissionPercent: number;
  earning: number;
}

export interface PayoutRecord {
  payoutId: string;
  amount: number;
  status: PayoutStatus;
  reference: string | null;
  requestedAt: string;
  paidAt: string | null;
}

export interface Earnings {
  totalEarned: number;
  availableBalance: number;
  pendingPayouts: number;
  paidOut: number;
  thisMonth: number;
  minimumPayout: number;
  canRequestPayout: boolean;
  payoutBlockedReason: string | null;
  monthly: MonthlyPoint[];
  recentLessons: EarningLine[];
  payouts: PayoutRecord[];
}

export const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

export const MAX_DOCUMENT_BYTES = 5 * 1024 * 1024;

export function minutesToTime(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
}

export function timeToMinutes(value: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }
  const h = Number(match[1]);
  const m = Number(match[2]);
  if (h > 24 || m > 59 || (h === 24 && m > 0)) {
    return null;
  }
  return h * 60 + m;
}

export function verificationLabel(state: VerificationState): { text: string; variant: 'success' | 'warning' | 'danger' | 'neutral' } {
  switch (state) {
    case 'approved':
      return { text: 'Verified', variant: 'success' };
    case 'inReview':
      return { text: 'In review', variant: 'warning' };
    case 'rejected':
      return { text: 'Changes needed', variant: 'danger' };
    default:
      return { text: 'Not submitted', variant: 'neutral' };
  }
}
