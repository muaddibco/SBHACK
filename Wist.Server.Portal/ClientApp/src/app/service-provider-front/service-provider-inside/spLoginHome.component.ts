import { Component, OnInit } from '@angular/core';
import { ServiceProviderFrontService } from '../service-provider-front.service';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { Title } from '@angular/platform-browser';

@Component({
  templateUrl: './spLoginHome.component.html'
})

export class SpLoginHomeComponent implements OnInit {

  public isLoaded: boolean;
  public pageTitle: string;
  public spId: string;
  public isCompromised: boolean;
  public hubConnection: HubConnection;

  constructor(private serviceProviderService: ServiceProviderFrontService,private titleService: Title) {
    console.log("SpLoginHomeComponent constructor");
    this.isLoaded = false;
    this.spId = sessionStorage.getItem('spId');
    this.isCompromised = false;
    
    console.log("SpLoginHomeComponent constructor; spId = " + this.spId);
  }

  ngOnInit() {
    console.log("SpLoginHomeComponent ngOnInit");

    this.hubConnection = new HubConnectionBuilder().withUrl("/identitiesHub").build();

    this.hubConnection.on("PushAuthorizationCompromised", (i) => {
      this.isCompromised = true;
    });

    this.hubConnection.start()
      .then(() => {
        console.log("IdentityHub Connection started");
        this.hubConnection.invoke("AddToGroup", sessionStorage.getItem('sessionKey'));
      })
      .catch(err => { console.error(err); });

    this.serviceProviderService.getServiceProvider(this.spId).subscribe(r => {
      console.log("SpLoginHomeComponent ngOnInit; getServiceProvider.r.description = " + r.description);
      this.pageTitle = r.description;
      this.titleService.setTitle(r.description +  " - Account page");
      this.isLoaded = true;
    });
  }
}
