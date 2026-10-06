import { Component, EventEmitter, Input, Output } from '@angular/core';
@Component({ selector: 'ui-toggle', standalone: true, template: `<label class="toggle" [class.disabled]="disabled"><input type="checkbox" [checked]="checked" [disabled]="disabled" (change)="onChange($any($event.target))" /><span class="track"></span></label>`, styles: [`.toggle{position:relative;width:42px;height:24px;display:inline-flex;flex:none}.toggle input{opacity:0;width:0;height:0}.track{position:absolute;inset:0;border-radius:var(--oi-radius-max);background:var(--oi-border-strong);transition:background .15s;cursor:pointer}.track:before{content:"";position:absolute;height:18px;width:18px;left:3px;top:3px;background:#fff;border-radius:50%;transition:transform .15s;box-shadow:var(--oi-shadow-low)}input:checked+.track{background:var(--oi-btn-primary)}input:checked+.track:before{transform:translateX(18px)}input:focus-visible+.track{outline:2px solid var(--oi-brand-base);outline-offset:2px}.disabled{opacity:.45}.disabled .track{cursor:not-allowed}`] })
export class UiToggleComponent {
  @Input() checked = false;
  @Input() disabled = false;
  @Output() changed = new EventEmitter<boolean>();

  onChange(input: HTMLInputElement): void {
    this.changed.emit(input.checked);
    input.checked = this.checked;
  }
}
