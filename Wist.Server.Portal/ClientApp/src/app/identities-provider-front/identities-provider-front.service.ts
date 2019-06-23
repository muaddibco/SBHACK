import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class IdentitiesProviderFrontService {

  constructor(private http: HttpClient) { }

  getIdentityProviders() {
    return this.http.get<IdentityProviderInfoDto[]>('/IdentityProvider/All');
  }

  getIdentityProvider(accountId: string) {
    return this.http.get<IdentityProviderInfoDto>('/IdentityProvider/ById/' + accountId);
  }
}

export interface IdentityProviderInfoDto {
  id: string;
  description: string;
  target: string;
}
