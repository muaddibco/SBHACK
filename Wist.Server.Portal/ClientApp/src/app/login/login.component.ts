import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { first } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { AuthenticationService } from '../authentication/authentication.service';

@Component({
  templateUrl: './login.component.html'
})

export class LoginComponent implements OnInit {

  loginForm: FormGroup;
  submitClick = false;
  submitted = false;
  returnUrl: string;
  error = '';

  constructor(private formBuilder: FormBuilder, private route: ActivatedRoute, private router: Router, private authenticationService: AuthenticationService, titleService: Title) {
    titleService.setTitle("Wist Demo Portal - Login");
  }

  ngOnInit() {
    this.loginForm = this.formBuilder.group({
      password: ['', Validators.required]
    });

    // reset login status
    //this.authenticationService.logout();

    // get return url from route parameters or default to '/'
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  // convenience getter for easy access to form fields
  get formData() { return this.loginForm.controls; }

  onCancel() {
    this.router.navigate([this.returnUrl]);
  }

  onLogin() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.loginForm.invalid) {
      return;
    }

    this.submitClick = true;
    this.processLogin();
  }

  processLogin() {
    this.authenticationService.login(this.route.snapshot.paramMap.get('id'), this.formData.password.value)
        .pipe(first())
        .subscribe(data => {
          this.navigate(data.accountType);
        }, error => {
            this.error = error;
            this.submitClick = false;
        });
    }

  navigate(accountType: number) {
    switch (accountType) {
        case 1: {
            this.router.navigate(['/identityProvider']);
            break;
        }
        case 2: {
            this.router.navigate(['/serviceProvider']);
            break;
        }
        case 3: {
            this.router.navigate(['/user']);
            break;
        }
    }
  }
}
