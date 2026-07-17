import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Product } from '../models/product';
import { AuthService } from '../services/auth.service';
import { CartService } from '../services/cart.service';
import { ProductService } from '../services/product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly products = signal<Product[]>([]);
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly source = signal('');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly searchTerm = signal('');
  readonly addedProductId = signal<number | null>(null);

  private readonly pageSize = 20;

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.loading.set(true);
    this.error.set('');

    this.productService.getProducts(page, this.pageSize).subscribe({
      next: (result) => {
        this.products.set(result.data);
        this.page.set(result.page);
        this.totalPages.set(result.totalPages);
        this.source.set(result.source);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not reach the Catalog API.');
        this.loading.set(false);
      }
    });
  }

  runSearch(): void {
    const term = this.searchTerm().trim();
    if (!term) {
      this.loadPage(1);
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.productService.search(term).subscribe({
      next: (results) => {
        this.products.set(results);
        this.source.set('Search');
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not reach the Catalog API.');
        this.loading.set(false);
      }
    });
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.loadPage(this.page() + 1);
    }
  }

  previousPage(): void {
    if (this.page() > 1) {
      this.loadPage(this.page() - 1);
    }
  }

  addToCart(productId: number): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/' } });
      return;
    }

    this.cartService.addItem(productId, 1).subscribe({
      next: () => {
        this.addedProductId.set(productId);
        setTimeout(() => this.addedProductId.set(null), 1500);
      },
      error: () => this.error.set('Could not add to cart.')
    });
  }
}
