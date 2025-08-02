export interface Person {
  id: string;
  name: string;
  gender?: string;
  birthday?: string;
  nationality?: string;
  mobile?: string;
  email?: string;
  address?: string;
  createdAt?: Date;
  updatedAt?: Date;
}

export interface PersonTableColumn {
  key: keyof Person;
  label: string;
  sortable?: boolean;
  filterable?: boolean;
  width?: string;
  type?: 'text' | 'date' | 'number' | 'boolean';
  formatter?: (value: any) => string;
}

export interface PersonTableProps {
  data?: Person[];
  columns?: PersonTableColumn[];
  loading?: boolean;
  pagination?: boolean;
  pageSize?: number;
  sortable?: boolean;
  filterable?: boolean;
  selectable?: boolean;
  onRowClick?: (person: Person) => void;
  onSelectionChange?: (selectedPersons: Person[]) => void;
}

export interface SortConfig {
  column: keyof Person;
  direction: 'asc' | 'desc';
}

export interface FilterConfig {
  [key: string]: string;
}