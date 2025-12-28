// features/notifications/data/data_sources/notifications_service.dart
import 'package:dio/dio.dart';

import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';

class NotificationsService {
  final DioClient _client;

  NotificationsService(this._client);

  Future<dynamic> getMyNotifications(int pageNumber, int pageSize) async {
    final Response res = await _client.get(
      ApiEndpoints.notifications,
      queryParameters: {
        'pageNumber': pageNumber,
        'pageSize': pageSize,
      },
    );
    return res.data;
  }

  Future<int> getUnreadNotificationsCount() async {
    final Response res = await _client.get(ApiEndpoints.notificationUnreadCount);
    final data = res.data;

    if (data is int) return data;
    if (data is num) return data.toInt();

    if (data is Map) {
      final m = data.cast<String, dynamic>();
      final v = m['count'] ?? m['unreadCount'] ?? m['value'] ?? m['data'];
      if (v is int) return v;
      if (v is num) return v.toInt();
    }

    throw Exception('Unexpected unread-count response: ${data.runtimeType}');
  }

  Future<void> markNotificationRead(int id) async {
    await _client.put(ApiEndpoints.notificationRead(id));
  }

  Future<void> markAllNotificationsRead() async {
    await _client.put(ApiEndpoints.notificationReadAll);
  }
}
