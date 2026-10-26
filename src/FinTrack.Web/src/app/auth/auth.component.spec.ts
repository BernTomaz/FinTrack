import { vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthComponent } from './auth.component';
import { FinTrackApiService } from '../fintrack-api.service';

describe('AuthComponent', () => {
  let http: HttpTestingController;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AuthComponent], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    TestBed.inject(FinTrackApiService).clearSession();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); vi.useRealTimers(); });

  it.each(['login', 'register'] as const)('prevents duplicate %s requests and allows retry after failure', (action) => {
    const app = TestBed.createComponent(AuthComponent).componentInstance;
    app['loginForm'].setValue({ email: 'user@example.com', password: 'Strong@123' });
    app['registerForm'].setValue({ name: 'Usuário', email: 'user@example.com', password: 'Strong@123' });
    app[action]();
    app[action]();
    expect(app['authPending']()).toBe(true);
    const request = http.expectOne(request => request.url.endsWith(`/auth/${action}`));
    request.flush('Unavailable', { status: 503, statusText: 'Unavailable' });
    expect(app['authPending']()).toBe(false);
    app[action]();
    http.expectOne(request => request.url.endsWith(`/auth/${action}`))
      .flush('Unavailable', { status: 503, statusText: 'Unavailable' });
  });

  it('releases the registration button after success', () => {
    const app = TestBed.createComponent(AuthComponent).componentInstance;
    app['registerForm'].setValue({ name: 'Usuário', email: 'user@example.com', password: 'Strong@123' });
    app['register']();
    http.expectOne(request => request.url.endsWith('/auth/register')).flush({});
    expect(app['authPending']()).toBe(false);
    expect(app['authMode']()).toBe('login');
    expect(app['loginForm'].controls.email.value).toBe('user@example.com');
  });

});
