import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app.routes';
import { App } from './app';
import { NotificationContainerComponent } from './components/notification-container/notification-container';

@NgModule({
  declarations: [App],
  imports: [BrowserModule, AppRoutingModule, NotificationContainerComponent],
  providers: [provideBrowserGlobalErrorListeners()],
  bootstrap: [App],
})
export class AppModule {}