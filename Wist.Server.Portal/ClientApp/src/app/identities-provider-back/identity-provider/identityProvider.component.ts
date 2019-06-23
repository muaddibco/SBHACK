import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { first } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { IdentityDto, IdentityAttributesSchemaDto, IdentityClassDto, IdentityAttributeClassDto, IdentitiesService } from '../identities.service';

enum ClaimSubject {
  User = 1,
  ServiceProvider = 2
}

@Component({
  templateUrl: './identityProvider.component.html'
})

export class IdentityProviderComponent implements OnInit {

  public pageTitle: string;
  public isLoaded: boolean;
  public accountId: string;
  public hubConnection: HubConnection;
  public identities: IdentityDto[];
  public isAdding: boolean;
  public claimSubjects: string[];
  public selectedClaimSubject: ClaimSubject;
  public publicKey: string;
  public identityAttributesSchema: IdentityAttributesSchemaDto;
  addIdentityForm: FormGroup;
  submitted = false;
  submitClick = false;
  error = '';
  public imageContent: string | ArrayBuffer;


  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private formBuilder: FormBuilder, private identityService: IdentitiesService, titleService: Title)
  {
    this.isAdding = false;
    this.isLoaded = false;
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    this.accountId = tokenInfo.accountId;
    this.pageTitle = tokenInfo.accountInfo;
    this.publicKey = btoa(baseUrl + "IdentityProvider/ProcessRootIdentityRequest/" + tokenInfo.publicSpendKey);
    titleService.setTitle(tokenInfo.accountInfo + " Back Office");
  }

  ngOnInit() {

    this.setClaimSubjects();

    this.getAllIdentities();

    this.getIdentityAttributesSchema();

    this.initializeHub();
  }

  private getIdentityAttributesSchema() {
        this.identityService.getIdentityAttributesSchema().subscribe(r => {
            this.identityAttributesSchema = r;
            this.addIdentityForm = this.formBuilder.group({
                identityDescription: ['', Validators.required],
                rootAttribute: ['', Validators.required]
            });
            for (var attr of this.identityAttributesSchema.associatedAttributes) {
                const ctrl = new FormControl('', Validators.required);
                this.addIdentityForm.registerControl('associatedAttribute' + attr.attributeType, ctrl);
            }
        });
    }

  private setClaimSubjects() {
        var options = Object.keys(ClaimSubject);
        this.claimSubjects = options.slice(options.length / 2);
    }

    private getAllIdentities() {
        this.http.get<IdentityDto[]>(this.baseUrl + 'IdentityProvider/GetAllIdentities/' + this.accountId).subscribe(r => {
            this.identities = r;
            this.isLoaded = true;
        });
    }

    private initializeHub() {
        this.hubConnection = new HubConnectionBuilder().withUrl("/identitiesHub").build();
        this.hubConnection.on("PushIdentity", (i) => {
            this.identities.push(i);
        });
        this.hubConnection.start()
            .then(() => {
                console.log("Connection started");
                this.hubConnection.invoke("AddToGroup", this.accountId);
            })
            .catch(err => { console.error(err); });
    }

  get formData() { return this.addIdentityForm.controls; }

  associatedAttrs(i: string) { return this.addIdentityForm.controls['associatedAttribute' + i]; }

  parseClaimSubject(value: string) {
    this.selectedClaimSubject = ClaimSubject[value];
  }

  addIdentity() {
    this.isAdding = true;
  }

  onSubmitAdding() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.addIdentityForm.invalid) {
      return;
    }

    this.submitClick = true;

    var identity = this.createIdentity();

    this.processAssociatedAttrs(identity);

    this.addIdentityDto(identity);

  }

    private createIdentity() {
        var identity = new IdentityClassDto();
        identity.description = this.formData.identityDescription.value;
        identity.rootAttribute = new IdentityAttributeClassDto();
        identity.rootAttribute.attributeType = this.identityAttributesSchema.rootAttribute.attributeType;
        identity.rootAttribute.content = this.formData.rootAttribute.value;
        identity.associatedAttributes = new Array<IdentityAttributeClassDto>();
        return identity;
    }

    private processAssociatedAttrs(identity: IdentityClassDto) {
        for (var attrSchema of this.identityAttributesSchema.associatedAttributes) {
            var associatedAttr = new IdentityAttributeClassDto();
            associatedAttr.attributeType = attrSchema.attributeType;
            associatedAttr.content = this.associatedAttrs(attrSchema.attributeType).value;
            identity.associatedAttributes.push(associatedAttr);
        }
    }

  private addIdentityDto(identity: IdentityClassDto) {
        this.identityService.addIdentity(identity)
            .pipe(first())
            .subscribe(data => {
                this.submitClick = false;
                this.isAdding = false;
                this.error = '';
            }, error => {
                this.submitClick = false;
                this.isAdding = false;
                this.error = 'Failed to create identity';
            });
    }

  onCancelAdding() {
    this.isAdding = false;
  }

  onSendRootAttribute() {

  }

  onFileChange(event) {
    const reader = new FileReader();

    if (event.target.files && event.target.files.length) {
      const [file] = event.target.files;
      reader.readAsDataURL(file);
      
      reader.onload = () => {
        this.imageContent = reader.result;
        console.log(this.imageContent);
      };
    }
  }
}
