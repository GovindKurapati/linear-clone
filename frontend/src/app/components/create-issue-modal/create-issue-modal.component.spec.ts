import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateIssueModalComponent } from './create-issue-modal.component';

describe('CreateIssueModalComponent', () => {
  let component: CreateIssueModalComponent;
  let fixture: ComponentFixture<CreateIssueModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateIssueModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateIssueModalComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
