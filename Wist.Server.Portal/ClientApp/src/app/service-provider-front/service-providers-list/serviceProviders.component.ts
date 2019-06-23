import { Component, OnInit } from '@angular/core';
import { ServiceProviderInfoDto, ServiceProviderFrontService } from '../service-provider-front.service';
import { Title } from '@angular/platform-browser';

@Component({
  templateUrl: './serviceProviders.component.html'
})

export class ServiceProvidersComponent implements OnInit {
  public serviceProviders: ServiceProviderInfoDto[];
  public isLoaded: boolean;

  constructor(private serviceProviderService: ServiceProviderFrontService, titleService: Title) {
    this.isLoaded = false;
    titleService.setTitle("Wist Demo Portal - Service Providers");
  }

  ngOnInit() {
    this.serviceProviderService.getServiceProviders().subscribe(r => {
      this.serviceProviders = r;
      this.isLoaded = true;
    });
  }
}
