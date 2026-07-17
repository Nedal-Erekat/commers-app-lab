import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';

const passwordsMatch = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal('');
  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatch }
  );

  submit(): void {
    if (this.form.invalid) return;

    this.error.set('');
    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();

    this.authService.register(email, password).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/account');
      },
      error: (err) => {
        this.submitting.set(false);
        const messages: string[] | undefined = err?.error?.errors;
        this.error.set(messages?.join(' ') ?? 'Registration failed.');
      }
    });
  }
}
