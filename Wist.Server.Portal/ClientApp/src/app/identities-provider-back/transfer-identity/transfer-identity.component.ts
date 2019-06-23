import { Component, Input, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { catchError, first } from 'rxjs/operators';
import { IdentitiesService } from '../identities.service'
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'transfer-identity',
  templateUrl: './transfer-identity.component.html'
})
/** textboxCopyClipboard component*/
export class TransferIdentityComponent implements OnInit {

  @Input() public identityDescriptionTxt: string = '';
  @Input() public idCardNumberTxt: string = '';
  publicSpendKey: string = '';
  publicViewKey: string = '';
  public isForUser: boolean;
  public sendIdentityForm: FormGroup;
  public submitted = false;
  public submitClick = false;
  public isLoaded = false;

  constructor(private service: IdentitiesService, private router: Router, private route: ActivatedRoute, private formBuilder: FormBuilder, titleService: Title) {
    this.isForUser = true;
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    titleService.setTitle(tokenInfo.accountInfo);
  }

  ngOnInit() {
    this.service.getIdentity(this.route.snapshot.paramMap.get('id')).subscribe(r => {
      this.identityDescriptionTxt = r.description;
      this.idCardNumberTxt = r.rootAttribute.content;
      //this.isForUser = r.claimSubject == "1";

      if (this.isForUser) {
        this.sendIdentityForm = this.formBuilder.group({
          psk: ['', Validators.required],
          //pvk: ['', Validators.required],
        });
      }
      else {
        this.sendIdentityForm = this.formBuilder.group({
          psk: ['', Validators.required]
        });
      }

      this.isLoaded = true;
    });

  }

  get formData() { return this.sendIdentityForm.controls; }

  onSubmitSending() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.sendIdentityForm.invalid) {
      return;
    }

    this.submitClick = true;

    if (this.isForUser) {
      var psk: string;
      psk = this.formData.psk.value;
      var spendKey = psk.substr(0, 64);
      var viewKey = psk.substr(64);

      this.service.sendIdentity(this.idCardNumberTxt, spendKey, viewKey)
        .subscribe(a => {
          this.router.navigate(['/identityProvider']);
        });
    }
    else {
      this.service.sendIdentity(this.idCardNumberTxt, this.formData.psk.value, null)
        .subscribe(a => {
          this.router.navigate(['/identityProvider']);
        });
    }
  }

  onCancelSend() {
    this.router.navigate(['/identityProvider']);
  }
}
