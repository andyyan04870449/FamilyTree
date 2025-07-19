import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private keywordSearchClickSource = new Subject<void>();
  keywordSearchClick$ = this.keywordSearchClickSource.asObservable();

  emitKeywordSearchClick() {
    this.keywordSearchClickSource.next();
  }
} 