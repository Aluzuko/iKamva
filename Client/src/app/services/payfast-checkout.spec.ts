import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PayFastCheckout } from './payfast-checkout';

describe('PayFastCheckout', () => {
  let service: PayFastCheckout;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PayFastCheckout);
  });

  afterEach(() => document.querySelectorAll('form').forEach((f) => f.remove()));

  it('posts every field to PayFast in order', () => {
    const submit = vi.spyOn(HTMLFormElement.prototype, 'submit').mockImplementation(() => undefined);
    service.submit({
      attemptId: 'a1',
      processUrl: 'https://sandbox.payfast.co.za/eng/process',
      fields: [
        { name: 'merchant_id', value: '10000100' },
        { name: 'amount', value: '620.00' },
        { name: 'signature', value: 'abc' },
      ],
    });
    const form = document.querySelector('form')!;
    expect(form.method).toBe('post');
    expect(form.action).toBe('https://sandbox.payfast.co.za/eng/process');
    expect(Array.from(form.querySelectorAll('input')).map((i) => i.name)).toEqual(['merchant_id', 'amount', 'signature']);
    expect(submit).toHaveBeenCalled();
    submit.mockRestore();
  });

  it('refuses to post anywhere except PayFast', () => {
    expect(() => service.submit({ attemptId: 'a1', processUrl: 'https://evil.example.com/eng/process', fields: [] })).toThrow();
    expect(() => service.submit({ attemptId: 'a1', processUrl: 'http://sandbox.payfast.co.za/eng/process', fields: [] })).toThrow();
  });
});
