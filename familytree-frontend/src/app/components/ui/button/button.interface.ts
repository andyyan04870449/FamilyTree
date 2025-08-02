export type ButtonVariant = 'default' | 'primary' | 'secondary' | 'danger' | 'favorite' | 'view';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps {
  variant?: ButtonVariant;
  size?: ButtonSize;
  disabled?: boolean;
  loading?: boolean;
  icon?: string;
  label?: string;
  title?: string;
  ariaLabel?: string;
  isActive?: boolean;
}