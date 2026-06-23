import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IssueBoardComponent } from './issue-board.component';

describe('IssueBoardComponent', () => {
  let component: IssueBoardComponent;
  let fixture: ComponentFixture<IssueBoardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IssueBoardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(IssueBoardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
