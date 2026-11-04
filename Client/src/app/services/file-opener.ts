import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root',
})
export class FileOpener {
  constructor(private readonly http: HttpClient, private readonly notifications: NotificationService) {}

  open(url: string, fileName: string): void {
    const tab = window.open('', '_blank');
    if (tab) {
      tab.opener = null;
    }
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        if (tab) {
          tab.location.href = objectUrl;
        } else {
          const link = document.createElement('a');
          link.href = objectUrl;
          link.download = fileName;
          link.click();
        }
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: () => {
        tab?.close();
        this.notifications.showError('Couldn’t open the document', 'It may have been removed. Refresh and try again.');
      },
    });
  }
}
