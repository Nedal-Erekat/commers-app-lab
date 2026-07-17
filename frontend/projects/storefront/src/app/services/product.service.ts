import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult, Product } from '../models/product';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.catalogApiUrl}/api/products`;

  getProducts(page = 1, pageSize = 20): Observable<PagedResult<Product>> {
    return this.http.get<PagedResult<Product>>(this.baseUrl, {
      params: { page, pageSize }
    });
  }

  search(term: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/search`, {
      params: { q: term }
    });
  }
}
