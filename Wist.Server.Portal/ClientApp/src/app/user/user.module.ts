import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatExpansionModule, MatInputModule, MatButtonToggleModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatDividerModule, MatProgressBarModule, MatListModule, MatStepperModule, MatCheckboxModule, MatRadioModule } from '@angular/material';

import { WebcamModule } from 'ngx-webcam';
import { QRCodeModule } from 'angularx-qrcode';
import { DeviceDetectorModule } from 'ngx-device-detector';
import { ZXingScannerModule } from '@zxing/ngx-scanner';

import { AuthorizationCheck } from '../authentication/authorizationCheck';
import { DuplicateUserComponent } from './duplicate-user/duplicate-user.component';
import { UserService } from './user.Service';
import { UserComponent } from './user.component';
import { UserClaimsComponent } from './user-claims/user-claims.component';
import { UserIdentityRequestComponent } from './user-identity-request/userIdentityRequest.component';
import { ServiceProviderLoginRegComponent } from './service-provider/service-provider.component';
import { QrCodeExModule } from '../qrcode/qrcode.module';
import { SignatureVerificationPopup } from './signature-verification-popup/signature-verification-popup.component';
import { QrCodePopupModule } from '../qrcode-popup/qrcode-popup.module';
import { GroupRelationsProofComponent } from './group-relations-proof/group-relations-proof.component';
import { RelationsValidationPopup } from './relations-validation-popup/relations-validation-popup.component';

@NgModule({
  declarations: [UserComponent, UserClaimsComponent, UserIdentityRequestComponent, ServiceProviderLoginRegComponent, DuplicateUserComponent, SignatureVerificationPopup, GroupRelationsProofComponent, RelationsValidationPopup],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    WebcamModule,
    QRCodeModule,
    QrCodePopupModule,
    DeviceDetectorModule.forRoot(),
    ZXingScannerModule,
    BrowserAnimationsModule,
    MatExpansionModule, MatInputModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatProgressBarModule, MatListModule, MatButtonToggleModule, MatDividerModule, MatStepperModule, MatCheckboxModule, MatRadioModule,
    QrCodeExModule,
    RouterModule.forRoot([
      { path: 'user', component: UserComponent, canActivate: [AuthorizationCheck] },
      { path: 'service-provider', component: ServiceProviderLoginRegComponent },
      { path: 'duplicate-user', component: DuplicateUserComponent, canActivate: [AuthorizationCheck] },
      { path: 'userIdentityRequest', component: UserIdentityRequestComponent, canActivate: [AuthorizationCheck] },
      { path: 'relationProofs', component: GroupRelationsProofComponent, canActivate: [AuthorizationCheck]}
    ])
  ],
  providers: [UserService],
  exports: [UserComponent],
  bootstrap: [UserComponent, UserClaimsComponent, UserIdentityRequestComponent, ServiceProviderLoginRegComponent, DuplicateUserComponent, SignatureVerificationPopup, GroupRelationsProofComponent, RelationsValidationPopup]
})
export class UserModule { }
