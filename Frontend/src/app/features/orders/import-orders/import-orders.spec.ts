import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImportOrders } from './import-orders';

describe('ImportOrders', () => {
  let component: ImportOrders;
  let fixture: ComponentFixture<ImportOrders>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImportOrders],
    }).compileComponents();

    fixture = TestBed.createComponent(ImportOrders);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
