import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UiToggleComponent } from './toggle.component';

describe('UiToggleComponent', () => {
  let fixture: ComponentFixture<UiToggleComponent>;
  let component: UiToggleComponent;
  let input: HTMLInputElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiToggleComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(UiToggleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
  });

  it('returns to the bound state after requesting a change', () => {
    let requested: boolean | undefined;
    component.changed.subscribe(value => requested = value);

    input.click();

    expect(requested).toBeTrue();
    expect(input.checked).toBeFalse();
  });

  it('reflects a change after the bound state is updated', () => {
    component.changed.subscribe(value => component.checked = value);

    input.click();
    fixture.detectChanges();

    expect(input.checked).toBeTrue();
  });
});
