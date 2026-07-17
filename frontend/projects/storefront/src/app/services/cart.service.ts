import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Cart } from '../models/cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/cart`;

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl);
  }

  addItem(productId: number, quantity = 1): Observable<Cart> {
    return this.http.post<Cart>(`${this.baseUrl}/items`, { productId, quantity });
  }

  removeItem(productId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${this.baseUrl}/items/${productId}`);
  }
}
