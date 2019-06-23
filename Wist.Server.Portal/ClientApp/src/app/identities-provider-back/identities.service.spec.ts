import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { IdentitiesService } from './identities.service';
import { compileBaseDefFromMetadata } from '@angular/compiler';

describe('IdentitiesService', () => {
  let component: IdentitiesService;
  let fixture: ComponentFixture<IdentitiesService>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [IdentitiesService]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(IdentitiesService);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('addIdentity Neg', async(() => {
    component.addIdentity(null);
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('getIdentity Neg', async(() => {
    component.getIdentity(null);
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('getIdentity', async(() => {
    component.getIdentity("aaa");
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('getIdentityAttributesSchema', async(() => {
    component.getIdentityAttributesSchema();
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('sendIdentity Neg 1', async(() => {
    component.sendIdentity(null, null, null);
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('sendIdentity Neg 2', async(() => {
    component.sendIdentity("aa", null, null);
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('sendIdentity Neg 3', async(() => {
    component.sendIdentity(null, "bb", null);
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));

  it('sendIdentity Neg 4', async(() => {
    component.sendIdentity(null, null, "cc");
    const titleText = fixture.nativeElement.querySelector('h1').textContent;
    expect(titleText).toEqual('MIA');
  }));
});
