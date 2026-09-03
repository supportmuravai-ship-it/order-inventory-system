import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkspaceComponent } from './workspace';

describe('Workspace', () => {
  let component: WorkspaceComponent;
  let fixture: ComponentFixture<WorkspaceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
