import '../../data/data_sources/notification_dto.dart';

abstract class NotificationsState {
  const NotificationsState();
}

class NotificationsInitial extends NotificationsState {
  const NotificationsInitial();
}

class NotificationsLoading extends NotificationsState {
  const NotificationsLoading();
}

class NotificationsSuccess extends NotificationsState {
  final List<NotificationDto> items;
  const NotificationsSuccess(this.items);
}

class NotificationsError extends NotificationsState {
  final String message;
  final List<NotificationDto>? cached;
  const NotificationsError(this.message, {this.cached});
}

class NotificationsMarkingRead extends NotificationsState {
  final List<NotificationDto> cached;
  const NotificationsMarkingRead(this.cached);
}

class NotificationsMarkAllReadLoading extends NotificationsState {
  final List<NotificationDto> cached;
  const NotificationsMarkAllReadLoading(this.cached);
}
