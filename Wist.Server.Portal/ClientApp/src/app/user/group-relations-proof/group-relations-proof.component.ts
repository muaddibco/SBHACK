import { Component, OnInit, QueryList } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { GroupRelation, UserService, UserAttributeDto } from '../user.Service';
import { WebcamImage, WebcamInitError, WebcamUtil } from 'ngx-webcam';
import { Subject, Observable } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSelectionListChange, MatListOption, MatCheckboxChange } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';

interface GroupRelationSelectable extends GroupRelation {
  isSelected: boolean;
}


@Component({
  templateUrl: 'group-relations-proof.component.html',
})
export class GroupRelationsProofComponent implements OnInit {
  public relations: GroupRelationSelectable[];
  public targetSpendKey: string;
  public targetViewKey: string;

  public userAttributes: UserAttributeDto[];
  public selectedAttribute: UserAttributeDto;

  public showWebcam = true;
  public allowCameraSwitch = true;
  public multipleWebcamsAvailable = false;
  public isCameraAvailable = false;
  public deviceId: string;
  public errors: WebcamInitError[] = [];
  public isErrorCam: boolean;
  public errorMsgCam: string;
  public skipCamera = false;
  public imageContent: string;
  // latest snapshot
  public webcamImage: WebcamImage = null;

  // webcam snapshot trigger
  private trigger: Subject<void> = new Subject<void>();
  // switch to next / previous / specific webcam; true/false: forward/backwards, string: deviceId
  private nextWebcam: Subject<boolean | string> = new Subject<boolean | string>();


  constructor(private userService: UserService, private router: Router, private route: ActivatedRoute, titleService: Title) {
    titleService.setTitle("Group Relations Proofs");
    this.relations = [];
  }

  ngOnInit() {
    const targets = this.route.snapshot.queryParams['actionInfo'].split('.');
    this.targetSpendKey = targets[0];
    this.targetViewKey = targets[1];

    this.getUserAttributes();
    this.initCam();

    this.userService.getGroupRelations().subscribe(r => {
      this.relations = r.map<GroupRelationSelectable>(g => { return { ...g, isSelected: false }; });
    });
  }

  private initCam() {
    WebcamUtil.getAvailableVideoInputs()
      .then((mediaDevices: MediaDeviceInfo[]) => {
        this.multipleWebcamsAvailable = mediaDevices && mediaDevices.length > 1;
        this.isCameraAvailable = mediaDevices && mediaDevices.length > 0;
        this.skipCamera = !mediaDevices || mediaDevices.length == 0;
      });
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

  clearSnapshot() {
    this.webcamImage = null;
  }

  onCancel() {
    this.router.navigate(['/user']);
  }

  onRelationSelectionChanged(evt: MatCheckboxChange, relation: GroupRelationSelectable) {
    relation.isSelected = evt.checked;
  }

  onSubmit() {
    console.log(this.relations);
    let selectedRelations: GroupRelation[] = this.relations.filter(v => {
      if (v.isSelected) {
        return v;
      }
    })

    console.log(selectedRelations);

    this.userService.sendRelationProofs({
      imageContent: this.showWebcam ? this.webcamImage.imageAsBase64 : null,
      target: this.targetSpendKey,
      targetViewKey: this.targetViewKey,
      relations: selectedRelations,
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
    })
      .subscribe(r => {
        this.router.navigate(['/user']);
      });
  }
}
