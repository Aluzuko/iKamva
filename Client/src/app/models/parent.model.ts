export type BookingStatus = 'upcoming' | 'completed' | 'cancelled';
export type InvoiceStatus = 'pending' | 'paid';

export interface ChildSummary {
  studentId: string;
  fullName: string;
  yearLevel: string | null;
  upcomingCount: number;
  completedCount: number;
  subjects: string[];
}

export interface UpcomingLesson {
  bookingId: string;
  subjectName: string;
  studentName: string;
  tutorName: string;
  scheduledAt: string;
  price: number;
}

export interface InvoiceSummary {
  invoiceId: string;
  studentName: string;
  periodDescription: string;
  amount: number;
  status: InvoiceStatus;
}

export interface FamilyDashboard {
  childrenLinked: number;
  upcomingLessonsThisWeek: number;
  monthlySpend: number;
  monthlySpendChangePercent: number;
  totalSessions: number;
  totalSessionsChangePercent: number;
  children: ChildSummary[];
  upcomingLessons: UpcomingLesson[];
  outstandingInvoice: InvoiceSummary | null;
}

export interface LessonHistoryItem {
  bookingId: string;
  subjectName: string;
  tutorName: string;
  scheduledAt: string;
  durationMinutes: number;
  status: BookingStatus;
  notes: string | null;
  recordingUrl: string | null;
}

export interface StudentDetail {
  studentId: string;
  fullName: string;
  yearLevel: string | null;
  enrolledSubjects: string[];
  isActive: boolean;
  upcomingCount: number;
  completedCount: number;
  avgRating: number | null;
  upcomingLessons: UpcomingLesson[];
  lessonHistory: LessonHistoryItem[];
}

export interface ChildPaymentSummary {
  studentId: string;
  studentName: string;
  thisMonthAmount: number;
  status: InvoiceStatus;
  pendingInvoiceId: string | null;
}

export interface PaymentsOverview {
  children: ChildPaymentSummary[];
  history: InvoiceSummary[];
}

export type LinkRelationship = 'parent' | 'guardian';
