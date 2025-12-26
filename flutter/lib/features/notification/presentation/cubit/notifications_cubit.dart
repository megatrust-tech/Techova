import 'package:bloc/bloc.dart';
import '../../data/data_sources/notification_dto.dart';
import '../../data/repositories/notifications_repo.dart';
import 'notifications_state.dart';

class NotificationsCubit extends Cubit<NotificationsState> {
  final NotificationsRepo _repo;

  NotificationsCubit(this._repo) : super(const NotificationsInitial());

  List<NotificationDto> _cache = [];

  Future<void> load({int pageNumber = 1, int pageSize = 10}) async {
    emit(const NotificationsLoading());
    try {
      final list = await _repo.getMyNotifications(
        pageNumber: pageNumber,
        pageSize: pageSize,
      );
      _cache = list;
      emit(NotificationsSuccess(list));
    } catch (e) {
      emit(const NotificationsError('Failed to load notifications'));
    }
  }

  Future<void> markAsRead(int id) async {
    final current = _cache;
    if (current.isEmpty) return;

    emit(NotificationsMarkingRead(current));
    try {
      await _repo.markRead(id);

      _cache = current
          .map((n) => n.id == id ? _copyWithIsRead(n, true) : n)
          .toList();

      emit(NotificationsSuccess(_cache));
    } catch (e) {
      emit(const NotificationsError('Failed to mark notification as read'));
      emit(NotificationsSuccess(_cache)); // restore UI
    }
  }

  Future<void> markAllAsRead() async {
    final current = _cache;
    emit(NotificationsMarkAllReadLoading(current));
    try {
      await _repo.markAllAsRead();

      _cache = current.map((n) => _copyWithIsRead(n, true)).toList();
      emit(NotificationsSuccess(_cache));
    } catch (e) {
      emit(const NotificationsError('Failed to mark all as read'));
      emit(NotificationsSuccess(_cache));
    }
  }

  NotificationDto _copyWithIsRead(NotificationDto n, bool isRead) {
    return NotificationDto(
      id: n.id,
      title: n.title,
      message: n.message,
      isRead: isRead,
      createdAt: n.createdAt,
      timeAgo: n.timeAgo,
    );
  }
}
