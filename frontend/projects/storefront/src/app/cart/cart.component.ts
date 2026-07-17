import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Cart } from '../models/cart';
import { CartService } from '../services/cart.service';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cart.component.html'
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly cart = signal<Cart | null>(null);
  readonly error = signal('');
  readonly checkingOut = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.cartService.getCart().subscribe({
      next: (cart) => this.cart.set(cart),
      error: () => this.error.set('Could not load your cart.')
    });
  }

  removeItem(productId: number): void {
    this.cartService.removeItem(productId).subscribe({
      next: (cart) => this.cart.set(cart),
      error: () => this.error.set('Could not update your cart.')
    });
  }

  checkout(): void {
    this.error.set('');
    this.checkingOut.set(true);

    this.orderService.checkout().subscribe({
      next: (order) => {
        this.checkingOut.set(false);
        this.router.navigate(['/orders', order.id]);
      },
      error: () => {
        this.checkingOut.set(false);
        this.error.set('Checkout failed.');
      }
    });
  }
}
