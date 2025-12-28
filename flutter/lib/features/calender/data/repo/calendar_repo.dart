import '../../../../core/error/dio_failure_mapper.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/error/failures.dart';
import '../../../../core/network/api_result.dart';
import '../../../../core/constants/api_endpoints.dart';

import '../../models/leave_balance_summary.dart';
import '../../models/paginated_leave_response.dart';
import '../../models/calendar_data_models.dart';

class CalendarRepository {
  final DioClient _client;
  CalendarRepository(this._client);

  Future<ApiResult<List<LeaveBalanceSummary>>> getRemainingLeaves() async {
    try {
      final res = await _client.get(ApiEndpoints.remainingLeaves);
      final list = (res.data as List? ?? []);
      final data = list
          .map((e) => LeaveBalanceSummary.fromJson((e as Map).cast<String, dynamic>()))
          .toList();
      return Success(data);
    } catch (e) {
      return Error(DioFailureMapper.map(e));
    }

  }

  Future<ApiResult<PaginatedLeaveResponse>> getMyLeaves({
    required int pageNumber,
    required int pageSize,
  }) async {
    try {
      final res = await _client.get(
        ApiEndpoints.myLeaves,
        queryParameters: {
          "PageNumber": pageNumber,
          "PageSize": pageSize,
        },
      );
      final data = PaginatedLeaveResponse.fromJson((res.data as Map).cast<String, dynamic>());
      return Success(data);
    } catch (e) {
      return Error(DioFailureMapper.map(e));
    }

  }

  Future<ApiResult<PaginatedLeaveResponse>> getPendingLeaves({
    String? status,
    required int pageNumber,
    required int pageSize,
  }) async {
    try {
      final res = await _client.get(
        ApiEndpoints.pendingApprovals,
        queryParameters: {
          if (status != null) "status": status,
          "PageNumber": pageNumber,
          "PageSize": pageSize,
        },
      );
      final data = PaginatedLeaveResponse.fromJson((res.data as Map).cast<String, dynamic>());
      return Success(data);
    } catch (e) {
      return Error(DioFailureMapper.map(e));
    }

  }

  /// Fetches calendar data for the given date range
  Future<ApiResult<CalendarDataResponse>> getCalendarData({
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (startDate != null) {
        queryParams['startDate'] = _formatDate(startDate);
      }
      if (endDate != null) {
        queryParams['endDate'] = _formatDate(endDate);
      }

      final res = await _client.get(
        ApiEndpoints.calendarData,
        queryParameters: queryParams,
      );
      final data = CalendarDataResponse.fromJson((res.data as Map).cast<String, dynamic>());
      return Success(data);
    } catch (e) {
      return Error(DioFailureMapper.map(e));
    }
  }

  String _formatDate(DateTime date) {
    return '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
  }
}
