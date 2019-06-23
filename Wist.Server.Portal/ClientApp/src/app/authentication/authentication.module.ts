import { NgModule } from '@angular/core';

import { AuthorizationCheck } from './authorizationCheck';
import { AuthenticationService } from './authentication.service';

@NgModule({
  providers: [AuthorizationCheck, AuthenticationService],
})
export class AuthenticationModule { }
