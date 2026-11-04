import { BadgeVariant } from '../components/status-badge/status-badge';

export interface StatusView {
  text: string;
  variant: BadgeVariant;
}

const VIEWS: Record<string, StatusView> = {
  upcoming: { text: 'Upcoming', variant: 'info' },
  completed: { text: 'Completed', variant: 'success' },
  cancelled: { text: 'Cancelled', variant: 'danger' },
  pending: { text: 'Pending', variant: 'warning' },
  paid: { text: 'Paid', variant: 'success' },
  approved: { text: 'Approved', variant: 'success' },
  rejected: { text: 'Rejected', variant: 'danger' },
  open: { text: 'Open', variant: 'warning' },
  resolved: { text: 'Resolved', variant: 'success' },
  sent: { text: 'Sent', variant: 'success' },
  failed: { text: 'Failed', variant: 'danger' },
  sending: { text: 'Sending', variant: 'info' },
};

export function statusView(status: string): StatusView {
  return VIEWS[status] ?? { text: status.charAt(0).toUpperCase() + status.slice(1), variant: 'neutral' };
}

export function accountView(isActive: boolean): StatusView {
  return isActive ? { text: 'Active', variant: 'success' } : { text: 'Suspended', variant: 'neutral' };
}

export function matchesSearch(query: string, ...fields: (string | null | undefined)[]): boolean {
  const q = query.trim().toLowerCase();
  return !q || fields.some((f) => (f ?? '').toLowerCase().includes(q));
}
