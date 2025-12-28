import 'package:dio/dio.dart';

import '../data_sources/leaves_data_source.dart';
import '../models/leave_item_model.dart';
import '../models/leave_status_model.dart';
import '../models/pending_approval_count_model.dart';

/// Repository for leaves list operations with caching
class LeavesRepository {
  final LeavesDataSource _dataSource;
  
  // In-memory cache for leave statuses
  List<LeaveStatusModel>? _cachedStatuses;

  LeavesRepository(this._dataSource);

  /// Gets leave statuses, using cache if available
  Future<List<LeaveStatusModel>> getLeaveStatuses({bool forceRefresh = false}) async {
    if (!forceRefresh && _cachedStatuses != null) {
      return _cachedStatuses!;
    }

    try {
      _cachedStatuses = await _dataSource.getLeaveStatuses();
      return _cachedStatuses!;
    } on DioException {
      rethrow;
    }
  }

  /// Gets employee's own leaves (paginated)
  Future<PaginatedLeavesResponse> getMyLeaves({
    int pageNumber = 1,
    int pageSize = 10,
  }) async {
    try {
      return await _dataSource.getMyLeaves(
        pageNumber: pageNumber,
        pageSize: pageSize,
      );
    } on DioException {
      rethrow;
    }
  }

  /// Gets pending approvals for manager/hr (paginated with status filter)
  Future<PaginatedLeavesResponse> getPendingApprovals({
    int pageNumber = 1,
    int pageSize = 10,
    String? status,
  }) async {
    try {
      return await _dataSource.getPendingApprovals(
        pageNumber: pageNumber,
        pageSize: pageSize,
        status: status,
      );
    } on DioException {
      rethrow;
    }
  }

  /// Clears the statuses cache
  void clearCache() {
    _cachedStatuses = null;
  }

  /// Gets pending approval count for manager/HR
  Future<PendingApprovalCount> getPendingApprovalCount() async {
    try {
      return await _dataSource.getPendingApprovalCount();
    } on DioException {
      rethrow;
    }
  }

  /// Downloads audit logs as CSV bytes
  Future<List<int>> downloadAuditLogs() async {
    try {
      return await _dataSource.downloadAuditLogs();
    } on DioException {
      rethrow;
    }
  }
}
