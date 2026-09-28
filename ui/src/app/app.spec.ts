import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { MockBridge } from './core/bridge/mock-bridge';
import { SCALUS_BRIDGE } from './core/bridge/scalus-bridge';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [MockBridge, { provide: SCALUS_BRIDGE, useExisting: MockBridge }]
    }).compileComponents();
  });

  it('renders the four configuration screens in the shell', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Protocols');
    expect(text).toContain('Applications');
    expect(text).toContain('Import / Export');
    expect(text).toContain('About');
  });

  it('asks for confirmation before removing an unregistered protocol', async () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const protocol = { Protocol: 'telnet', AppId: null };
    app.config = { Protocols: [protocol], Applications: [] };

    const removal = app.removeProtocol(protocol);

    expect(app.confirm.open).toBeTrue();
    expect(app.confirm.title).toBe('Remove telnet://?');
    expect(app.confirm.confirmLabel).toBe('Remove protocol');

    app.resolveConfirm(false);
    await removal;

    expect(app.config.Protocols).toContain(protocol);
  });

  it('asks for confirmation before removing an application', async () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const application = {
      Id: 'test-app',
      Name: 'Test application',
      Description: '',
      Platforms: ['Windows' as const],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: [] },
      Exec: 'ssh',
      Args: []
    };
    app.config = {
      Protocols: [{ Protocol: 'ssh', AppId: application.Id }],
      Applications: [application]
    };
    app.editor = application;
    app.editorOpen = true;

    const removal = app.removeApplication(application);

    expect(app.confirm.open).toBeTrue();
    expect(app.confirm.title).toBe('Remove Test application?');
    expect(app.confirm.confirmLabel).toBe('Remove application');
    expect(app.confirm.message).toContain('ssh://');

    app.resolveConfirm(false);
    await removal;

    expect(app.config.Applications).toContain(application);
    expect(app.config.Protocols[0].AppId).toBe(application.Id);
    expect(app.editorOpen).toBeTrue();
  });

  it('closes an application menu when clicking elsewhere on the page', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const menu = fixture.nativeElement.querySelector('details.menu') as HTMLDetailsElement;
    menu.open = true;

    document.body.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));

    expect(menu.open).toBeFalse();
  });

  it('closes an application menu when selecting an action', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const menu = fixture.nativeElement.querySelector('details.menu') as HTMLDetailsElement;
    const edit = menu.querySelector('button') as HTMLButtonElement;
    menu.open = true;

    edit.click();

    expect(menu.open).toBeFalse();
  });

  it('closes the previous application menu when another opens', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const menus = fixture.nativeElement.querySelectorAll('details.menu') as NodeListOf<HTMLDetailsElement>;
    expect(menus.length).toBeGreaterThan(1);

    menus[0].querySelector('summary')?.click();
    menus[1].querySelector('summary')?.click();

    expect(menus[0].open).toBeFalse();
    expect(menus[1].open).toBeTrue();
  });

  it('filters custom protocol handlers by the application protocol', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    app.platform = 'Windows';
    app.config = {
      Protocols: [{ Protocol: 'vnc', AppId: null }],
      Applications: [
        {
          Id: 'vnc-viewer',
          Name: 'VNC Viewer',
          Platforms: ['Windows'],
          Protocol: 'vnc',
          Parser: { ParserId: 'url', Options: [] },
          Exec: 'vnc.exe'
        },
        {
          Id: 'web-browser',
          Name: 'Web Browser',
          Platforms: ['Windows'],
          Protocol: 'https',
          Parser: { ParserId: 'url', Options: [] },
          Exec: 'browser.exe'
        }
      ]
    };

    expect(app.appOptionsFor(app.config.Protocols[0])).toEqual([
      { label: 'VNC Viewer · URL', value: 'vnc-viewer' }
    ]);
  });
});
