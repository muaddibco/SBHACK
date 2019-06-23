import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserService, UserAssociatedAttributeDto, UnauthorizedUseDto, DocumentSignatureVerification, GroupRelation, RelationProofsValidationResults } from './user.Service';
import { FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { first, window } from 'rxjs/operators';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { Title } from '@angular/platform-browser';
import { DomSanitizer } from '@angular/platform-browser';
import { DeviceDetectorService } from 'ngx-device-detector';
import { SignatureVerificationPopup } from './signature-verification-popup/signature-verification-popup.component'
import { MatDialog, MatBottomSheet } from '@angular/material';
import { QrCodePopupComponent } from '../qrcode-popup/qrcode-popup.component';
import { RelationsValidationPopup } from './relations-validation-popup/relations-validation-popup.component';


@Component({
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.scss']
})

export class UserComponent implements OnInit {

  public action: string;
  public actionError: string;
  public isActionError: boolean;
  public actionText: string = "";
  public target: string;
  public payload: string;

  public hubConnection: HubConnection;
  public spendKey: string;
  public viewKey: string;
  public pageTitle: string;
  public accountId: string;
  public isLoaded: boolean;
  public isCompromised: boolean;
  public userAssociatedAttributesLoaded: boolean;
  public userAssociatedAttributes: UserAssociatedAttributeDto[];
  public associatedIdentitiesForm: FormGroup;
  public imageContent: string;
  submitted = false;
  submitClick = false;
  error = '';
  public qrContent: string;
  public isMobile = false;
  public activateQrScanner = false;
  public device: MediaDeviceInfo;
  public devices: MediaDeviceInfo[];
  private selectedDevice: number;

  public unauthorizedUse: UnauthorizedUseDto;
  public groupRelations: GroupRelation[];

  constructor(
    private userService: UserService,
    private router: Router,
    private formBuilder: FormBuilder,
    titleService: Title,
    private sanitizer: DomSanitizer,
    private deviceService: DeviceDetectorService, public dialog: MatDialog, private bottomSheet: MatBottomSheet) {
    this.isLoaded = false;
    this.accountId = sessionStorage.getItem('AccountId');
    this.userAssociatedAttributesLoaded = false;
    this.unauthorizedUse = null;
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    titleService.setTitle(tokenInfo.accountInfo);
    this.imageContent = null;
    this.isMobile = true; //deviceService.isMobile() || deviceService.isTablet();
    this.groupRelations = [];
  }


  ngOnInit() {
    this.initializeHub();

    this.getUserDetails();

    this.getUserAssociatedAttributes();

    this.getGroupRelations();
  }

  private initializeHub() {
    this.hubConnection = new HubConnectionBuilder().withUrl("/identitiesHub").build();

    this.hubConnection.on("PushUnauthorizedUse", (i) => {
      this.unauthorizedUse = i as UnauthorizedUseDto;
      this.isCompromised = true;
    });

    this.hubConnection.on("PushGroupRelation", (i) => {
      const groupRelation = i as GroupRelation;
      this.groupRelations.push(groupRelation);
    });

    this.hubConnection.on("PushRelationValidation", (i) => {
      const validationResults = i as RelationProofsValidationResults;
      console.log(validationResults);
      const dialogRef = this.dialog.open(RelationsValidationPopup, { data: validationResults });
      dialogRef.afterClosed().subscribe(r => { });

    });

    this.hubConnection.start()
      .then(() => {
        console.log("Connection started");
        this.hubConnection.invoke("AddToGroup", this.accountId);
      })
      .catch(err => { console.error(err); });
  }

  private getUserDetails() {
        this.userService.getUserDetails().subscribe(r => {
            this.spendKey = r.publicSpendKey;
            this.viewKey = r.publicViewKey;
            this.pageTitle = r.accountInfo;
            this.qrContent = r.publicSpendKey + r.publicViewKey;
            this.isCompromised = r.isCompromised;
            this.isLoaded = true;
        });
    }

  private getUserAssociatedAttributes() {
    this.userService.getUserAssociatedAttributes().subscribe(r => {
      this.userAssociatedAttributes = [];
      this.associatedIdentitiesForm = this.formBuilder.group({});
      for (var attr of r) {
        if (attr.attributeType != '7') {
          this.userAssociatedAttributes.push(attr);
          const ctrl = new FormControl(attr.content);
          this.associatedIdentitiesForm.registerControl('associatedAttribute' + attr.attributeType, ctrl);
        }
        else {
          this.imageContent = attr.content;
        }
      }
      this.userAssociatedAttributesLoaded = true;
    });
  }

  private getGroupRelations() {
    this.userService.getGroupRelations().subscribe(r => {
      this.groupRelations = r;
    });
  }

  associatedAttrs(i: string) { return this.associatedIdentitiesForm.controls['associatedAttribute' + i]; }

  ProvideProofs(actionInfo: string) {
    this.router.navigate(['/relationProofs'], { queryParams: { actionInfo: actionInfo } });
  }

  ValidateDocumentSignature(actionInfo: string) {
    console.log("ValidateDocumentSignature: " + actionInfo);
    let parts = actionInfo.split('.');
    let publicKey = parts[0];
    let documentHash = parts[1];
    let documentRecordHeight = parts[2];
    let signatureRecordHeight = parts[3];

    this.userService.getDocumentSignatureVerification(publicKey, documentHash, documentRecordHeight, signatureRecordHeight).subscribe(r => {
      let ver = r as DocumentSignatureVerification;
      console.log(ver);
      const dialogRef = this.dialog.open(SignatureVerificationPopup, { data: r });
      dialogRef.afterClosed().subscribe(r => {  });
    });
  }

  SignDocumentAtServiceProvider(actionInfo: string) {
    this.router.navigate(['/service-provider'], { queryParams: { action: 'Sign Document', target: actionInfo} });
  }

  RegEmployeeAtServiceProvider(actionInfo: string) {
    this.router.navigate(['/service-provider'], { queryParams: { action: 'Register Employee', target: actionInfo } });
  }

  RegToServiceProvider(actionInfo: string) {
    this.router.navigate(['/service-provider'], { queryParams: { action: 'Register', target: actionInfo } });
  }

  LoginToServiceProvider(actionInfo: string) {
    this.router.navigate(['/service-provider'], { queryParams: { action: 'Authorize', target: actionInfo } });
  }

  onSubmitAssociatedAttributes() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.associatedIdentitiesForm.invalid) {
      return;
    }

    this.submitClick = true;

    for (var attr of this.userAssociatedAttributes) {
      attr.content = this.associatedAttrs(attr.attributeType).value;
    }

    this.userService.updateUserAssociatedAttributes(this.userAssociatedAttributes)
      .pipe(first())
      .subscribe(
      data => {
        this.submitted = false;
        this.submitClick = false;
      },
      error => {
        this.submitted = false;
        this.submitClick = false;
        this.error = error;
      });
  }

  copyQR() {
    let selBox = document.createElement('textarea');
    selBox.style.position = 'fixed';
    selBox.style.left = '0';
    selBox.style.top = '0';
    selBox.style.opacity = '0';
    selBox.value = this.qrContent;
    document.body.appendChild(selBox);
    selBox.focus();
    selBox.select();
    document.execCommand('copy');
    document.body.removeChild(selBox);
  }

  onqrCodeReaderinputChanged(evt) {
    if (evt.target.value != null) {
      evt.target.value = null;
    }
  }

  onPaste(evt: any) {
    var qrCode: string;
    
    qrCode = evt.clipboardData.getData('text/plain');
    this.isActionError = false;
    this.actionError = "";

    if (qrCode.length >= 64) {
      this.processActionType(qrCode);
    }

    evt.stopPropagation();
  }

  processActionType(qrCode: string) {
    console.log(qrCode);
    var that = this;
    this.userService.getActionType(qrCode).subscribe(r => {
      that.action = r.action;
      that.target = r.actionInfo;

      if (r.action == '1') { that.router.navigate(['/userIdentityRequest'], { queryParams: { target: r.actionInfo } }); }
      else if (r.action == '2') { that.RegToServiceProvider(r.actionInfo); }
      else if (r.action == '3') { that.LoginToServiceProvider(r.actionInfo); }
      else if (r.action == '4') { alert('You are not allowed to register as employee'); }
      else if (r.action == '5') { that.RegEmployeeAtServiceProvider(r.actionInfo); }
      else if (r.action == '6') { that.SignDocumentAtServiceProvider(r.actionInfo); }
      else if (r.action == '7') { that.ValidateDocumentSignature(r.actionInfo); }
      else if (r.action == '8') { that.ProvideProofs(r.actionInfo); }
    }, e => {
      (<HTMLInputElement>document.getElementById("qrCodeReader")).value = "";
      that.isActionError = true;
      that.actionError = e;
    });
  }

  sendCompromisedProofs() {
    this.userService.sendCompromisedProofs(this.unauthorizedUse).pipe(first()).subscribe(data => {},error => {});
  }

  sanitize(img: string) {
    return this.sanitizer.bypassSecurityTrustUrl(img);
  }

  scanSuccessHandler(qrCode: string) {
    if (qrCode.length >= 64) {
      this.processActionType(qrCode);
    }
  }

  camerasFoundHandler(cameras: MediaDeviceInfo[]) {
    let found = false;

    this.devices = cameras;
    this.selectedDevice = 0;
    for (let cameraInfo of cameras) {
      this.selectedDevice++;
      if (cameraInfo.label.includes("back")) {
        this.device = cameraInfo;
        found = true;
      }
    }
    this.selectedDevice--;

    if (!found) {
      this.device = cameras[cameras.length - 1];
      this.selectedDevice = cameras.length - 1;
    }
  }

  switchCamera() {
    console.log(this.selectedDevice);
    this.selectedDevice = (this.selectedDevice + 1) % this.devices.length;
    console.log(this.selectedDevice);
    console.log(this.device);
    this.device = this.devices[this.selectedDevice];
    console.log(this.device);
  }

  clearCompromised() {
    this.userService.clearCompromised().subscribe(r => {
      this.unauthorizedUse = null;
      this.isCompromised = false;
    });
  }

  onShowQrClick() {
    this.bottomSheet.open(QrCodePopupComponent, { data: { qrCode: btoa("addr://" + this.spendKey + "." + this.viewKey) } });
  }

  removeGroupRelation(groupRelation: GroupRelation) {
    if (confirm("Are you sure you want to remove the relation to group '" + groupRelation.groupName + "' of '" + groupRelation.groupOwnerName + "'?")) {
      this.userService.deleteGroupRelation(groupRelation.groupRelationId).subscribe(r => {
        this.removeItemFromGroupRelations(this.groupRelations, groupRelation);
      });
    }
  }

  removeItemFromGroupRelations(list: GroupRelation[], itemToRemove: GroupRelation) {
    let index = -1;
    let found = false;
    for (let item of list) {
      index++;
      if (item.groupRelationId == itemToRemove.groupRelationId) {
        found = true;
        break;
      }
    }

    if (found) {
      list.splice(index, 1);
    }
  }
}
