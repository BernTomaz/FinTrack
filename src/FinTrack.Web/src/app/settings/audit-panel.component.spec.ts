import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuditPanelComponent } from './audit-panel.component';

describe('AuditPanelComponent', () => {
  const event = { id: 'one', actorUserId: 'owner', action: 'Updated', entityType: 'Account', entityId: 'account', occurredAtUtc: '2026-01-02T23:59:59Z' };
  let http: HttpTestingController;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AuditPanelComponent], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(AuditPanelComponent);
    fixture.componentRef.setInput('token', 'test-token');
    fixture.detectChanges();
    return fixture;
  }
  function list(page = 1, total = 1) {
    const request = http.expectOne(r => r.url.includes('/audit-events?') && new URL(r.url).searchParams.get('page') === String(page));
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({ items: [event], totalCount: total, page, pageSize: 20 });
  }

  it('loads details on demand and renders snapshots as text', () => {
    const fixture = setup();
    list();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Edição');
    fixture.componentInstance['toggle'](event);
    http.expectOne(r => r.url.endsWith('/audit-events/one')).flush({ ...event, correlationId: 'support', beforeJson: '{"Name":"<img src=x onerror=alert(1)>","InitialBalance":25}', afterJson: '{"Name":"Corrigida","InitialBalance":50}' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('<img src=x onerror=alert(1)>');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Antes');
    expect(fixture.nativeElement.textContent).toContain('Depois');
    expect(fixture.nativeElement.textContent).toContain('support');
    fixture.destroy();
  });

  it('paginates and restarts at page one when filters change or clear', () => {
    const fixture = setup();
    list(1, 21);
    fixture.componentInstance['load'](2);
    list(2, 21);
    fixture.componentInstance['filters'].setValue({ entityType: 'Account', action: 'Updated', from: '2026-01-01', to: '2026-01-02' });
    fixture.componentInstance['load']();
    const filtered = http.expectOne(r => r.url.includes('/audit-events?') && new URL(r.url).searchParams.get('entityType') === 'Account');
    expect(new URL(filtered.request.url).searchParams.get('page')).toBe('1');
    filtered.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    fixture.componentInstance['clear']();
    const cleared = http.expectOne(r => r.url.includes('/audit-events?'));
    expect(new URL(cleared.request.url).searchParams.has('entityType')).toBe(false);
    cleared.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nenhum evento encontrado');
    fixture.destroy();
  });

  it('offers retry after an error and cancels pending details on destruction', () => {
    const fixture = setup();
    http.expectOne(r => r.url.includes('/audit-events?')).flush('Erro', { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Tente atualizar');
    fixture.componentInstance['load']();
    list();
    fixture.componentInstance['toggle'](event);
    const detail = http.expectOne(r => r.url.endsWith('/audit-events/one'));
    fixture.destroy();
    expect(detail.cancelled).toBe(true);
  });

  it('rejects reversed dates without sending a request', () => {
    const fixture = setup();
    list();
    fixture.componentInstance['filters'].patchValue({ from: '2026-02-01', to: '2026-01-01' });
    fixture.componentInstance['load']();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('A data inicial');
    http.expectNone(r => r.url.includes('/audit-events?'));
    fixture.destroy();
  });
});
