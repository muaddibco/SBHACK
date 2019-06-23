import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { UserService } from '../user.Service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Title } from '@angular/platform-browser';

@Component({
  templateUrl: './duplicate-user.component.html'
})

export class DuplicateUserComponent implements OnInit {

  public isLoaded: boolean;
  public spendKey: string;
  public viewKey: string;
  public accountId: string;
  public pageTitle: string;
  public duplicationForm: FormGroup;
  submitted = false;
  submitClick = false;

  constructor(private route: ActivatedRoute, private router: Router, private userService: UserService, private formBuilder: FormBuilder,titleService: Title) {
    this.isLoaded = false;
    this.accountId = sessionStorage.getItem('AccountId');
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    titleService.setTitle(tokenInfo.accountInfo);
  }

  ngOnInit() {
    this.userService.getUserDetails().subscribe(r => {
      this.spendKey = r.publicSpendKey;
      this.viewKey = r.publicViewKey;
      this.pageTitle = r.accountInfo;
      this.isLoaded = true;
    });

    this.duplicationForm = this.formBuilder.group({
      accountInfo: ['', Validators.required]
    });
  }

  get formData() { return this.duplicationForm.controls; }

  onSubmitDuplicating() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.duplicationForm.invalid) {
      return;
    }

    this.submitClick = true;

    this.userService.duplicateUser(this.accountId, this.formData.accountInfo.value).subscribe(r => {
      this.router.navigate(['/user']);
    });
  }

  onCancelDuplicating() {
    this.router.navigate(['/user']);
  }
}
