import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PageHeader } from '../page-header/page-header';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [PageHeader],
  templateUrl: './coming-soon.html',
  styleUrl: './coming-soon.scss',
})
export class ComingSoon {
  title = 'Coming soon';

  constructor(route: ActivatedRoute) {
    this.title = route.snapshot.data['title'] ?? 'Coming soon';
  }
}
