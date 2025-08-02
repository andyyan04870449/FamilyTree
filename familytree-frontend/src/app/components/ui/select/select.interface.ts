import { SelectOption } from '../../../../shared/interfaces/select-option.interface';

export type SelectSize = 'sm' | 'md' | 'lg';
export type SelectVariant = 'default' | 'outlined' | 'filled';

export interface SelectProps {
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
  searchable?: boolean;
  clearable?: boolean;
  size?: SelectSize;
  variant?: SelectVariant;
  error?: boolean;
  loading?: boolean;
}

export { SelectOption };