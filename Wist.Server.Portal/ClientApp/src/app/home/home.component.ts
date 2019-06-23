import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  constructor(titleService: Title) {
    titleService.setTitle("Wist Demo Portal Home");
  }
}
