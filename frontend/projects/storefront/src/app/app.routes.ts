import { Routes } from '@angular/router';
import { AccountComponent } from './account/account.component';
import { CartComponent } from './cart/cart.component';
import { authGuard } from './guards/auth.guard';
import { LoginComponent } from './login/login.component';
import { OrderDetailComponent } from './orders/order-detail.component';
import { OrdersComponent } from './orders/orders.component';
import { ProductListComponent } from './product-list/product-list.component';
import { RegisterComponent } from './register/register.component';

export const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'account', component: AccountComponent, canActivate: [authGuard] },
  { path: 'cart', component: CartComponent, canActivate: [authGuard] },
  { path: 'orders', component: OrdersComponent, canActivate: [authGuard] },
  { path: 'orders/:id', component: OrderDetailComponent, canActivate: [authGuard] }
];
