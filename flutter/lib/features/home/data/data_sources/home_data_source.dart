import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../../../leaves/data/models/leave_item_model.dart';
import '../models/leave_balance_model.dart';
import '../models/unread_count_dto.dart';

/// Data source for home screen data
class HomeDataSource {
  final DioClient _dioClient;

  HomeDataSource(this._dioClient);

  /// Fetches remaining leave balance for the user
  Future<List<LeaveBalanceModel>> getRemainingLeaves() async {
    final response = await _dioClient.get(ApiEndpoints.remainingLeaves);
    final data = response.data as List<dynamic>;
    return data
        .map((json) => LeaveBalanceModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Fetches recent leaves for the user (limited)
  Future<List<LeaveItem>> getRecentLeaves({int limit = 5}) async {
    final response = await _dioClient.get(
      ApiEndpoints.myLeaves,
      queryParameters: {
        'PageNumber': 1,
        'PageSize': limit,
      },
    );
    final data = response.data as Map<String, dynamic>;
    final items = data['items'] as List<dynamic>? ?? [];
    return items
        .map((json) => LeaveItem.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<UnreadCountDto> getUnreadCount() async {
    final res = await _dioClient.get(ApiEndpoints.notificationUnreadCount);
    final data = res.data;

    // supports: {count: 3} OR {data: {count: 3}}
    if (data is Map<String, dynamic>) {
      final inner = (data['data'] is Map<String, dynamic>) ? data['data'] as Map<String, dynamic> : data;
      return UnreadCountDto.fromJson(inner);
    }
    return const UnreadCountDto(count: 0);
  }
}
