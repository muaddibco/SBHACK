import { NgModule } from '@angular/core'
import { IdentityProvidersListComponent } from './identity-providers-list/identity-providers-list.component';
import { IdentityProviderFrontComponent } from './identity-provider/identity-provider-front.component';
import { IdentitiesProviderFrontService } from './identities-provider-front.service';
import { RouterModule } from '@angular/router';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { QrCodeExModule } from '../qrcode/qrcode.module';

@NgModule({
  declarations: [IdentityProvidersListComponent, IdentityProviderFrontComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    QrCodeExModule,
    RouterModule.forRoot([
      { path: 'identityProviders', component: IdentityProvidersListComponent },
      { path: 'ip/:id', component: IdentityProviderFrontComponent }
    ])
  ],
  providers: [IdentitiesProviderFrontService],
  exports: [IdentityProvidersListComponent, IdentityProviderFrontComponent],
  bootstrap: [IdentityProvidersListComponent, IdentityProviderFrontComponent]
})
export class IdentitiesProviderFrontModule { }
