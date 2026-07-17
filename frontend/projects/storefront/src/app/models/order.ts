import { CartItem } from './cart';

export interface Order {
  id: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  items: CartItem[];
}
