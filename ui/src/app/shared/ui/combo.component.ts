import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild, ChangeDetectionStrategy } from '@angular/core';

import { FormsModule } from '@angular/forms';

// Editable combobox: pick a known option or type a free-text value. Styled to match
// the app's inputs/selects rather than the browser's native <datalist> popup.
@Component({
  selector: 'ui-combo',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="combo" [class.open]="open">
      <input #field class="input combo-input" type="text" [attr.id]="inputId || null"
        [placeholder]="placeholder" autocomplete="off" spellcheck="false" autocapitalize="off"
        role="combobox" aria-autocomplete="list" [attr.aria-expanded]="open"
        [ngModel]="value" (ngModelChange)="onInput($event)"
        (focus)="open = true" (keydown)="onKeydown($event)" />
        <button type="button" class="chev" tabindex="-1"
          [attr.aria-label]="open ? 'Hide options' : 'Show options'"
          (mousedown)="$event.preventDefault()" (click)="toggle()">
          <svg viewBox="0 0 256 256" aria-hidden="true"><path d="M208 96l-80 80-80-80" fill="none" stroke="currentColor" stroke-width="22" stroke-linecap="round" stroke-linejoin="round"/></svg>
        </button>
        @if (open && filtered.length) {
          <ul class="panel" role="listbox">
            @for (opt of filtered; track opt; let i = $index) {
              <li role="option"
                [attr.aria-selected]="opt === value" [class.active]="i === activeIndex" [class.selected]="opt === value"
                (mousedown)="$event.preventDefault(); choose(opt)" (mousemove)="activeIndex = i">
                <span>{{ opt }}</span>
                @if (opt === value) {
                  <svg class="tick" viewBox="0 0 256 256" aria-hidden="true"><path d="M232 56L104 184l-56-56" fill="none" stroke="currentColor" stroke-width="24" stroke-linecap="round" stroke-linejoin="round"/></svg>
                }
              </li>
            }
          </ul>
        }
      </div>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .combo { position: relative; }
    .combo-input { padding-right: 34px; }
    .chev { position: absolute; top: 0; right: 0; height: 100%; width: 34px; display: grid; place-items: center; background: none; border: none; padding: 0; color: var(--oi-content-tertiary); cursor: pointer; }
    .chev:hover { color: var(--oi-content-secondary); }
    .chev svg { width: 15px; height: 15px; transition: transform .16s cubic-bezier(.2,.8,.2,1); }
    .combo.open .chev svg { transform: rotate(180deg); }
    .panel { position: absolute; top: calc(100% + 6px); left: 0; right: 0; z-index: 60; margin: 0; padding: var(--oi-space-xs); list-style: none; max-height: 232px; overflow-y: auto; background: var(--oi-bg-primary); border: 1px solid var(--oi-border-muted); border-radius: var(--oi-radius-l); box-shadow: var(--oi-shadow-mid); transform-origin: top; animation: combo-in .12s cubic-bezier(.2,.8,.2,1); }
    .panel li { display: flex; align-items: center; justify-content: space-between; gap: var(--oi-space-s); padding: 8px var(--oi-space-m); border-radius: var(--oi-radius); font-size: 13px; color: var(--oi-content-secondary); cursor: pointer; }
    .panel li.active { background: var(--oi-bg-secondary); color: var(--oi-content-primary); }
    .panel li.selected { color: var(--oi-brand-base); font-weight: 600; }
    .tick { width: 15px; height: 15px; flex: none; color: var(--oi-brand-base); }
    @keyframes combo-in { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: none; } }
    @media (prefers-reduced-motion: reduce) { .panel { animation: none; } .chev svg { transition: none; } }
  `],
})
export class UiComboComponent {
  @Input() options: string[] = [];
  @Input() value = '';
  @Input() placeholder = '';
  @Input() inputId = '';
  @Output() valueChange = new EventEmitter<string>();
  @ViewChild('field') field!: ElementRef<HTMLInputElement>;

  open = false;
  activeIndex = -1;

  constructor(private host: ElementRef<HTMLElement>) {}

  get filtered(): string[] {
    const v = (this.value || '').trim().toLowerCase();
    if (!v) return this.options;
    // When the field already equals an option exactly, show the whole list so the
    // user can still switch; otherwise narrow to substring matches.
    if (this.options.some(o => o.toLowerCase() === v)) return this.options;
    return this.options.filter(o => o.toLowerCase().includes(v));
  }

  onInput(v: string): void {
    this.value = v;
    this.valueChange.emit(v);
    this.open = true;
    this.activeIndex = -1;
  }

  toggle(): void {
    this.open = !this.open;
    if (this.open) { this.field?.nativeElement.focus(); }
  }

  choose(opt: string): void {
    this.value = opt;
    this.valueChange.emit(opt);
    this.open = false;
    this.activeIndex = -1;
  }

  onKeydown(e: KeyboardEvent): void {
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        if (!this.open) { this.open = true; break; }
        this.activeIndex = Math.min(this.activeIndex + 1, this.filtered.length - 1);
        break;
      case 'ArrowUp':
        e.preventDefault();
        if (this.open) { this.activeIndex = Math.max(this.activeIndex - 1, 0); }
        break;
      case 'Enter':
        if (this.open && this.activeIndex >= 0 && this.activeIndex < this.filtered.length) {
          e.preventDefault();
          this.choose(this.filtered[this.activeIndex]);
        }
        break;
      case 'Escape':
        if (this.open) { e.preventDefault(); e.stopPropagation(); this.open = false; }
        break;
    }
  }

  @HostListener('document:mousedown', ['$event'])
  onDocumentDown(e: MouseEvent): void {
    if (!this.host.nativeElement.contains(e.target as Node)) { this.open = false; }
  }
}
