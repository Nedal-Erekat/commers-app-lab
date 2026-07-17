import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Order } from '../models/order';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-detail.component.html'
})
export class OrderDetailComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<Order | null>(null);
  readonly error = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.orderService.getOrder(id).subscribe({
      next: (order) => this.order.set(order),
      error: () => this.error.set('Order not found.')
    });
  }
}
