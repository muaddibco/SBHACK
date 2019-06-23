import { Component, OnInit } from '@angular/core';
import { IdentityProviderInfoDto, IdentitiesProviderFrontService } from '../identities-provider-front.service';



@Component({
  templateUrl: './identity-providers-list.component.html',
})

export class IdentityProvidersListComponent implements OnInit {

  public isLoaded = false;
  public identityProviders: IdentityProviderInfoDto[];

  constructor(private service: IdentitiesProviderFrontService) {
    this.identityProviders = [];
  }

  ngOnInit() {
    this.service.getIdentityProviders().subscribe(r =>
    {
      this.identityProviders = r;
      this.isLoaded = true;
    });
  }
}
