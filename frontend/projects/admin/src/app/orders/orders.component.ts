import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AdminOrder } from '../models/order';
import { OrderService } from '../services/order.service';

const STATUSES = ['Placed', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './orders.component.html'
})
export class OrdersComponent {
  private readonly orderService = inject(OrderService);

  readonly orders = signal<AdminOrder[]>([]);
  readonly error = signal('');
  readonly statuses = STATUSES;

  constructor() {
    this.load();
  }

  load(): void {
    this.orderService.getAllOrders().subscribe({
      next: (orders) => this.orders.set(orders),
      error: () => this.error.set('Failed to load orders.')
    });
  }

  updateStatus(order: AdminOrder, status: string): void {
    if (status === order.status) return;

    this.orderService.updateStatus(order.id, status).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Failed to update order status.')
    });
  }
}
