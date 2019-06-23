import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatExpansionModule, MatInputModule, MatTabsModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatDividerModule, MatProgressBarModule, MatListModule, MatGridListModule } from '@angular/material';

import { ServiceProvidersComponent } from './service-providers-list/serviceProviders.component';
import { SpComponent } from './service-provider/sp.component';
import { SpLoginHomeComponent } from './service-provider-inside/spLoginHome.component';
import { ServiceProviderFrontService } from './service-provider-front.service';
import { QrCodeExModule } from '../qrcode/qrcode.module';
import { AuthorizationCheck } from '../authentication/authorizationCheck';
import { NotificationPopupModule } from '../notification-popup/notification-popup.module';

@NgModule({
  declarations: [ServiceProvidersComponent, SpComponent, SpLoginHomeComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatExpansionModule, MatInputModule, MatTabsModule, MatSelectModule, MatDialogModule, MatButtonModule, MatBottomSheetModule, MatCardModule, MatIconModule, MatDividerModule, MatProgressBarModule, MatListModule, MatGridListModule,
    QrCodeExModule,
    NotificationPopupModule,
    RouterModule.forRoot([
      { path: 'serviceProviders', component: ServiceProvidersComponent },
      { path: 'sp/:id', component: SpComponent },
      { path: 'spLoginHome', component: SpLoginHomeComponent, canActivate: [AuthorizationCheck] },
])
  ],
  providers: [ServiceProviderFrontService],
  bootstrap: [ServiceProvidersComponent, SpComponent, SpLoginHomeComponent]
})
export class ServiceProviderFrontModule { }
