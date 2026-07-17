export interface Product {
  id: number;
  name: string;
  category: string;
  price: number;
  stock: number;
  createdAt: string;
}

export interface PagedResult<T> {
  source: string;
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
