import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TransferIdentityComponent } from './transfer-identity.component';

describe('TransferIdentityComponent', () => {
  let component: TransferIdentityComponent;
  let fixture: ComponentFixture<TransferIdentityComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [TransferIdentityComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TransferIdentityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('ngOnInit', async(() => {
    component.ngOnInit()
    const titleText = fixture.nativeElement.querySelector('label').textContent;
    expect(titleText).toEqual('Identity Description');
  }));

  it('onCancelSend', async(() => {
    component.onCancelSend();
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('Sending identity attribute');
  }));

  it('onSubmitSending', async(() => {
    component.onSubmitSending();
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('Sending identity attribute');
  }));
});
