import { Component, Inject, OnInit} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService, UserAttributeTransferWithValidationsDto, UserAttributeDto, UserAttributeTransferDto } from '../user.Service';
import { HubConnection } from '@aspnet/signalr';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { WebcamImage, WebcamInitError, WebcamUtil } from 'ngx-webcam';
import { Subject, Observable } from 'rxjs';
import { MatButtonToggleChange } from '@angular/material';

@Component({
    selector: 'service-provider-login-reg',
    templateUrl: './service-provider.component.html',
    styleUrls: ['./service-provider.component.css']
})

/** serviceProviderLoginReg component*/
export class ServiceProviderLoginRegComponent implements OnInit {
  public userAttributes: UserAttributeDto[];
  public selectedAttributes: UserAttributeDto[];
  public selectedAttribute: UserAttributeDto;
  public publicKeyText: string;
  public sessionKeyText: string;
  public hubConnection: HubConnection;

  public validations: UserAttributeTransferWithValidationsDto;
  public withValidations: boolean = false;
  public validationEntries: string[];

  public withGroupSelection: boolean = false;
  public documentName: string;
  public documentHash: string;
  public documentRecordHeight: string;
  public groupEntries: string[];
  public selectedGroupEntry: string;

  public accountId: string;
  public confirmForm: FormGroup;
  public submitted = false;
  public submitClick = false;
  public error = '';

  public skipCameraCompleted = false;
  public showWebcam = true;
  public allowCameraSwitch = true;
  public multipleWebcamsAvailable = false;
  public isCameraAvailable = false;
  public deviceId: string;
  public errors: WebcamInitError[] = [];
  public isErrorCam: boolean;
  public errorMsgCam: string;
  public skipCamera = false;
  public toggleCameraText: string;
  public imageContent: string;
  // latest snapshot
  public webcamImage: WebcamImage = null;

  // webcam snapshot trigger
  private trigger: Subject<void> = new Subject<void>();
  // switch to next / previous / specific webcam; true/false: forward/backwards, string: deviceId
  private nextWebcam: Subject<boolean | string> = new Subject<boolean | string>();


  public action: string;
  public target: string;
  public sessionKey: string;
  public extraInfo: string;

    /** serviceProviderLoginReg ctor */
  constructor(private httpClient: HttpClient, @Inject('BASE_URL') private baseUrl: string, private route: ActivatedRoute, private formBuilder: FormBuilder, private userService: UserService, private router: Router, titleService: Title) {
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    this.accountId = tokenInfo.accountId;
    this.action = 'Submit';
    this.validations = new UserAttributeTransferWithValidationsDto();
    this.validations.userAttributeTransfer = new UserAttributeTransferDto();
    titleService.setTitle(tokenInfo.accountInfo);

    this.toggleCameraText = "Turn camera off";
  }

  ngOnInit() {
    this.action = this.route.snapshot.queryParams['action'];
    var actionInfo = this.route.snapshot.queryParams['target'];
    
    this.getUserAttributes();

    this.processActionInfo(actionInfo);

    this.initCam();
  }

  private initCam() {
    WebcamUtil.getAvailableVideoInputs()
      .then((mediaDevices: MediaDeviceInfo[]) => {
        this.multipleWebcamsAvailable = mediaDevices && mediaDevices.length > 1;
        this.isCameraAvailable = mediaDevices && mediaDevices.length > 0;
        this.skipCamera = !mediaDevices || mediaDevices.length == 0;
      });
  }

  private processActionInfo(actionInfo: string) {

    var actionInfoParts = actionInfo.split(":");

    this.target = actionInfoParts[0];
    this.sessionKey = actionInfoParts[1];
    this.extraInfo = actionInfoParts[2];

    if (actionInfoParts.length > 3) {
      if (this.action != "Sign Document") {
        this.validationEntries = actionInfoParts[3].split("|");
        this.confirmForm = this.formBuilder.group({
          password: ['', Validators.required]
        });
        this.withValidations = true;
      }
      else {
        this.groupEntries = actionInfoParts[3].split("|");
        this.documentName = this.extraInfo.split("|")[0];
        this.documentHash = this.extraInfo.split("|")[1];
        this.documentRecordHeight = this.extraInfo.split("|")[2];
        this.withGroupSelection = true;
      }
    }
  }

  private getUserAttributes() {
    this.userService.getUserAttributes()
        .subscribe(r => {
            this.userAttributes = [];
            for (let item of r) {
                if (!item.isOverriden) {
                    this.userAttributes.push(item);
                }
            }
        });
  }

  onChange(userAttribute) {
    if (userAttribute != null && this.selectedAttributes.find(t => t == userAttribute)) {
      this.selectedAttributes.push(userAttribute);
    }
  }

  selectionChange(input: HTMLInputElement, attr) {
    if (input.checked === true) {
      this.selectedAttribute = attr;
    }
  }

  submit() {
    this.processCameraCapture();

    if (this.action === 'Authorize') {
      this.autorize();
    }
    else if (this.action === 'Register') {
      this.onBoard();
    }
    else if (this.action === 'Register Employee') {
      this.employeeRequest();
    }
    else if (this.action === 'Sign Document') {
      this.documentSignRequest();
    }
  }

    private processCameraCapture() {
        if (!this.skipCamera) {
            if (this.webcamImage == null) {
                this.errorMsgCam = "No image captured!";
                this.isErrorCam = true;
            }
            else {
                this.imageContent = this.webcamImage.imageAsBase64;
            }
        }
        else {
            this.imageContent = null;
        }
    }

    private onBoard() {
        this.httpClient.post<boolean>(this.baseUrl + 'User/SendOnboardingRequest', {
            assetId: this.selectedAttribute.assetId,
            attributeType: this.selectedAttribute.attributeType,
            content: this.selectedAttribute.content,
            isOverriden: this.selectedAttribute.isOverriden,
            lastBlindingFactor: this.selectedAttribute.lastBlindingFactor,
            lastCommitment: this.selectedAttribute.lastCommitment,
            lastDestinationKey: this.selectedAttribute.lastDestinationKey,
            lastTransactionKey: this.selectedAttribute.lastTransactionKey,
            originalBlindingFactor: this.selectedAttribute.originalBlindingFactor,
            originalCommitment: this.selectedAttribute.originalCommitment,
            validated: this.selectedAttribute.validated,
            source: this.selectedAttribute.source,
            target: this.target,
            payload: this.sessionKey,
            imageContent: this.imageContent
        }).
        subscribe(r => {
          if (r) {
              this.router.navigate(['/user']);
          }
        });
    }

  private employeeRequest() {
    this.httpClient.post<boolean>(this.baseUrl + 'User/SendEmployeeRequest', {
      assetId: this.selectedAttribute.assetId,
      attributeType: this.selectedAttribute.attributeType,
      content: this.selectedAttribute.content,
      isOverriden: this.selectedAttribute.isOverriden,
      lastBlindingFactor: this.selectedAttribute.lastBlindingFactor,
      lastCommitment: this.selectedAttribute.lastCommitment,
      lastDestinationKey: this.selectedAttribute.lastDestinationKey,
      lastTransactionKey: this.selectedAttribute.lastTransactionKey,
      originalBlindingFactor: this.selectedAttribute.originalBlindingFactor,
      originalCommitment: this.selectedAttribute.originalCommitment,
      validated: this.selectedAttribute.validated,
      source: this.selectedAttribute.source,
      target: this.target,
      payload: this.sessionKey,
      extraInfo: this.extraInfo,
      imageContent: this.imageContent
    }).
      subscribe(r => {
        if (r) {
          this.router.navigate(['/user']);
        }
      });
  }

  private documentSignRequest() {
    this.httpClient.post<boolean>(this.baseUrl + 'User/SendDocumentSignRequest', {
      assetId: this.selectedAttribute.assetId,
      attributeType: this.selectedAttribute.attributeType,
      content: this.selectedAttribute.content,
      isOverriden: this.selectedAttribute.isOverriden,
      lastBlindingFactor: this.selectedAttribute.lastBlindingFactor,
      lastCommitment: this.selectedAttribute.lastCommitment,
      lastDestinationKey: this.selectedAttribute.lastDestinationKey,
      lastTransactionKey: this.selectedAttribute.lastTransactionKey,
      originalBlindingFactor: this.selectedAttribute.originalBlindingFactor,
      originalCommitment: this.selectedAttribute.originalCommitment,
      validated: this.selectedAttribute.validated,
      source: this.selectedAttribute.source,
      target: this.target,
      payload: this.sessionKey,
      extraInfo: this.selectedGroupEntry.replace(";", "|") + "|" + this.documentHash + "|" + this.documentRecordHeight,
      imageContent: this.imageContent
    }).
      subscribe(r => {
        if (r) {
          this.router.navigate(['/user']);
        }
      });
  }

  private autorize() {
    console.log(this.action);
    this.httpClient.post<boolean>(this.baseUrl + 'User/SendAuthenticationRequest', {
      assetId: this.selectedAttribute.assetId,
      attributeType: this.selectedAttribute.attributeType,
      content: this.selectedAttribute.content,
      isOverriden: this.selectedAttribute.isOverriden,
      lastBlindingFactor: this.selectedAttribute.lastBlindingFactor,
      lastCommitment: this.selectedAttribute.lastCommitment,
      lastDestinationKey: this.selectedAttribute.lastDestinationKey,
      lastTransactionKey: this.selectedAttribute.lastTransactionKey,
      originalBlindingFactor: this.selectedAttribute.originalBlindingFactor,
      originalCommitment: this.selectedAttribute.originalCommitment,
      validated: this.selectedAttribute.validated,
      source: this.selectedAttribute.source,
      target: this.target,
      payload: this.sessionKey,
      imageContent: this.imageContent
    }).
      subscribe(r => {
        this.router.navigate(['/user']);
      });
  }

  get formData() { return this.confirmForm.controls; }

  onConfirmValidations() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.confirmForm.invalid) {
      return;
    }

    this.submitClick = true;

    this.processCameraCapture();

    this.validations.userAttributeTransfer.imageContent = this.imageContent;
    this.validations.password = this.formData.password.value;
    this.setUserAttributeTransfer();
    this.validations.validations = this.validationEntries;

    this.httpClient.post<boolean>(this.baseUrl + 'User/SendOnboardingWithValidationsRequest', this.validations).
      subscribe(r => {
        this.router.navigate(['/user']);
      });;
  }

  private setUserAttributeTransfer() {
        this.validations.userAttributeTransfer = new UserAttributeTransferDto();
        this.validations.userAttributeTransfer.assetId = this.selectedAttribute.assetId;
        this.validations.userAttributeTransfer.attributeType = this.selectedAttribute.attributeType;
        this.validations.userAttributeTransfer.content = this.selectedAttribute.content;
        this.validations.userAttributeTransfer.isOverriden = this.selectedAttribute.isOverriden;
        this.validations.userAttributeTransfer.lastBlindingFactor = this.selectedAttribute.lastBlindingFactor;
        this.validations.userAttributeTransfer.lastCommitment = this.selectedAttribute.lastCommitment;
        this.validations.userAttributeTransfer.lastDestinationKey = this.selectedAttribute.lastDestinationKey;
        this.validations.userAttributeTransfer.lastTransactionKey = this.selectedAttribute.lastTransactionKey;
        this.validations.userAttributeTransfer.originalBlindingFactor = this.selectedAttribute.originalBlindingFactor;
        this.validations.userAttributeTransfer.originalCommitment = this.selectedAttribute.originalCommitment;
        this.validations.userAttributeTransfer.validated = this.selectedAttribute.validated;
    this.validations.userAttributeTransfer.source = this.selectedAttribute.source;
    this.validations.userAttributeTransfer.target = this.target;
    this.validations.userAttributeTransfer.payload = this.sessionKey;
    }

  public triggerSnapshot(): void {
    this.trigger.next();
  }

  public toggleWebcam(): void {
    this.showWebcam = !this.showWebcam;
  }

  public showNextWebcam(directionOrDeviceId: boolean | string): void {
    this.nextWebcam.next(directionOrDeviceId);
  }

  public handleImage(webcamImage: WebcamImage): void {
    console.info('received webcam image', webcamImage);
    this.webcamImage = webcamImage;
  }

  public handleInitError(error: WebcamInitError): void {
    this.errors.push(error);
  }

  public cameraWasSwitched(deviceId: string): void {
    console.log('active device: ' + deviceId);
    this.deviceId = deviceId;
  }

  public get triggerObservable(): Observable<void> {
    return this.trigger.asObservable();
  }

  public get nextWebcamObservable(): Observable<boolean | string> {
    return this.nextWebcam.asObservable();
  }

  onToggleCamera() {
    this.skipCamera = !this.skipCamera;

    if (this.skipCamera) {
      this.toggleCameraText = "Turn camera on";
    }
    else {
      this.toggleCameraText = "Turn camera off";
    }
  }

  onCancel() {
    this.router.navigate(['/user']);
  }

  clearSnapshot() {
    this.webcamImage = null;
  }

  onSkipCameraChanged(evt: MatButtonToggleChange) {
    this.skipCameraCompleted = true;
  }
}


