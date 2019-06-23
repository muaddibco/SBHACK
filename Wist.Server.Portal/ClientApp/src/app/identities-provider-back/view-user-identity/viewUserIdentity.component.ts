import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { IdentityDto, IdentityClassDto, IdentityAttributeClassDto, IdentityAttributesSchemaDto, IdentitiesService } from '../identities.service';
import { Title } from '@angular/platform-browser';

@Component({
  templateUrl: './viewUserIdentity.component.html',
})

export class ViewUserIdentityComponent implements OnInit {

  public identityAttributesSchema: IdentityAttributesSchemaDto;
  public identity: IdentityDto;
  public isLoaded: boolean;

  constructor(private identityService: IdentitiesService,private router: Router,private route: ActivatedRoute,titleService: Title) {
    this.isLoaded = false;
    this.identityAttributesSchema = null;
    let tokenInfo = JSON.parse(sessionStorage.getItem('TokenInfo'));
    titleService.setTitle(tokenInfo.accountInfo);
  }

  ngOnInit() {
    this.identityService.getIdentityAttributesSchema().subscribe(r => {
      this.identityAttributesSchema = r;
    });

    this.identityService.getIdentity(this.route.snapshot.paramMap.get('id')).subscribe(r => {
      this.identity = r;
      this.isLoaded = true;
    });
  }

  getAssociatedAttrContent(attrType: string) {
    for (var attr of this.identity.associatedAttributes) {
      if (attr.attributeType == attrType) {
        return attr.content;
      }
    }

    return null;
  }

  getAssociatedAttrCommitment(attrType: string) {
    for (var attr of this.identity.associatedAttributes) {
      if (attr.attributeType == attrType) {
        return attr.originatingCommitment;
      }
    }

    return null;
  }

  onSend() {
    this.router.navigate(['/transfer-identity/' + this.route.snapshot.paramMap.get('id')]);
  }

  onBack() {
    this.router.navigate(['/identityProvider']);
  }
}
