import { Component, ChangeDetectionStrategy } from '@angular/core';
@Component({ selector: 'ui-card', standalone: true, template: `<section class="card"><ng-content /></section>`, changeDetection: ChangeDetectionStrategy.Eager,
 styles: [`.card{background:var(--oi-bg-primary);border:1px solid var(--oi-border-muted);border-radius:var(--oi-radius-l);padding:var(--oi-space-xl);box-shadow:var(--oi-shadow-low);margin-bottom:var(--oi-space-l)}`] })
export class UiCardComponent {}
