import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class ServiceProviderFrontService {
  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  getServiceProviders() {
    return this.http.get<ServiceProviderInfoDto[]>(this.baseUrl + "ServiceProviders/GetAll");
  }

  getServiceProvider(id: string) {
    return this.http.get<ServiceProviderInfoDto>(this.baseUrl + "ServiceProviders/ById/" + id);
  }
  getSessionInfo(id: string) {
    return this.http.get<ServiceProviderSessionDto>(this.baseUrl + "SpUsers/GetSessionInfo/" + id);
  }

  getSpDocuments(id: string) {
    return this.http.get<SpDocument[]>("/SpUsers/GetDocuments/" + id);
  }
}

export interface ServiceProviderSessionDto {
  publicKey: string;
  sessionKey: string;
}

export interface ServiceProviderInfoDto {
  id: string;
  description: string;
  target: string;
}

export interface SpDocument {
  documentId: number;
  documentName: string;
  hash: string;
  signatures: SpDocumentSignature[];
}

export interface SpDocumentSignature {
  documentId: number;
  signatureId: number;
  documentHash: string;
  documentRecordHeight: number;
  signatureRecordHeight: number;
}
