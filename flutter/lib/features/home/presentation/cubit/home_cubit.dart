import 'package:dio/dio.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/models/leave_balance_model.dart';
import '../../data/repositories/home_repository.dart';
import '../../../leaves/data/models/leave_item_model.dart';

part 'home_state.dart';

/// Cubit for managing employee home screen data
class HomeCubit extends Cubit<HomeState> {
  final HomeRepository _repository;

  int unreadCount = 0;
  HomeCubit(this._repository) : super(const HomeInitial());

  /// Initialize home screen data
  Future<void> initialize() async {
    print('[HomeCubit] initialize() called');
    emit(const HomeLoaded(isLoadingBalance: true, isLoadingLeaves: true));

    // Load both in parallel
    await Future.wait([
      _loadLeaveBalance(),
      _loadRecentLeaves(),
      _loadUnreadCount(),
    ]);
    print('[HomeCubit] initialize() completed');
  }

  Future<void> _loadUnreadCount() async {
    try {
      final count = await _repository.getUnreadCount();
      unreadCount = count;

      // force rebuild without changing state structure
      final s = state;
      if (s is HomeLoaded) {
        emit(s.copyWith()); // same state fields, new emission -> rebuild
      }
    } catch (_) {
      // ignore count failure (don’t break home)
    }
  }

  /// Public method to refresh unread count (called on FCM notification received)
  Future<void> refreshUnreadCount() async {
    await _loadUnreadCount();
  }

  Future<void> _loadLeaveBalance() async {
    try {
      final balance = await _repository.getRemainingLeaves();
      final currentState = state;
      if (currentState is HomeLoaded) {
        emit(currentState.copyWith(
          leaveBalance: balance,
          isLoadingBalance: false,
        ));
      }
    } on DioException catch (e) {
      final currentState = state;
      if (currentState is HomeLoaded) {
        emit(currentState.copyWith(
          isLoadingBalance: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  Future<void> _loadRecentLeaves() async {
    try {
      final leaves = await _repository.getRecentLeaves(limit: 5);
      final currentState = state;
      if (currentState is HomeLoaded) {
        emit(currentState.copyWith(
          recentLeaves: leaves,
          isLoadingLeaves: false,
        ));
      }
    } on DioException catch (e) {
      final currentState = state;
      if (currentState is HomeLoaded) {
        emit(currentState.copyWith(
          isLoadingLeaves: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Refresh all home data
  Future<void> refresh() async {
    await initialize();
  }

  String _extractErrorMessage(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      if (data is Map<String, dynamic>) {
        final message = data['message'] ?? data['Message'] ?? data['error'];
        if (message != null) return message.toString();
      }
    }
    return 'Unable to load data. Please try again.';
  }
}
