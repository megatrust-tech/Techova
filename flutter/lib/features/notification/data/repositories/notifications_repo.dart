// features/notifications/data/repositories/notifications_repo.dart
import '../data_sources/notification_dto.dart';
import '../data_sources/notifications_service.dart';

class NotificationsRepo {
  final NotificationsService _service;

  NotificationsRepo(this._service);

  Future<List<NotificationDto>> getMyNotifications({
    required int pageNumber,
    required int pageSize,
  }) async {
    final res = await _service.getMyNotifications(pageNumber, pageSize);
    final items = _extractItems(res);

    return items
        .map((e) => NotificationDto.fromJson((e as Map).cast<String, dynamic>()))
        .toList();
  }

  Future<int> getUnreadCount() => _service.getUnreadNotificationsCount();

  Future<void> markRead(int id) => _service.markNotificationRead(id);

  Future<void> markAllAsRead() => _service.markAllNotificationsRead();

  List<dynamic> _extractItems(dynamic res) {
    if (res is List) return res;

    if (res is Map) {
      final m = res.cast<String, dynamic>();

      final direct = m['items'] ?? m['data'] ?? m['notifications'];
      if (direct is List) return direct;

      // Sometimes backend wraps again: { data: { items: [...] } }
      final data = m['data'];
      if (data is Map) {
        final dm = data.cast<String, dynamic>();
        final nested = dm['items'] ?? dm['data'] ?? dm['notifications'];
        if (nested is List) return nested;
      }
    }

    throw Exception('Unexpected notifications response: ${res.runtimeType}');
  }
}
