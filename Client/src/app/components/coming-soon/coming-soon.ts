import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  templateUrl: './coming-soon.html',
  styleUrl: './coming-soon.scss',
})
export class ComingSoon {
  title = 'Coming soon';

  constructor(route: ActivatedRoute) {
    this.title = route.snapshot.data['title'] ?? 'Coming soon';
  }
}
