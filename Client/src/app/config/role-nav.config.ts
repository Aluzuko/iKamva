import { UserRole } from '../models/user-role.model';
import { NavItem } from '../models/nav-item.model';

export const ROLE_NAV_ITEMS: Record<UserRole, NavItem[]> = {
  [UserRole.Student]: [
    { label: 'Dashboard', icon: 'bi-speedometer2', route: 'dashboard' },
    { label: 'Find a Tutor', short: 'Find', icon: 'bi-search', route: 'find-tutor' },
    { label: 'My Lessons', short: 'Lessons', icon: 'bi-book', route: 'lessons' },
    { label: 'Messages', icon: 'bi-chat-left', route: 'messages' },
    { label: 'Billing', icon: 'bi-credit-card', route: 'billing' },
  ],
  [UserRole.Parent]: [
    { label: 'Dashboard', icon: 'bi-speedometer2', route: 'dashboard' },
    { label: 'My Children', icon: 'bi-people', route: 'children' },
    { label: 'Payments', icon: 'bi-credit-card', route: 'payments' },
    { label: 'Messages', icon: 'bi-chat-left', route: 'messages' },
    { label: 'Link Student', short: 'Link', icon: 'bi-link-45deg', route: 'link-student' },
  ],
  [UserRole.Tutor]: [
    { label: 'Dashboard', icon: 'bi-speedometer2', route: 'dashboard' },
    { label: 'Availability', icon: 'bi-calendar-week', route: 'availability' },
    { label: 'Lessons', icon: 'bi-easel', route: 'lessons' },
    { label: 'Messages', icon: 'bi-chat-left', route: 'messages' },
    { label: 'Lesson Notes', short: 'Notes', icon: 'bi-journal-text', route: 'notes' },
    { label: 'Earnings & Payouts', icon: 'bi-wallet2', route: 'earnings' },
    { label: 'Feedback', icon: 'bi-star', route: 'feedback' },
    { label: 'Profile & Verification', short: 'Profile', icon: 'bi-patch-check', route: 'profile' },
  ],
  [UserRole.Admin]: [
    { label: 'Overview', icon: 'bi-speedometer2', route: 'dashboard' },
    { label: 'Tutors', icon: 'bi-mortarboard', route: 'tutors' },
    { label: 'Students & Parents', icon: 'bi-people', route: 'users' },
    { label: 'Subjects & Pricing', icon: 'bi-tags', route: 'pricing' },
    { label: 'Bookings & Payments', icon: 'bi-calendar-check', route: 'payments' },
    { label: 'Disputes & Refunds', icon: 'bi-exclamation-triangle', route: 'disputes' },
    { label: 'Analytics & Reports', icon: 'bi-bar-chart', route: 'analytics' },
    { label: 'Email queue', short: 'Email', icon: 'bi-envelope', route: 'emails' },
  ],
};

export function shortNavLabel(item: NavItem | string): string {
  return typeof item === 'string' ? item.split(' & ')[0] : item.short ?? item.label.split(' & ')[0];
}
