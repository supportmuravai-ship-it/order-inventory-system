import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StoreSelection } from './store-selection';

describe('StoreSelection', () => {
  let component: StoreSelection;
  let fixture: ComponentFixture<StoreSelection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoreSelection],
    }).compileComponents();

    fixture = TestBed.createComponent(StoreSelection);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
