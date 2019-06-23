import { Component, Input, Inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { UserAttributeDto, UserAttributeLastUpdateDto, UserAttributeTransferDto, UserAttributeTransferWithValidationsDto } from '../user.Service'

@Component({
    selector: 'app-user-claims',
    templateUrl: './user-claims.component.html',
    styleUrls: ['./user-claims.component.css']
})
/** user-claims component*/
export class UserClaimsComponent implements OnInit {

  //@Input() public id: string = '';
  public userAttributes: UserAttributeDto[];
  public hubConnection: HubConnection;

    /** user-claims ctor */
  constructor(private httpClient: HttpClient, @Inject('BASE_URL') private baseUrl: string, private route: ActivatedRoute) {
    this.userAttributes = [];

    httpClient.get<UserAttributeDto[]>(baseUrl + 'User/GetUserAttributes').
      subscribe(r => {
        this.userAttributes = r;
      });
  }

  ngOnInit() {
    this.hubConnection = new HubConnectionBuilder().withUrl("/identitiesHub").build();
    var accountId = sessionStorage.getItem('AccountId');
    this.hubConnection.on("PushAttribute", (i) => {
      for (var a of this.userAttributes) {
        if (a.originalBlindingFactor == (i as UserAttributeDto).originalBlindingFactor) {
          return;
        }
      }
      this.userAttributes.push(i);
    });

    this.hubConnection.start()
      .then(() => {
        console.log("Connection started");
        this.hubConnection.invoke("AddToGroup", accountId);
      })
      .catch(err => { console.error(err); });

    this.definePushUserAttributeUpdate();

    this.definePushUserAttributeLastUpdate();
  }

  private definePushUserAttributeLastUpdate() {
        this.hubConnection.on("PushUserAttributeLastUpdate", (i) => {
            if (i != null) {
                var attributeUpdate = (i as UserAttributeLastUpdateDto);
                if (attributeUpdate != null) {
                    var attributeToUpdate = this.userAttributes.find(t => t.assetId == attributeUpdate.assetId && !t.isOverriden);
                    if (attributeToUpdate != null) {
                        attributeToUpdate.lastBlindingFactor = attributeUpdate.lastBlindingFactor;
                        attributeToUpdate.lastCommitment = attributeUpdate.lastCommitment;
                        attributeToUpdate.lastDestinationKey = attributeUpdate.lastDestinationKey;
                        attributeToUpdate.lastTransactionKey = attributeUpdate.lastTransactionKey;
                    }
                }
            }
        });
    }

  private definePushUserAttributeUpdate() {
        this.hubConnection.on("PushUserAttributeUpdate", (i) => {
            if (i != null) {
                var attributeUpdate = (i as UserAttributeDto);
                if (attributeUpdate != null) {
                    var originalBlindingFactor = (i as UserAttributeDto).originalBlindingFactor;
                    var attributeToUpdate = this.userAttributes.find(t => t.originalBlindingFactor == originalBlindingFactor);
                    if (attributeToUpdate != null) {
                        attributeToUpdate.attributeType = attributeUpdate.attributeType;
                        attributeToUpdate.content = attributeUpdate.content;
                        attributeToUpdate.isOverriden = attributeUpdate.isOverriden;
                        attributeToUpdate.lastBlindingFactor = attributeUpdate.lastBlindingFactor;
                        attributeToUpdate.lastCommitment = attributeUpdate.lastCommitment;
                        attributeToUpdate.lastDestinationKey = attributeUpdate.lastDestinationKey;
                        attributeToUpdate.lastTransactionKey = attributeUpdate.lastTransactionKey;
                        attributeToUpdate.originalBlindingFactor = attributeUpdate.originalBlindingFactor;
                        attributeToUpdate.source = attributeUpdate.source;
                        attributeToUpdate.validated = attributeUpdate.validated;
                    }
                }
            }
        });
    }

  validateContent(attribute: UserAttributeDto, input) {
    console.log("inputText:" + input);
    console.log("attribute:" + attribute);
    if (attribute != null) {
      var attributeDtoClone = new UserAttributeDto();
      attributeDtoClone.attributeType = attribute.attributeType;
      attributeDtoClone.content = attribute.content;
      attributeDtoClone.lastCommitment = attribute.lastCommitment;
      attributeDtoClone.originalCommitment = attribute.originalCommitment;
      attributeDtoClone.validated = attribute.validated;
      attribute.content = input;
      console.log(attribute);
      this.httpClient.post<UserAttributeDto>(this.baseUrl + 'User/ValidateAttribute',  { attributeType: attribute.attributeType, source: attribute.source, assetId: attribute.assetId, originalBlindingFactor: attribute.originalBlindingFactor, originalCommitment: attribute.originalCommitment, lastBlindgingFactor: attribute.lastBlindingFactor, lastCommitment: attribute.lastCommitment, lastTransactionKey: attribute.lastTransactionKey, lastDestinationKey: attribute.lastDestinationKey, validated: attribute.validated, content: attribute.content, isOverriden: attribute.isOverriden}).
        subscribe(r => {
          var validatedObj = r;
          console.log("validatedObj:" + validatedObj);
          attribute.content = validatedObj.content;
          attribute.validated = validatedObj.validated;
        });
    }
  }
}

