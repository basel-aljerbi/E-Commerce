export interface AuditLogDto {
  id: number;
  action: string;
  entityType: string;
  entityId: number | null;
  userEmail: string | null;
  oldValues: string | null;
  newValues: string | null;
  timestamp: string;
}

export interface AuditLogQueryParams {
  pageNumber?: number;
  pageSize?: number;
  action?: string;
  entityType?: string;
  fromDate?: string;
  toDate?: string;
}
