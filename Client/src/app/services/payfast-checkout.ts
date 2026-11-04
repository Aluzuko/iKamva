import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { PaymentRedirect } from '../models/parent.model';
import { ParentApiService } from './parent-api.service';

const ALLOWED_HOSTS = ['sandbox.payfast.co.za', 'www.payfast.co.za'];

@Injectable({ providedIn: 'root' })
export class PayFastCheckout {
  constructor(private readonly parentApi: ParentApiService) {}

  start(invoiceId: string): Observable<PaymentRedirect> {
    return this.parentApi.startCheckout(invoiceId).pipe(tap((redirect) => this.submit(redirect)));
  }

  submit(redirect: PaymentRedirect): void {
    const url = new URL(redirect.processUrl);
    if (url.protocol !== 'https:' || !ALLOWED_HOSTS.includes(url.hostname)) {
      throw new Error('Unexpected payment URL');
    }
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = url.toString();
    form.style.display = 'none';
    for (const field of redirect.fields) {
      const input = document.createElement('input');
      input.type = 'hidden';
      input.name = field.name;
      input.value = field.value;
      form.appendChild(input);
    }
    document.body.appendChild(form);
    form.submit();
  }

  errorMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse && err.error?.message) {
      return err.error.message;
    }
    return 'We couldn’t start the payment. Try again.';
  }
}
