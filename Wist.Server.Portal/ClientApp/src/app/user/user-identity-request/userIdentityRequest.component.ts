import { Component, OnInit } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { WebcamImage, WebcamInitError, WebcamUtil } from 'ngx-webcam';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UserService } from '../user.Service';
import { Title } from '@angular/platform-browser';

@Component({
  templateUrl: './userIdentityRequest.component.html',
})

export class UserIdentityRequestComponent implements OnInit {

  public pageTitle: string;
  public showWebcam = true;
  public allowCameraSwitch = true;
  public multipleWebcamsAvailable = false;
  public isCameraAvailable = false;
  public deviceId: string;
  public errors: WebcamInitError[] = [];
  public requestIdentityForm: FormGroup;
  public submitted = false;
  public submitClick = false;
  public isError: boolean;
  public errorMsg: string;
  public skipCamera = false;
  public toggleCameraText: string;
  public target: string;

  constructor(private service: UserService, private route: ActivatedRoute, private router: Router, private formBuilder: FormBuilder, titleService: Title) {
    this.isError = false;
    this.errorMsg = "";
    this.toggleCameraText = "Turn camera off";
    this.target = this.route.snapshot.queryParams['target'];
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    this.pageTitle = tokenInfo.accountInfo;
    titleService.setTitle(tokenInfo.accountInfo);
  }


  // latest snapshot
  public webcamImage: WebcamImage = null;

  // webcam snapshot trigger
  private trigger: Subject<void> = new Subject<void>();
  // switch to next / previous / specific webcam; true/false: forward/backwards, string: deviceId
  private nextWebcam: Subject<boolean | string> = new Subject<boolean | string>();

  ngOnInit() {
    this.requestIdentityForm = this.formBuilder.group({
      idCard: ['', Validators.required],
      password: ['', Validators.required]
    });

    WebcamUtil.getAvailableVideoInputs()
      .then((mediaDevices: MediaDeviceInfo[]) => {
        this.multipleWebcamsAvailable = mediaDevices && mediaDevices.length > 1;
        this.isCameraAvailable = mediaDevices && mediaDevices.length > 0;
        this.skipCamera = !mediaDevices || mediaDevices.length == 0;
      });
  }

  get formData() { return this.requestIdentityForm.controls; }

  onSubmitSending() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.requestIdentityForm.invalid) {
      return;
    }

    this.submitClick = true;

    var imageContent = "";

    if (!this.skipCamera) {
      if (this.webcamImage == null) {
        this.errorMsg = "No image captured!"
        this.isError = true;
      }
      else {
        imageContent = this.webcamImage.imageAsBase64;
      }
    }

    this.service.requestIdentity(this.target, this.formData.idCard.value, this.formData.password.value, imageContent)
      .subscribe(a => {
        this.router.navigate(['/user']);
      },
      err => {
        this.isError = true;
        this.errorMsg = err;
        });
  }

  public triggerSnapshot(): void {
    this.trigger.next();
  }

  public toggleWebcam(): void {
    this.showWebcam = !this.showWebcam;
  }

  public showNextWebcam(directionOrDeviceId: boolean | string): void {
    // true => move forward through devices
    // false => move backwards through devices
    // string => move to device with given deviceId
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

  onCancel() {
    this.router.navigate(['/user']);
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

  clearSnapshot() {
    this.webcamImage = null;
  }
}
