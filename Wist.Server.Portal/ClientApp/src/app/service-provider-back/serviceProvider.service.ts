import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class ServiceProviderService {
  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  getServiceProvider(id: string) {
    return this.http.get<ServiceProviderInfoDto>(this.baseUrl + "ServiceProviders/ById/" + id);
  }

  getRegistrations() {
    return this.http.get<ServiceProviderRegistrationDto[]>(this.baseUrl + "ServiceProviders/GetRegistrations");
  }

  getIdentityAttributeValidationDescriptors() {
    return this.http.get<IdentityAttributeValidationDescriptorDto[]>(this.baseUrl + "ServiceProviders/GetIdentityAttributeValidationDescriptors");
  }
  
  getIdentityAttributeValidations() {
    return this.http.get<IdentityAttributeValidationDefinitionDto[]>(this.baseUrl + "ServiceProviders/GetIdentityAttributeValidations");
  }

  saveIdentityAttributeValidations(identityAttributeValidationDefinitions: IdentityAttributeValidationDefinitionDto[]) {
    return this.http.post<any>("/ServiceProviders/UpdateIdentityAttributeValidationDefinitions", { identityAttributeValidationDefinitions });
  }

  getEmployeeGroups() {
    return this.http.get<EmployeeGroup[]>("/ServiceProviders/GetEmployeeGroups");
  }

  addEmployeeGroup(employeeGroup: EmployeeGroup) {
    return this.http.post<EmployeeGroup>("/ServiceProviders/AddEmployeeGroup", employeeGroup);
  }

  deleteEmployeeGroup(groupId: number) {
    return this.http.delete("/ServiceProviders/DeleteEmployeeGroup/" + groupId.toString());
  }

  getEmployees() {
    return this.http.get<EmployeeRecord[]>("/ServiceProviders/GetEmployees");
  }

  addEmployee(employee: EmployeeRecord) {
    return this.http.post<EmployeeRecord>("/ServiceProviders/AddEmployee", employee);
  }

  updateEmployee(employee: EmployeeRecord) {
    return this.http.post<EmployeeRecord>("/ServiceProviders/UpdateEmployee", employee);
  }

  deleteEmployee(employeeId: number) {
    return this.http.delete("/ServiceProviders/DeleteEmployee/" + employeeId.toString());
  }

  getSpDocuments() {
    return this.http.get<SpDocument[]>("/ServiceProviders/GetDocuments");
  }

  addSpDocument(document: SpDocument) {
    return this.http.post<SpDocument>("/ServiceProviders/AddDocument", document);
  }

  deleteSpDocument(documentId: number) {
    return this.http.delete("/ServiceProviders/DeleteDocument/" + documentId.toString());
  }

  addAllowedSigner(documentId: number, allowedSigner: AllowedSigner) {
    return this.http.post<AllowedSigner>("/ServiceProviders/AddAllowedSigner/" + documentId.toString(), allowedSigner);
  }

  deleteAllowedSigner(allowedSignerId: number) {
    return this.http.delete("/ServiceProviders/DeleteAllowedSigner/" + allowedSignerId.toString());
  }
}

export interface ServiceProviderRegistrationDto {
  serviceProviderRegistrationId: string;
  commitment: string;
}

export interface SpAttributeDto {
  attributeType: string;
  source: string;
  assetId: string;
  originalBlindingFactor: string;
  originalCommitment: string;
  issuingCommitment: string;
  validated: boolean;
  content: string;
  isOverriden: boolean;
}

export interface IdentityAttributeValidationDescriptorDto {
  attributeType: string;
  attributeTypeName: string;
  validationType: string;
  validationTypeName: string;
  validationCriterionTypes: string[];
}

export interface IdentityAttributeValidationDefinitionDto {
  attributeType: string;
  validationType: string;
  criterionValue: string;
}

export class IdentityAttributeValidationDefinitionClassDto implements IdentityAttributeValidationDefinitionDto {
  attributeType: string;
  attributeTypeName: string;
  validationType: string;
  validationTypeName: string;
  criterionValue: string;
}

export interface ServiceProviderInfoDto {
  id: string;
  description: string;
  target: string;
}

export interface EmployeeGroup {
  groupId: number;
  groupName: string;
}

export interface EmployeeRecord {
  employeeId: number;
  description: string;
  rawRootAttribute: string;
  assetId: string;
  registrationCommitment: string;
  groupId: number;
}

export interface AllowedSigner {
  allowedSignerId: number;
  groupOwner: string;
  groupName: string;
}

export interface SpDocument {
  documentId: number;
  documentName: string;
  hash: string;
  allowedSigners: AllowedSigner[];
  signatures: SpDocumentSignature[];
}

export interface SpDocumentSignature {
  documentId: number;
  signatureId: number;
  documentHash: string;
  documentRecordHeight: number;
  signatureRecordHeight: number;
}
