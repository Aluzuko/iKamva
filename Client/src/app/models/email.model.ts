export type EmailStatus = 'pending' | 'sending' | 'sent' | 'failed' | 'cancelled';
export type EmailPriority = 'high' | 'normal' | 'low';
export type EmailCategory =
  | 'general'
  | 'welcome'
  | 'accountVerification'
  | 'passwordReset'
  | 'studentInvite'
  | 'invoice'
  | 'paymentConfirmation'
  | 'letter'
  | 'tutorVerification';

export const EMAIL_CATEGORY_LABELS: Record<EmailCategory, string> = {
  general: 'General',
  welcome: 'Welcome',
  accountVerification: 'Account verification',
  passwordReset: 'Password reset',
  studentInvite: 'Student invite',
  invoice: 'Invoice',
  paymentConfirmation: 'Payment confirmation',
  letter: 'Letter',
  tutorVerification: 'Tutor verification',
};

export interface EmailListItem {
  emailId: string;
  category: EmailCategory;
  status: EmailStatus;
  priority: EmailPriority;
  toEmail: string;
  toName: string | null;
  subject: string;
  attempts: number;
  maxAttempts: number;
  attachmentCount: number;
  createdAt: string;
  nextAttemptAt: string;
  sentAt: string | null;
  lastError: string | null;
}

export interface EmailAttachmentInfo {
  attachmentId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isInline: boolean;
}

export interface EmailDetail {
  summary: EmailListItem;
  ccEmails: string | null;
  bccEmails: string | null;
  replyToEmail: string | null;
  htmlBody: string;
  textBody: string | null;
  isSensitive: boolean;
  bodyPurged: boolean;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  lastAttemptAt: string | null;
  providerMessageId: string | null;
  attachments: EmailAttachmentInfo[];
}

export interface EmailPage {
  items: EmailListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface EmailStats {
  pending: number;
  due: number;
  sending: number;
  sent: number;
  failed: number;
  cancelled: number;
  oldestDueAt: string | null;
  workerEnabled: boolean;
  transport: string;
}

export interface EmailProcessResult {
  claimed: number;
  sent: number;
  retrying: number;
  failed: number;
}

export const MAX_EMAIL_ATTACHMENT_BYTES = 10 * 1024 * 1024;
export const MAX_EMAIL_ATTACHMENTS = 10;
export const BLOCKED_ATTACHMENT_EXTENSIONS = ['.exe', '.bat', '.cmd', '.com', '.scr', '.js', '.jse', '.vbs', '.vbe', '.ps1', '.msi', '.jar', '.dll', '.lnk', '.hta'];
