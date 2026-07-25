import { UserRole } from '../models/user-role.model';
import { NavItem } from '../models/nav-item.model';

export const ROLE_NAV_ITEMS: Record<UserRole, NavItem[]> = {
  [UserRole.Student]: [
    { label: 'Dashboard', icon: 'bi-grid-1x2-fill', route: 'dashboard' },
    { label: 'Find a Tutor', icon: 'bi-search', route: 'find-tutor' },
    { label: 'My Bookings', icon: 'bi-calendar-check', route: 'bookings' },
    { label: 'Lesson History', icon: 'bi-clock-history', route: 'history' },
    { label: 'Messages', icon: 'bi-chat-dots', route: 'messages', badge: 2 },
    { label: 'Invoices & Billing', icon: 'bi-receipt', route: 'billing' },
    { label: 'Profile', icon: 'bi-person-circle', route: 'profile' },
  ],
  [UserRole.Parent]: [
    { label: 'Dashboard', icon: 'bi-grid-1x2-fill', route: 'dashboard' },
    { label: 'My Children', icon: 'bi-people', route: 'children' },
    { label: 'Bookings', icon: 'bi-calendar-check', route: 'bookings' },
    { label: 'Payments & Invoices', icon: 'bi-credit-card', route: 'billing' },
    { label: 'Messages', icon: 'bi-chat-dots', route: 'messages' },
    { label: 'Profile', icon: 'bi-person-circle', route: 'profile' },
  ],
  [UserRole.Tutor]: [
    { label: 'Dashboard', icon: 'bi-grid-1x2-fill', route: 'dashboard' },
    { label: 'Availability', icon: 'bi-calendar-week', route: 'availability' },
    { label: 'Lessons', icon: 'bi-easel', route: 'lessons' },
    { label: 'Lesson Notes', icon: 'bi-journal-text', route: 'notes' },
    { label: 'Earnings & Payouts', icon: 'bi-cash-coin', route: 'earnings' },
    { label: 'Feedback', icon: 'bi-star', route: 'feedback' },
    { label: 'Profile & Verification', icon: 'bi-patch-check', route: 'profile' },
  ],
  [UserRole.Admin]: [
    { label: 'Dashboard', icon: 'bi-grid-1x2-fill', route: 'dashboard' },
    { label: 'Tutors', icon: 'bi-mortarboard', route: 'tutors' },
    { label: 'Students & Parents', icon: 'bi-people', route: 'users' },
    { label: 'Subjects & Pricing', icon: 'bi-tags', route: 'pricing' },
    { label: 'Bookings & Payments', icon: 'bi-wallet2', route: 'payments' },
    { label: 'Disputes & Refunds', icon: 'bi-exclamation-triangle', route: 'disputes' },
    { label: 'Analytics & Reports', icon: 'bi-bar-chart', route: 'analytics' },
    { label: 'Settings', icon: 'bi-gear', route: 'settings' },
  ],
};
