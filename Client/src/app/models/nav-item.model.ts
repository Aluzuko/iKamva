export interface NavItem {
  label: string;
  icon: string; // bootstrap-icons class
  route: string; // relative child route, e.g. 'dashboard'
  badge?: number | 'new';
  short?: string;
}
