import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ROLE_NAV_ITEMS } from './config/role-nav.config';
import { UserRole } from './models/user-role.model';
import { authGuard, guestGuard, roleGuard } from './guards/auth.guard';
import { ComingSoon } from './components/coming-soon/coming-soon';
import { MainLayout } from './components/main-layout/main-layout';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { ForgotPassword } from './pages/forgot-password/forgot-password';

function placeholdersFor(role: UserRole, built: string[] = []): Routes {
  const routes: Routes = ROLE_NAV_ITEMS[role]
    .filter((item) => !built.includes(item.route))
    .map((item) => ({ path: item.route, component: ComingSoon, data: { title: item.label } }));

  if (!ROLE_NAV_ITEMS[role].some((item) => item.route === 'profile')) {
    routes.push({ path: 'profile', component: ComingSoon, data: { title: 'Profile' } });
  }

  return routes;
}

function roleArea(role: UserRole, built: Routes = []): Routes[number] {
  return {
    path: role,
    component: MainLayout,
    data: { role },
    canActivate: [authGuard, roleGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...built,
      ...placeholdersFor(
        role,
        built.map((r) => r.path ?? '')
      ),
    ],
  };
}

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'login/:role', redirectTo: 'login' },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'register/:role', component: Register, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'role-select', redirectTo: 'login' },
  roleArea(UserRole.Student, [
    { path: 'dashboard', loadComponent: () => import('./pages/student-dashboard/student-dashboard').then((m) => m.StudentDashboard) },
    { path: 'find-tutor', loadComponent: () => import('./pages/find-tutor/find-tutor').then((m) => m.FindTutor) },
    { path: 'tutors/:tutorId', loadComponent: () => import('./pages/student-tutor/student-tutor').then((m) => m.StudentTutor) },
    { path: 'lessons', loadComponent: () => import('./pages/student-lessons/student-lessons').then((m) => m.StudentLessons) },
    { path: 'payments/result', loadComponent: () => import('./pages/payment-result/payment-result').then((m) => m.PaymentResult) },
    { path: 'messages', loadComponent: () => import('./pages/messages/messages').then((m) => m.MessagesPage) },
    { path: 'billing', loadComponent: () => import('./pages/student-billing/student-billing').then((m) => m.StudentBilling) },
  ]),
  roleArea(UserRole.Parent, [
    { path: 'dashboard', loadComponent: () => import('./pages/parent-dashboard/parent-dashboard').then((m) => m.ParentDashboard) },
    { path: 'children', loadComponent: () => import('./pages/parent-children/parent-children').then((m) => m.ParentChildren) },
    { path: 'children/:studentId', loadComponent: () => import('./pages/student-detail/student-detail').then((m) => m.StudentDetailPage) },
    { path: 'payments', loadComponent: () => import('./pages/parent-payments/parent-payments').then((m) => m.ParentPayments) },
    { path: 'payments/result', loadComponent: () => import('./pages/payment-result/payment-result').then((m) => m.PaymentResult) },
    { path: 'link-student', loadComponent: () => import('./pages/link-student/link-student').then((m) => m.LinkStudent) },
    { path: 'messages', loadComponent: () => import('./pages/messages/messages').then((m) => m.MessagesPage) },
  ]),
  roleArea(UserRole.Tutor, [
    { path: 'dashboard', loadComponent: () => import('./pages/tutor-dashboard/tutor-dashboard').then((m) => m.TutorDashboard) },
    { path: 'availability', loadComponent: () => import('./pages/tutor-availability/tutor-availability').then((m) => m.TutorAvailability) },
    { path: 'lessons', loadComponent: () => import('./pages/tutor-lessons/tutor-lessons').then((m) => m.TutorLessons) },
    { path: 'messages', loadComponent: () => import('./pages/messages/messages').then((m) => m.MessagesPage) },
    { path: 'notes', loadComponent: () => import('./pages/tutor-notes/tutor-notes').then((m) => m.TutorNotes) },
    { path: 'earnings', loadComponent: () => import('./pages/tutor-earnings/tutor-earnings').then((m) => m.TutorEarnings) },
    { path: 'feedback', loadComponent: () => import('./pages/tutor-feedback/tutor-feedback').then((m) => m.TutorFeedback) },
    { path: 'profile', loadComponent: () => import('./pages/tutor-profile/tutor-profile').then((m) => m.TutorProfile) },
  ]),
  roleArea(UserRole.Admin, [
    { path: 'dashboard', loadComponent: () => import('./pages/admin-dashboard/admin-dashboard').then((m) => m.AdminDashboard) },
    { path: 'tutors', loadComponent: () => import('./pages/admin-tutors/admin-tutors').then((m) => m.AdminTutors) },
    { path: 'tutors/:tutorId', loadComponent: () => import('./pages/admin-tutor-detail/admin-tutor-detail').then((m) => m.AdminTutorDetail) },
    { path: 'users', loadComponent: () => import('./pages/admin-users/admin-users').then((m) => m.AdminUsers) },
    { path: 'pricing', loadComponent: () => import('./pages/admin-subjects/admin-subjects').then((m) => m.AdminSubjects) },
    { path: 'payments', loadComponent: () => import('./pages/admin-bookings/admin-bookings').then((m) => m.AdminBookings) },
    { path: 'disputes', loadComponent: () => import('./pages/admin-disputes/admin-disputes').then((m) => m.AdminDisputes) },
    { path: 'analytics', loadComponent: () => import('./pages/admin-analytics/admin-analytics').then((m) => m.AdminAnalytics) },
    { path: 'emails', loadComponent: () => import('./pages/admin-emails/admin-emails').then((m) => m.AdminEmails) },
  ]),
  { path: '**', redirectTo: 'login' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
