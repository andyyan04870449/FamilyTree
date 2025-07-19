import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PersonTable } from './person-table';

describe('PersonTable', () => {
  let component: PersonTable;
  let fixture: ComponentFixture<PersonTable>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonTable]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PersonTable);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
