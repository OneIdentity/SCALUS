import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'ui-button',
  standalone: true,
  template: `<button [type]="type" [class]="'btn btn-' + variant" [disabled]="disabled"><ng-content /></button>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.btn{display:inline-flex;align-items:center;justify-content:center;gap:var(--oi-space-s);height:36px;padding:0 var(--oi-space-l);border-radius:var(--oi-radius);border:1px solid transparent;font-size:13.5px;font-weight:600;transition:background .12s,border-color .12s,color .12s}.btn-primary{background:var(--oi-btn-primary);color:#fff}.btn-primary:hover:not(:disabled){background:var(--oi-btn-primary-hover)}.btn-ghost{background:transparent;color:var(--oi-content-secondary);border-color:var(--oi-border)}.btn-ghost:hover:not(:disabled){background:var(--oi-bg-secondary);border-color:var(--oi-border-strong)}.btn-subtle{background:var(--oi-bg-secondary);color:var(--oi-content-secondary)}.btn-subtle:hover:not(:disabled){background:var(--oi-bg-tertiary)}.btn-danger{background:transparent;color:var(--oi-content-error);border-color:var(--oi-border)}.btn-danger:hover:not(:disabled){background:var(--oi-bg-error);border-color:var(--oi-btn-danger)}.btn:disabled{opacity:.55}`]
})
export class UiButtonComponent { @Input() variant: 'primary' | 'ghost' | 'subtle' | 'danger' = 'ghost'; @Input() disabled = false; @Input() type: 'button' | 'submit' = 'button'; }
