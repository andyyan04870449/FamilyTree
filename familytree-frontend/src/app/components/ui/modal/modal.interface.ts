export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | 'full';
export type ModalVariant = 'default' | 'danger' | 'success' | 'warning' | 'info';

export interface ModalProps {
  isOpen?: boolean;
  size?: ModalSize;
  variant?: ModalVariant;
  title?: string;
  showCloseButton?: boolean;
  closeOnBackdropClick?: boolean;
  closeOnEscape?: boolean;
  preventBodyScroll?: boolean;
  centered?: boolean;
}

export interface ConfirmDialogProps extends ModalProps {
  message: string;
  confirmText?: string;
  cancelText?: string;
  isDanger?: boolean;
}

export interface InputDialogProps extends ModalProps {
  inputLabel?: string;
  inputPlaceholder?: string;
  inputType?: 'text' | 'email' | 'password' | 'number';
  inputValue?: string;
  submitText?: string;
  cancelText?: string;
  required?: boolean;
}