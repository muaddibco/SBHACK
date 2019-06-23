import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class IdentitiesService {
  constructor(private http: HttpClient) { }

  addIdentity(identity: IdentityDto) {
    return this.http.post<any>('/IdentityProvider/CreateIdentity', identity);
  }

  getIdentity(id: string) {
    return this.http.get<IdentityDto>('/IdentityProvider/GetIdentityById/' + id);
  }

  sendIdentity(idCardContent: string, pkSpendKey: string, pkViewKey: string) {
    return this.http.post<any>('/IdentityProvider/SendAssetIdNew',
      {
        id: idCardContent,
        publicSpendKey: pkSpendKey,
        publicViewKey: pkViewKey
      });
  }

  getIdentityAttributesSchema() {
    return this.http.get<IdentityAttributesSchemaDto>('/IdentityProvider/GetAttributesSchema');
  }
}

export class IdentityAttributeClassDto implements IdentityAttributeDto {
  attributeType: string;
  content: string;
  originatingCommitment: string;
}

export class IdentityClassDto implements IdentityDto {
  numberOfTransfers: number;
  id: string;
  description: string;
  rootAttribute: IdentityAttributeDto;
  associatedAttributes: IdentityAttributeDto[];
}

export interface IdentityDto {
  numberOfTransfers: number;
  id: string;
  description: string;
  rootAttribute: IdentityAttributeDto;
  associatedAttributes: IdentityAttributeDto[];
}

export interface IdentityAttributeDto {
  attributeType: string;
  content: string;
  originatingCommitment: string;
}

export interface IdentityAttributeSchemaDto {
  name: string;
  attributeType: string;
}

export interface IdentityAttributesSchemaDto {
  rootAttribute: IdentityAttributeSchemaDto;
  associatedAttributes: IdentityAttributeSchemaDto[];
}
