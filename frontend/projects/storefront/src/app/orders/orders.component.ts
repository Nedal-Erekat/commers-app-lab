import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Order } from '../models/order';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './orders.component.html'
})
export class OrdersComponent implements OnInit {
  private readonly orderService = inject(OrderService);

  readonly orders = signal<Order[]>([]);
  readonly error = signal('');

  ngOnInit(): void {
    this.orderService.getOrders().subscribe({
      next: (orders) => this.orders.set(orders),
      error: () => this.error.set('Could not load your orders.')
    });
  }
}
