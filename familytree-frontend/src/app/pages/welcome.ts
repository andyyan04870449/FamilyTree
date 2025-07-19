import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-welcome',
  templateUrl: './welcome.html',
  styleUrls: ['./welcome.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule]
})
export class WelcomeComponent {}
