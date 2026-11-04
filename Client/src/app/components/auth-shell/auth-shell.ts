import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './auth-shell.html',
  styleUrl: './auth-shell.scss',
})
export class AuthShell {
  @Input({ required: true }) title!: string;
  @Input() subtitle: string | null = null;
  @Input() side: 'signin' | 'signup' = 'signin';
}
