import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private fullTextSearchClickSource = new Subject<void>();
  fullTextSearchClick$ = this.fullTextSearchClickSource.asObservable();

  emitFullTextSearchClick() {
    this.fullTextSearchClickSource.next();
  }
} 