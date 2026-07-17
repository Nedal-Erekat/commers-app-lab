import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatResponse } from '../models/chat';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/chat`;

  sendMessage(message: string, conversationId: string | null): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(this.baseUrl, { message, conversationId });
  }
}
