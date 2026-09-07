import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ROLE_NAV_ITEMS } from './config/role-nav.config';
import { UserRole } from './models/user-role.model';
import { authGuard, roleGuard } from './guards/auth.guard';
import { ComingSoon } from './components/coming-soon/coming-soon';
import { MainLayout } from './components/main-layout/main-layout';
import { RoleSelect } from './pages/role-select/role-select';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { ForgotPassword } from './pages/forgot-password/forgot-password';
import { ParentDashboard } from './pages/parent-dashboard/parent-dashboard';
import { ParentChildren } from './pages/parent-children/parent-children';
import { StudentDetailPage } from './pages/student-detail/student-detail';
import { ParentPayments } from './pages/parent-payments/parent-payments';
import { LinkStudent } from './pages/link-student/link-student';
import { AdminDashboard } from './pages/admin-dashboard/admin-dashboard';
import { AdminTutors } from './pages/admin-tutors/admin-tutors';
import { AdminUsers } from './pages/admin-users/admin-users';
import { AdminSubjects } from './pages/admin-subjects/admin-subjects';
import { AdminBookings } from './pages/admin-bookings/admin-bookings';
import { AdminDisputes } from './pages/admin-disputes/admin-disputes';
import { AdminAnalytics } from './pages/admin-analytics/admin-analytics';
import { Profile } from './pages/profile/profile';

function screensFor(role: UserRole, excludeRoutes: string[] = []) {
  return ROLE_NAV_ITEMS[role]
    .filter((item) => !excludeRoutes.includes(item.route))
    .map((item) => ({
      path: item.route,
      component: ComingSoon,
      data: { title: item.label },
    }));
}

const routes: Routes = [
  { path: '', redirectTo: '/role-select', pathMatch: 'full' },
  { path: 'role-select', component: RoleSelect },
  { path: 'login/:role', component: Login },
  { path: 'register/:role', component: Register },
  { path: 'forgot-password', component: ForgotPassword },
  {
    path: 'student',
    component: MainLayout,
    data: { role: UserRole.Student },
    canActivate: [authGuard, roleGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Student),
    ],
  },
  {
    path: 'parent',
    component: MainLayout,
    data: { role: UserRole.Parent },
    canActivate: [authGuard, roleGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: ParentDashboard },
      { path: 'children', component: ParentChildren },
      { path: 'children/:studentId', component: StudentDetailPage },
      { path: 'payments', component: ParentPayments },
      { path: 'link-student', component: LinkStudent },
    ],
  },
  {
    path: 'tutor',
    component: MainLayout,
    data: { role: UserRole.Tutor },
    canActivate: [authGuard, roleGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Tutor),
    ],
  },
  {
    path: 'admin',
    component: MainLayout,
    data: { role: UserRole.Admin },
    canActivate: [authGuard, roleGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AdminDashboard },
      { path: 'tutors', component: AdminTutors },
      { path: 'users', component: AdminUsers },
      { path: 'pricing', component: AdminSubjects },
      { path: 'payments', component: AdminBookings },
      { path: 'disputes', component: AdminDisputes },
      { path: 'analytics', component: AdminAnalytics },
      ...screensFor(UserRole.Admin, ['dashboard', 'tutors', 'users', 'pricing', 'payments', 'disputes', 'analytics']),
    ],
  },
  {path:  'profile', component: Profile},
  { path: '**', redirectTo: '/role-select' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
