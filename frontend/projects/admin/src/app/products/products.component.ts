import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Product } from '../models/product';
import { ProductService } from '../services/product.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './products.component.html'
})
export class ProductsComponent {
  private readonly productService = inject(ProductService);
  private readonly fb = inject(FormBuilder);

  readonly products = signal<Product[]>([]);
  readonly editingId = signal<number | null>(null);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.productService.getProducts(1, 100).subscribe({
      next: (result) => this.products.set(result.data),
      error: () => this.error.set('Failed to load products.')
    });
  }

  edit(product: Product): void {
    this.editingId.set(product.id);
    this.form.setValue({
      name: product.name,
      category: product.category,
      price: product.price,
      stock: product.stock
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', category: '', price: 0, stock: 0 });
  }

  submit(): void {
    if (this.form.invalid) return;

    this.error.set('');
    const request = this.form.getRawValue();
    const editingId = this.editingId();

    const save$ = editingId === null
      ? this.productService.create(request)
      : this.productService.update(editingId, request);

    save$.subscribe({
      next: () => {
        this.cancelEdit();
        this.load();
      },
      error: () => this.error.set('Failed to save product.')
    });
  }

  delete(product: Product): void {
    if (!confirm(`Delete "${product.name}"?`)) return;

    this.productService.delete(product.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Failed to delete product.')
    });
  }
}
