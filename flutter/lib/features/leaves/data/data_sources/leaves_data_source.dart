import 'package:dio/dio.dart';

import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../models/leave_item_model.dart';
import '../models/leave_status_model.dart';
import '../models/pending_approval_count_model.dart';
import '../models/leave_audit_log_model.dart';

/// Data source for leaves list operations
class LeavesDataSource {
  final DioClient _dioClient;

  LeavesDataSource(this._dioClient);

  /// Fetches available leave statuses from API
  Future<List<LeaveStatusModel>> getLeaveStatuses() async {
    final response = await _dioClient.get(ApiEndpoints.leaveStatuses);
    final data = response.data as List<dynamic>;
    return data
        .map((json) => LeaveStatusModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Fetches employee's own leaves (paginated)
  Future<PaginatedLeavesResponse> getMyLeaves({
    int pageNumber = 1,
    int pageSize = 10,
  }) async {
    final response = await _dioClient.get(
      ApiEndpoints.myLeaves,
      queryParameters: {
        'PageNumber': pageNumber,
        'PageSize': pageSize,
      },
    );
    return PaginatedLeavesResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Fetches pending approvals for manager/hr (paginated with status filter)
  Future<PaginatedLeavesResponse> getPendingApprovals({
    int pageNumber = 1,
    int pageSize = 10,
    String? status,
  }) async {
    final queryParams = <String, dynamic>{
      'PageNumber': pageNumber,
      'PageSize': pageSize,
    };
    if (status != null) {
      queryParams['status'] = status;
    }
    
    final response = await _dioClient.get(
      ApiEndpoints.pendingApprovals,
      queryParameters: queryParams,
    );
    return PaginatedLeavesResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Fetches pending approval count for manager/HR
  /// Response: {"pendingManagerApproval":4,"pendingHRApproval":0,"totalPending":4}
  Future<PendingApprovalCount> getPendingApprovalCount() async {
    final response = await _dioClient.get(ApiEndpoints.pendingApprovalCount);
    final data = response.data as Map<String, dynamic>;
    return PendingApprovalCount.fromJson(data);
  }

  /// Fetches leave request history/audit log
  Future<List<LeaveAuditLogDto>> getRequestHistory(int requestId) async {
    final response = await _dioClient.get(ApiEndpoints.leaveHistory(requestId));
    final data = response.data as List<dynamic>;
    return data
        .map((json) => LeaveAuditLogDto.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Downloads audit logs as CSV bytes
  /// Returns the raw bytes of the CSV file
  Future<List<int>> downloadAuditLogs() async {
    final response = await _dioClient.get(
      ApiEndpoints.auditLogsDownload,
      options: Options(responseType: ResponseType.bytes),
    );
    return response.data as List<int>;
  }
}
