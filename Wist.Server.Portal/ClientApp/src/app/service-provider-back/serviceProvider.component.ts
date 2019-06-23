import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder } from '@angular/forms';
import { SpAttributeDto, ServiceProviderRegistrationDto, IdentityAttributeValidationDescriptorDto, IdentityAttributeValidationDefinitionClassDto, ServiceProviderService, EmployeeGroup, EmployeeRecord, SpDocument, AllowedSigner } from './serviceProvider.service';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { first } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { MatDialog, MatBottomSheet } from '@angular/material';
import { DialogAddValidationDialog } from './add-validation-dialod/add-validation-dialog.component';
import { QrCodePopupComponent } from '../qrcode-popup/qrcode-popup.component';
import { DialogEmployeeGroupDialog } from './employee-group-dialog/employee-group-dialog.component';
import { DialogEmployeeRecordDialog } from './employee-record-dialog/employee-record-dialog.component';
import { DocumentDialog } from './document-dialog/document-dialog.component';
import { AllowedSignerDialog } from './allowed-signer-dialog/allowed-signer-dialog.component';
import { all } from 'q';

@Component({
  templateUrl: './serviceProvider.component.html'
})

export class ServiceProviderComponent implements OnInit {
  public pageTitle: string;
  public accountId: string;
  public registrations: ServiceProviderRegistrationDto[];
  public spAttributes: SpAttributeDto[];
  public identityAttributeValidationDescriptors: IdentityAttributeValidationDescriptorDto[];
  public identityAttributeValidationDefinitions: IdentityAttributeValidationDefinitionClassDto[];
  public employeeGroups: EmployeeGroup[];
  public employees: EmployeeRecord[];
  public documents: SpDocument[];

  public isIdentityAttributeValidationDefinitionsDirty: boolean;

  public areRegistrationsLoaded: boolean;
  public areValidationsLoaded: boolean;
  public areEmployeesGroupsLoaded: boolean;
  public areEmployeesLoaded: boolean;
  public areDocumentsLoaded: boolean;

  public target: string;
  public isQrCodeLoaded: boolean;

  public attributeTypes: string[];
  public hubConnection: HubConnection;

  constructor(private http: HttpClient, private serviceProviderService: ServiceProviderService, private formBuilder: FormBuilder, titleService: Title, public dialog: MatDialog, private bottomSheet: MatBottomSheet) {
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    this.accountId = tokenInfo.accountId;
    this.pageTitle = tokenInfo.accountInfo;
    this.areRegistrationsLoaded = false;
    this.areValidationsLoaded = false;
    this.areEmployeesGroupsLoaded = false;
    this.areEmployeesLoaded = false;
    this.areDocumentsLoaded = false;
    this.attributeTypes = [];
    this.spAttributes = [];
    this.registrations = [];
    this.identityAttributeValidationDescriptors = [];
    this.identityAttributeValidationDefinitions = [];
    this.isIdentityAttributeValidationDefinitionsDirty = false;
    this.isQrCodeLoaded = false;
    titleService.setTitle(tokenInfo.accountInfo + " Back Office");
  }

  ngOnInit() {
    var accountId = sessionStorage.getItem('AccountId');

    this.initializeHub(accountId);

    var that = this;
    this.serviceProviderService.getServiceProvider(this.accountId).subscribe((r) => {
      that.target = r.target;
      that.isQrCodeLoaded = true;
    });

    this.serviceProviderService.getRegistrations().subscribe((r) => {
      that.registrations = r;
      that.areRegistrationsLoaded = true;
    });

    this.serviceProviderService.getEmployeeGroups().subscribe((r) => {
      that.employeeGroups = r;
      that.areEmployeesGroupsLoaded = true;
    });

    this.serviceProviderService.getEmployees().subscribe((r) => {
      that.employees = r;
      that.areEmployeesLoaded = true;
    });

    this.serviceProviderService.getSpDocuments().subscribe((r) => {
      that.documents = r;
      that.areDocumentsLoaded = true;
    });

    this.initializeValidations();
  }

  private initializeValidations() {
    var that = this;
    this.serviceProviderService.getIdentityAttributeValidationDescriptors().subscribe((r) => {
      that.identityAttributeValidationDescriptors = r;
      for (let validationDesc of that.identityAttributeValidationDescriptors) {
        that.attributeTypes.push(validationDesc.attributeTypeName);
      }
      that.serviceProviderService.getIdentityAttributeValidations().subscribe((r) => {
        for (let validation of r) {
          var validationClass = new IdentityAttributeValidationDefinitionClassDto();
          validationClass.attributeType = validation.attributeType;
          validationClass.attributeTypeName = that.getAttributeTypeName(validation.attributeType);
          validationClass.validationType = validation.validationType;
          validationClass.validationTypeName = that.getValidationTypeName(validation.validationType);
          validationClass.criterionValue = validation.criterionValue;
          that.identityAttributeValidationDefinitions.push(validationClass);
        }
        that.areValidationsLoaded = true;
      });
    });
    return that;
  }

  private initializeHub(accountId: string) {
    this.hubConnection = new HubConnectionBuilder().withUrl("/identitiesHub").build();
    this.hubConnection.on("PushRegistration", (i) => {
      this.registrations.push(i);
    });
    this.hubConnection.on("PushAttribute", (i) => {
      this.spAttributes.push(i);
    });
    this.hubConnection.on("PushEmployeeUpdate", (i) => {
      for (let employee of this.employees) {
        if (employee.assetId === i.assetId) {
          employee.registrationCommitment = i.registrationCommitment;
        }
      }
    });
    this.hubConnection.start()
      .then(() => {
        console.log("Connection started");
        this.hubConnection.invoke("AddToGroup", accountId);
      })
      .catch(err => { console.error(err); });
  }

  getAttributeTypeName(attributeType: string) {
    for (let validationDesc of this.identityAttributeValidationDescriptors) {
      if (validationDesc.attributeType == attributeType) {
        return validationDesc.attributeTypeName;
      }
    }

    return "";
  }

  getValidationTypeName(validationType: string) {
    for (let validationDesc of this.identityAttributeValidationDescriptors) {
      if (validationDesc.validationType == validationType) {
        return validationDesc.validationTypeName;
      }
    }

    return "";
  }

  openAddValidationDialog(): void {
    const dialogRef = this.dialog.open(DialogAddValidationDialog, {
      width: '400px',
      data: { identityAttributeValidationDescriptors: this.identityAttributeValidationDescriptors, attributeTypes: this.attributeTypes }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
      var validationDefinition = new IdentityAttributeValidationDefinitionClassDto();
      validationDefinition.attributeType = result.selectedAttributeType;
      validationDefinition.attributeTypeName = this.getAttributeTypeName(result.selectedAttributeType);
      if (result.selectedAttributeType == "4") {
        validationDefinition.validationType = "2";
        validationDefinition.criterionValue = result.ageRestriction;
      }
      else {
        validationDefinition.validationType = "1";
      }

      validationDefinition.validationTypeName = this.getValidationTypeName(validationDefinition.validationType);

      this.identityAttributeValidationDefinitions.push(validationDefinition);

      this.isIdentityAttributeValidationDefinitionsDirty = true;
    });
  }

  removeEmployeeGroup(employeeGroup: EmployeeGroup) {
    const that = this;
    if (confirm("Are you sure you want to remove Employee Group " + employeeGroup.groupName + "?")) {
      this.serviceProviderService.deleteEmployeeGroup(employeeGroup.groupId).subscribe(r => {
        that.removeEmployeeGroupFromList(that.employeeGroups, employeeGroup);
      });
    }
  }

  openAddEmployeeGroupDialog(): void {
    const dialogRef = this.dialog.open(DialogEmployeeGroupDialog, { width: '400px', data: { isAdding: true } });
    const that = this;
    dialogRef.afterClosed().subscribe(result => {
      this.serviceProviderService.addEmployeeGroup({ groupName: result.groupName, groupId: 0 })
        .subscribe(r => { that.employeeGroups.push(r) });
    });
  }

  onUpdateValidations() {
    this.serviceProviderService.saveIdentityAttributeValidations(this.identityAttributeValidationDefinitions).pipe(first())
      .subscribe(
        data => {
          
        },
        error => {
        });
    this.isIdentityAttributeValidationDefinitionsDirty = false;
  }

  onShowQrClick() {
    this.bottomSheet.open(QrCodePopupComponent, { data: { qrCode: this.target } });
  }

  removeEmployeeGroupFromList(list: EmployeeGroup[], itemToRemove: EmployeeGroup) {
    let index = -1;
    let found = false;
    for (let item of list) {
      index++;
      if (item.groupId == itemToRemove.groupId) {
        found = true;
        break;
      }
    }

    if (found) {
      list.splice(index, 1);
    }
  }

  getGroupName(groupId: number) {
    if (groupId === 0) {
      return '';
    }

    let group = this.employeeGroups.find((v, i, o) => v.groupId === groupId);

    return group != null ? group.groupName : '';
  }

  newEmployee() {
    const dialogRef = this.dialog.open(DialogEmployeeRecordDialog, {
      width: '400px', data: {
        isAdding: true,
        groups: this.employeeGroups
      }
    });

    const that = this;
    dialogRef.afterClosed().subscribe(result => {
      this.serviceProviderService.addEmployee({ employeeId: 0, description: result.description, rawRootAttribute: result.rawRootAttribute, assetId: null, groupId: result.groupId, registrationCommitment:null }).subscribe(r => {
        that.employees.push(r);
      });
    });

  }

  editEmployee(employee: EmployeeRecord) {
    const dialogRef = this.dialog.open(DialogEmployeeRecordDialog, {
      width: '400px', data: {
        employeeId: employee.employeeId, description: employee.description, rawRootAttribute: employee.rawRootAttribute, groupId: employee.groupId, registrationCommitment: employee.registrationCommitment, isAdding: false, groups: this.employeeGroups, model: employee
      }
    });

    const that = this;
    dialogRef.afterClosed().subscribe(result => {
      let employeeUpdated = result.model as EmployeeRecord;

      employeeUpdated.description = result.description;
      employeeUpdated.groupId = result.groupId;
      employeeUpdated.rawRootAttribute = result.rawRootAttribute;
      
      this.serviceProviderService.updateEmployee(employeeUpdated).subscribe(r => { });
    });

  }

  removeEmployee(employee: EmployeeRecord) {
    const that = this;
    if (confirm("Are you sure you want to remove Employee " + employee.description + "?")) {
      this.serviceProviderService.deleteEmployee(employee.employeeId).subscribe(r => {
        that.removeEmployeeFromList(that.employees, employee);
      });
    }
  }

  removeEmployeeFromList(list: EmployeeRecord[], itemToRemove: EmployeeRecord) {
    let index = -1;
    let found = false;
    for (let item of list) {
      index++;
      if (item.employeeId == itemToRemove.employeeId) {
        found = true;
        break;
      }
    }

    if (found) {
      list.splice(index, 1);
    }
  }

  openAddNewDocumentDialog() {
    const dialogRef = this.dialog.open(DocumentDialog, { width: '400px' });

    const that = this;
    dialogRef.afterClosed().subscribe(result => {
      that.serviceProviderService.addSpDocument({ documentId: 0, documentName: result.documentName, hash: result.hash, allowedSigners: null, signatures: null }).subscribe((r) => {
        that.documents.push(r);
      });
    });
  }

  openAddAllowedSignerDialog(doc: SpDocument) {
    const dialogRef = this.dialog.open(AllowedSignerDialog, { width: '400px', data: { documentId: doc.documentId } });

    const that = this;
    dialogRef.afterClosed().subscribe(result => {
      that.serviceProviderService.addAllowedSigner(result.documentId, { allowedSignerId: 0, groupOwner: result.owner, groupName: result.groupName }).subscribe((r) => {
        for (let doc of that.documents) {
          if (doc.documentId === result.documentId) {
            doc.allowedSigners.push(r);
          }
        }
      });
    });
  }

  removeAllowedSigner(doc: SpDocument, allowedSigner: AllowedSigner) {
    const that = this;
    if (confirm("Are you sure you want to remove Allowed Signer " + allowedSigner.groupOwner + " : " + allowedSigner.groupName + "?")) {
      this.serviceProviderService.deleteAllowedSigner(allowedSigner.allowedSignerId).subscribe(r => {
        that.removeAllowedSignerFromList(doc.allowedSigners, allowedSigner);
      });
    }
  }

  removeAllowedSignerFromList(list: AllowedSigner[], itemToRemove: AllowedSigner) {
    let index = -1;
    let found = false;
    for (let item of list) {
      index++;
      if (item.allowedSignerId == itemToRemove.allowedSignerId) {
        found = true;
        break;
      }
    }

    if (found) {
      list.splice(index, 1);
    }
  }
}
