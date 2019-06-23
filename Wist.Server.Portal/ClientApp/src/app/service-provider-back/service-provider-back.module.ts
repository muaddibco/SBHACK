import { NgModule } from '@angular/core';
import { ServiceProviderComponent } from './serviceProvider.component';
import { QrCodeExModule } from '../qrcode/qrcode.module';
import { AuthorizationCheck } from '../authentication/authorizationCheck';
import { RouterModule } from '@angular/router';
import { ServiceProviderService } from './serviceProvider.service';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatExpansionModule, MatInputModule, MatTabsModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatDividerModule, MatProgressBarModule, MatListModule } from '@angular/material';
import { DialogAddValidationDialog } from './add-validation-dialod/add-validation-dialog.component';
import { QrCodePopupModule } from '../qrcode-popup/qrcode-popup.module';
import { DialogEmployeeGroupDialog } from './employee-group-dialog/employee-group-dialog.component';
import { DialogEmployeeRecordDialog } from './employee-record-dialog/employee-record-dialog.component';
import { DocumentDialog } from './document-dialog/document-dialog.component';
import { QRCodeModule } from 'angularx-qrcode';
import { AllowedSignerDialog } from './allowed-signer-dialog/allowed-signer-dialog.component';

@NgModule({
  declarations: [ServiceProviderComponent, DialogAddValidationDialog, DialogEmployeeGroupDialog, DialogEmployeeRecordDialog, DocumentDialog, AllowedSignerDialog],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    MatExpansionModule, MatInputModule, MatTabsModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatDividerModule, MatProgressBarModule, MatListModule,
    QrCodeExModule,
    QrCodePopupModule,
    QRCodeModule,
    RouterModule.forRoot([
      { path: 'serviceProvider', component: ServiceProviderComponent, canActivate: [AuthorizationCheck] }
    ])
  ],
  providers: [ServiceProviderService],
  bootstrap: [ServiceProviderComponent, DialogAddValidationDialog, DialogEmployeeGroupDialog, DialogEmployeeRecordDialog, DocumentDialog, AllowedSignerDialog]
})
export class ServiceProviderBackModule { }
