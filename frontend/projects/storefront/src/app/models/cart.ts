export interface CartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface Cart {
  items: CartItem[];
  total: number;
}
