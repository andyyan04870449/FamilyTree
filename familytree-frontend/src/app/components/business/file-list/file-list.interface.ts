export interface FileUploadModel {
  id: string;
  originalFilename: string;
  filename: string;
  fileSize: number;
  uploadTime: Date;
  status: FileStatus;
  isMerged?: boolean;
  projectId?: string;
  userId?: string;
  errorMessage?: string;
  processedRecords?: number;
  totalRecords?: number;
}

export type FileStatus = 'PENDING' | 'PROCESSING' | 'COMPLETED' | 'FAILED' | 'CANCELLED';

export interface DeleteImpactResponse {
  success: boolean;
  fileName: string;
  personCount: number;
  personNames: string[];
  hasMorePersons: boolean;
  message?: string;
}

export interface FileListProps {
  files?: FileUploadModel[];
  loading?: boolean;
  error?: string;
  showActions?: boolean;
  allowDelete?: boolean;
  allowDownload?: boolean;
  onFileDelete?: (file: FileUploadModel) => void;
  onFileDownload?: (file: FileUploadModel) => void;
  onRefresh?: () => void;
}

export interface FileListFilter {
  status?: FileStatus[];
  dateRange?: {
    startDate?: Date;
    endDate?: Date;
  };
  searchTerm?: string;
}