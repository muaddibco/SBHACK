import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interceptingHandler } from '@angular/common/http/src/module';

@Injectable()
export class UserService {

  _baseUrl: string;

  constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this._baseUrl = baseUrl;
  }

  getUserDetails() {
    return this.http.get<IUser>(this._baseUrl + "user/GetUserDetails");
  }

  getUserAttributes() {
    return this.http.get<UserAttributeDto[]>(this._baseUrl + 'user/GetUserAttributes')
  }

  duplicateUser(sourceAccountId: string, accountInfo: string) {
    return this.http.post<any>(this._baseUrl + "accounts/DuplicateUserAccount/", { sourceAccountId, accountInfo });
  }

  requestIdentity(target: string, idCardContent: string, password: string, imageContent: string) {
    return this.http.post<any>(this._baseUrl + "user/RequestForIdentity/", { target, idCardContent, password, imageContent });
  }

  getUserAssociatedAttributes() {
    return this.http.get<UserAssociatedAttributeDto[]>(this._baseUrl + "user/GetUserAssociatedAttributes");
  }

  updateUserAssociatedAttributes(attrs: UserAssociatedAttributeDto[]) {
    return this.http.post<any>(this._baseUrl + "user/UpdateUserAssociatedAttributes/", attrs);
  }

  getActionType(target: string) {
    return this.http.get<ActionTypeDto>("/User/GetActionType?actionInfo=" + target);
  }

  getSpRequiredValidations(target: string) {
    return this.http.get<UserAttributeTransferWithValidationsDto>(this._baseUrl + "user/GetSpValidations/" + target);
  }

  sendCompromisedProofs(unauthorizedUse: UnauthorizedUseDto) {
    return this.http.post<any>(this._baseUrl + "user/SendCompromisedProofs/", unauthorizedUse);
  }

  clearCompromised() {
    return this.http.post<any>(this._baseUrl + "user/ClearCompromised", null);
  }

  getDocumentSignatureVerification(documentCreator: string, documentHash: string, documentRecordHeight: string, signatureRecordBlockHeight: string) {
    return this.http.get<DocumentSignatureVerification>("/User/GetDocumentSignatureVerification?documentCreator=" + documentCreator + "&documentHash=" + documentHash + "&documentRecordHeight=" + documentRecordHeight + "&signatureRecordBlockHeight=" + signatureRecordBlockHeight);
  }

  getGroupRelations() {
    return this.http.get<GroupRelation[]>("/User/GetGroupRelations");
  }

  deleteGroupRelation(groupRelationId: number) {
    return this.http.delete("/User/DeleteGroupRelation/" + groupRelationId.toString());
  }

  sendRelationProofs(relationProofs: any) {
    return this.http.post("/User/SendRelationsProofs", relationProofs);
  }
}

export interface UserAssociatedAttributeDto {
  attributeType: string;
  attributeTypeName: string;
  content: string;
}

export interface ActionTypeDto {
  action: string;
  actionInfo: string;
}


export class UserAttributeDto {
  attributeType: string;
  source: string;
  assetId: string;
  originalBlindingFactor: string;
  originalCommitment: string;
  lastBlindingFactor: string;
  lastCommitment: string;
  lastTransactionKey: string;
  lastDestinationKey: string;
  validated: boolean;
  content: string;
  isOverriden: boolean;
}

export class UserAttributeLastUpdateDto {
  assetId: string;
  lastBlindingFactor: string;
  lastCommitment: string;
  lastTransactionKey: string;
  lastDestinationKey: string;
}

export class UserAttributeTransferDto extends UserAttributeDto {
  target: string;
  payload: string;
  imageContent: string;
}

export class UserAttributeTransferWithValidationsDto {
  userAttributeTransfer: UserAttributeTransferDto;
  password: string;
  validations: string[];
}

export interface UnauthorizedUseDto {
  keyImage: string;
  target: string;
}

export class User implements IUser {

  publicViewKey: string;

  publicSpendKey: string;

  id: string;

  accountInfo: string;

  isCompromised: boolean;
}

export interface IUser {

  publicViewKey: string;

  publicSpendKey: string;

  id: string;

  accountInfo: string;

  isCompromised: boolean;
}

export interface DocumentSignatureVerification {
  signatureTransactionFound: boolean;
  documentRecordTransactionFound: boolean;
  documentHashMatch: boolean;
  signerSignatureMatch: boolean;
  allowedGroupRelation: boolean;
  allowedGroupMatching: boolean;
}

export interface GroupRelation {
  groupRelationId: number;
  groupOwnerName: string;
  groupOwnerKey: string;
  groupName: string;
}

export interface RelationsProofs {
  targetSpendKey: string;
  targetViewKey: string
  imageContent: string;
  relations: GroupRelation[];
}

export interface RelationProofsValidationResults {
  imageContent: string;
  isImageCorrect: boolean;
  validationResults: RelationProofValidationResult[];
}

export interface RelationProofValidationResult {
  relatedAttributeOwner: string;
  relatedAttributeContent: string;
  isRelationCorrect: boolean;
}
