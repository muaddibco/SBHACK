import { Component, Inject, SimpleChanges, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})

export class NavMenuComponent implements OnInit {
  isExpanded = false;
  isHidden: boolean;

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { this.isHidden = true; }

  ngOnInit() {
    this.setLogoutButtonVisibility();
  }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }

  logout() {
    console.log("calling logout");
    this.isHidden = true;
    this.http.post<any>('/accounts/logout', null);
    this.http.post<any>(this.baseUrl + '/accounts/logout', null);
    this.http.post(this.baseUrl + '/accounts/logout', null);
    this.http.post(this.baseUrl + 'accounts/logout', null);
  }

  setLogoutButtonVisibility() {
    if (sessionStorage.getItem('TokenInfo')) {
      this.isHidden = false;
    }
    else {
      this.isHidden = true;
    }
  }
}
