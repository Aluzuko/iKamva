import { BookingStatus, InvoiceStatus, PaymentRedirect } from './parent.model';
import { AvailabilitySlot, Review } from './tutor.model';

export interface SubjectRef {
  subjectId: string;
  name: string;
}

export interface TutorCard {
  tutorId: string;
  fullName: string;
  headline: string | null;
  hourlyRate: number;
  yearsExperience: number;
  subjects: SubjectRef[];
  averageRating: number | null;
  reviewCount: number;
  completedLessons: number;
  weeklyHours: number;
}

export interface TutorPublicProfile {
  tutorId: string;
  fullName: string;
  bio: string | null;
  hourlyRate: number;
  yearsExperience: number;
  subjects: SubjectRef[];
  averageRating: number | null;
  reviewCount: number;
  completedLessons: number;
  availability: AvailabilitySlot[];
  reviews: Review[];
}

export interface SlotDay {
  date: string;
  starts: string[];
}

export interface Slots {
  timeZone: string;
  durationMinutes: number;
  days: SlotDay[];
}

export interface TutorSearch {
  subjectId?: string | null;
  maxRate?: number | null;
  minRating?: number | null;
  search?: string | null;
  sort?: string | null;
}

export interface CreateBookingPayload {
  tutorId: string;
  subjectId: string;
  startUtc: string;
  durationMinutes: number;
}

export interface BookingCreated {
  bookingId: string;
  invoiceId: string;
  scheduledAt: string;
  price: number;
  paymentRequired: boolean;
  paymentRedirect: PaymentRedirect | null;
  paymentError: string | null;
}

export interface StudentLesson {
  bookingId: string;
  tutorId: string;
  tutorName: string;
  subjectName: string;
  scheduledAt: string;
  durationMinutes: number;
  price: number;
  status: BookingStatus;
  notes: string | null;
  recordingUrl: string | null;
  cancellationReason: string | null;
  canCancel: boolean;
  canReview: boolean;
  myRating: number | null;
  myComment: string | null;
}

export interface LinkedParent {
  fullName: string;
  relationship: 'parent' | 'guardian';
}

export interface SubjectProgress {
  subjectName: string;
  completedLessons: number;
  hours: number;
  lastLessonAt: string | null;
}

export interface StudentDashboard {
  nextLesson: StudentLesson | null;
  upcomingLessons: StudentLesson[];
  upcomingCount: number;
  completedCount: number;
  hoursThisMonth: number;
  lessonsToReview: number;
  subjects: SubjectProgress[];
  linkCode: string;
  parents: LinkedParent[];
}

export interface StudentInvoice {
  invoiceId: string;
  periodDescription: string;
  amount: number;
  status: InvoiceStatus;
  paidAt: string | null;
}

export interface Charge {
  bookingId: string;
  scheduledAt: string;
  tutorName: string;
  subjectName: string;
  durationMinutes: number;
  price: number;
  status: BookingStatus;
}

export interface StudentBilling {
  paidByParent: boolean;
  parents: LinkedParent[];
  linkCode: string;
  outstanding: number;
  thisMonthTotal: number;
  upcomingTotal: number;
  thisMonth: Charge[];
  invoices: StudentInvoice[];
}

export const LESSON_DURATIONS = [30, 60, 90, 120];
export const SAST = '+0200';
