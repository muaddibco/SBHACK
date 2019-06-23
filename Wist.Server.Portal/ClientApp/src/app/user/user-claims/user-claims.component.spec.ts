import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UserClaimsComponent } from './user-claims.component';
import { UserAttributeDto } from '../user.Service'

describe('UserClaimsComponent', () => {
  let component: UserClaimsComponent;
  let fixture: ComponentFixture<UserClaimsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [UserClaimsComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UserClaimsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('ngOnInit', async(() => {
    component.ngOnInit()
    const titleText = fixture.nativeElement.querySelector('th').textContent;
    expect(titleText).toEqual('Last Commitment');
  }));

  it('validateContent Neg', async(() => {
    component.validateContent(null, null);
    const titleText = fixture.nativeElement.querySelector('h4').textContent;
    expect(titleText).toEqual('Root Identity Proofs');
  }));

  it('onSubmitSending Empty', async(() => {
    var userAttributeDto: UserAttributeDto;
    userAttributeDto = new UserAttributeDto();
    component.validateContent(userAttributeDto, null);
    const titleText = fixture.nativeElement.querySelector('h4').textContent;
    expect(titleText).toEqual('Root Identity Proofs');
  }));
});
