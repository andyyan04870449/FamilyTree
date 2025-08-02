export interface AuditViewerProps {
  canManage?: boolean;
  showStatistics?: boolean;
  autoRefresh?: boolean;
  refreshInterval?: number;
  pageSize?: number;
}

export interface AuditLogEntry {
  id: string;
  eventType: string;
  action: string;
  userId?: string;
  userName?: string;
  userRole?: string;
  resourceType?: string;
  resourceId?: string;
  ipAddress?: string;
  userAgent?: string;
  success: boolean;
  errorMessage?: string;
  additionalData?: Record<string, any>;
  occurredAt: Date;
  securityLevel?: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
}

export interface AuditLogFilter {
  timeRange?: {
    startDate?: Date;
    endDate?: Date;
  };
  userId?: string;
  userName?: string;
  eventTypes?: string[];
  resourceType?: string;
  securityLevels?: string[];
  success?: boolean;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortDirection?: 'ASC' | 'DESC';
}

export interface AuditLogQueryResult {
  logs: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditStatistics {
  totalEvents: number;
  successRate: number;
  suspiciousEvents: number;
  uniqueUsers: number;
  eventsByType: Record<string, number>;
  eventsByHour: Record<string, number>;
}