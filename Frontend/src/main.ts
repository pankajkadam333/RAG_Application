import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { APP_CONFIG, loadAppConfig } from './app/config/app-config';

loadAppConfig().then(config =>
  bootstrapApplication(AppComponent, {
    providers: [
      { provide: APP_CONFIG, useValue: config },
      provideRouter(routes),
      provideHttpClient(),
      provideAnimations()
    ]
  })
).catch(err => console.error(err));
