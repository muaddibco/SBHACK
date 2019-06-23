import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { IdentitiesService } from './identities.service';
import { IdentityProviderComponent } from './identity-provider/identityProvider.component';
import { TransferIdentityComponent } from './transfer-identity/transfer-identity.component';
import { ViewUserIdentityComponent } from './view-user-identity/viewUserIdentity.component';
import { identityProviderBackReducer } from './store/identity-provider-back.reducers';
import { IdentityProviderBackEffects } from './store/identity-provider-back.effects'
import { AuthorizationCheck } from '../authentication/authorizationCheck';
import { AuthenticationModule } from '../authentication/authentication.module';
import { QrCodeExModule } from '../qrcode/qrcode.module';

@NgModule({
  declarations: [IdentityProviderComponent, TransferIdentityComponent, ViewUserIdentityComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    QrCodeExModule,
    AuthenticationModule,
    StoreModule.forRoot({ identityProviderBackEditor: identityProviderBackReducer }),
    EffectsModule.forRoot([IdentityProviderBackEffects]),
    RouterModule.forRoot([
      { path: 'transfer-identity/:id', component: TransferIdentityComponent, canActivate: [AuthorizationCheck] },
      { path: 'identityProvider', component: IdentityProviderComponent, canActivate: [AuthorizationCheck] },
      { path: 'view-identity/:id', component: ViewUserIdentityComponent, canActivate: [AuthorizationCheck] },
    ])
  ],
  providers: [IdentitiesService],
  exports: [IdentityProviderComponent, TransferIdentityComponent, ViewUserIdentityComponent],
  bootstrap: [IdentityProviderComponent, TransferIdentityComponent, ViewUserIdentityComponent]
})
export class IdentityProviderBackModule { }

