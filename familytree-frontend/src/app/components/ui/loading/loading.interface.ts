export type LoadingSize = 'sm' | 'md' | 'lg' | 'xl';
export type LoadingVariant = 'spinner' | 'dots' | 'pulse' | 'bars';

export interface LoadingProps {
  size?: LoadingSize;
  variant?: LoadingVariant;
  message?: string;
  fullScreen?: boolean;
  overlay?: boolean;
  color?: string;
}