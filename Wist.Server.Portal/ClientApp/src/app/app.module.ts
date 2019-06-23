import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { AccountsComponent } from './accounts/accounts.component';
import { LoginComponent } from './login/login.component';
import { httpInterceptor } from './interceptor/httpInterceptor';
import { ErrorInterceptor } from './interceptor/errorInterceptor';
import { RegisterComponent } from './register/register.component';
import { portalReducer } from './store/portal.reducers';
import { PortalEffects } from './store/portal.effects'

import { IdentityProviderBackModule } from './identities-provider-back/identityProviderBack.module'
import { AuthenticationModule } from './authentication/authentication.module'
import { QrCodeExModule } from './qrcode/qrcode.module';
import { IdentitiesProviderFrontModule } from './identities-provider-front/identities-provider-front.module';
import { UserModule } from './user/user.module';
import { ServiceProviderBackModule } from './service-provider-back/service-provider-back.module';
import { ServiceProviderFrontModule } from './service-provider-front/service-provider-front.module';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatExpansionModule, MatInputModule } from '@angular/material';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    AccountsComponent,
    LoginComponent,
    RegisterComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    MatExpansionModule, MatInputModule,
    QrCodeExModule,
    IdentityProviderBackModule,
    IdentitiesProviderFrontModule,
    UserModule,
    AuthenticationModule,
    ServiceProviderBackModule,
    ServiceProviderFrontModule,
    StoreModule.forRoot({ portal: portalReducer }),
    EffectsModule.forRoot([PortalEffects]),
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'accounts', component: AccountsComponent },
      { path: 'login/:id', component: LoginComponent },
      { path: 'register', component: RegisterComponent},
    ])
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: httpInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
