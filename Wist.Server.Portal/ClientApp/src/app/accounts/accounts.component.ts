import { Component, Inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-accounts',
  templateUrl: './accounts.component.html'
})
export class AccountsComponent implements OnInit {
  public accounts: AccountDto[];
  public accountsUser: AccountDto[];
  public accountsSP: AccountDto[];
  public accountsIP: AccountDto[];
  public isLoaded: boolean;

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private router: Router, titleService: Title) {
    this.isLoaded = false;
    this.accountsUser = [];
    this.accountsSP = [];
    this.accountsIP = [];
    titleService.setTitle("Wist Demo Portal - Accounts");
  }

  ngOnInit() {
    this.http.get<AccountDto[]>(this.baseUrl + 'Accounts/GetAll').subscribe(result => {
      this.accounts = result;

      for (var a of result) {
        a.showLogin = false;
        if (a.accountType == 1) {
          this.accountsIP.push(a);
        }
        if (a.accountType == 2) {
          this.accountsSP.push(a);
        }
        if (a.accountType == 3) {
          this.accountsUser.push(a);
        }
      }
      this.isLoaded = true;
    }, error => console.error(error));
  }

  gotoLogin(account: AccountDto) {
    this.router.navigate(['/login', account.accountId], { queryParams: { returnUrl: this.router.url } });
  }

  removeIPAccount(account: AccountDto) {
    if (confirm("Are you sure you want to remove the Account " + account.accountInfo + "?")) {
      this.http.post<any>(this.baseUrl + 'Accounts/RemoveAccount', account).subscribe(r => {
        this.removeAccountFromList(this.accountsIP, account);
      });
    }
  }

  removeSPAccount(account: AccountDto) {
    if (confirm("Are you sure you want to remove the Account " + account.accountInfo + "?")) {
      this.http.post<any>(this.baseUrl + 'Accounts/RemoveAccount', account).subscribe(r => {
        this.removeAccountFromList(this.accountsSP, account);
      });
    }
  }

  removeUserAccount(account: AccountDto) {
    if (confirm("Are you sure you want to remove the Account " + account.accountInfo + "?")) {
      this.http.post<any>(this.baseUrl + 'Accounts/RemoveAccount', account).subscribe(r => {
        this.removeAccountFromList(this.accountsUser, account);
      });
    }
  }

  removeAccountFromList(list: AccountDto[], itemToRemove: AccountDto) {
    let index = -1;
    let found = false;
    for (let item of list) {
      index++;
      if (item.accountId == itemToRemove.accountId) {
        found = true;
        break;
      }
    }

    if (found) {
      list.splice(index, 1);
    }
  }
}

export interface AccountDto {
  accountId: number;
  accountType: number;
  accountInfo: string;
  password: string;
  token: string;
  publicViewKey: string;
  publicSpendKey: string;
  showLogin: boolean;
  error: string;
  isError: boolean;
}
