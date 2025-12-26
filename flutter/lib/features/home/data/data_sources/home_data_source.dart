import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../../../leaves/data/models/leave_item_model.dart';
import '../models/leave_balance_model.dart';

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
}
