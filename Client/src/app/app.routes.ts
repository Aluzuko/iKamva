import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ROLE_NAV_ITEMS } from './config/role-nav.config';
import { UserRole } from './models/user-role.model';
import { ComingSoon } from './components/coming-soon/coming-soon';
import { MainLayout } from './components/main-layout/main-layout';
import { RoleSelect } from './pages/role-select/role-select';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { ForgotPassword } from './pages/forgot-password/forgot-password';

function screensFor(role: UserRole) {
  return ROLE_NAV_ITEMS[role].map((item) => ({
    path: item.route,
    component: ComingSoon,
    data: { title: item.label },
  }));
}

const routes: Routes = [
  { path: '', component: RoleSelect },
  { path: 'login/:role', component: Login },
  { path: 'register/:role', component: Register },
  { path: 'forgot-password', component: ForgotPassword },
  {
    path: 'student',
    component: MainLayout,
    data: { role: UserRole.Student },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Student),
    ],
  },
  {
    path: 'parent',
    component: MainLayout,
    data: { role: UserRole.Parent },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Parent),
    ],
  },
  {
    path: 'tutor',
    component: MainLayout,
    data: { role: UserRole.Tutor },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Tutor),
    ],
  },
  {
    path: 'admin',
    component: MainLayout,
    data: { role: UserRole.Admin },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      ...screensFor(UserRole.Admin),
    ],
  },
  { path: '**', redirectTo: '/role-select' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}