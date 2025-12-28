import 'package:dio/dio.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/models/leave_item_model.dart';
import '../../data/models/leave_status_model.dart';
import '../../data/repositories/leaves_repository.dart';

part 'leaves_state.dart';

/// Cubit for managing employee leaves list
class LeavesCubit extends Cubit<LeavesState> {
  final LeavesRepository _repository;
  static const int _pageSize = 10;

  LeavesCubit(this._repository) : super(const LeavesInitial());

  /// Initialize the leaves screen
  Future<void> initialize() async {
    emit(const LeavesLoaded(isLoadingStatuses: true, isLoadingLeaves: true));

    // Load statuses and leaves in parallel
    await Future.wait([
      _loadStatuses(),
      _loadLeaves(),
    ]);
  }

  Future<void> _loadStatuses() async {
    try {
      final statuses = await _repository.getLeaveStatuses();
      final currentState = state;
      if (currentState is LeavesLoaded) {
        emit(currentState.copyWith(
          statuses: statuses,
          isLoadingStatuses: false,
        ));
      }
    } on DioException catch (e) {
      final currentState = state;
      if (currentState is LeavesLoaded) {
        emit(currentState.copyWith(
          isLoadingStatuses: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  Future<void> _loadLeaves({bool refresh = false}) async {
    final currentState = state;
    if (currentState is! LeavesLoaded) return;

    if (refresh) {
      emit(currentState.copyWith(isLoadingLeaves: true, currentPage: 1));
    }

    try {
      final response = await _repository.getMyLeaves(
        pageNumber: 1,
        pageSize: _pageSize,
      );

      final updatedState = state;
      if (updatedState is LeavesLoaded) {
        emit(updatedState.copyWith(
          leaves: response.items,
          isLoadingLeaves: false,
          currentPage: 1,
          hasMore: response.hasMore,
          clearErrorMessage: true,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is LeavesLoaded) {
        emit(updatedState.copyWith(
          isLoadingLeaves: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Load more leaves (pagination)
  Future<void> loadMore() async {
    final currentState = state;
    if (currentState is! LeavesLoaded) return;
    if (currentState.isLoadingMore || !currentState.hasMore) return;

    emit(currentState.copyWith(isLoadingMore: true));

    try {
      final nextPage = currentState.currentPage + 1;
      final response = await _repository.getMyLeaves(
        pageNumber: nextPage,
        pageSize: _pageSize,
      );

      final updatedState = state;
      if (updatedState is LeavesLoaded) {
        emit(updatedState.copyWith(
          leaves: [...updatedState.leaves, ...response.items],
          isLoadingMore: false,
          currentPage: nextPage,
          hasMore: response.hasMore,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is LeavesLoaded) {
        emit(updatedState.copyWith(
          isLoadingMore: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Select a status filter
  void selectStatus(String? status) {
    final currentState = state;
    if (currentState is LeavesLoaded) {
      if (status == null) {
        emit(currentState.copyWith(clearSelectedStatus: true));
      } else {
        emit(currentState.copyWith(selectedStatus: status));
      }
    }
  }

  /// Refresh leaves list
  Future<void> refresh() async {
    await _loadLeaves(refresh: true);
  }

  /// Extract user-friendly error message from DioException
  String _extractErrorMessage(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      if (data is Map<String, dynamic>) {
        final message = data['message'] ?? data['Message'] ?? data['error'];
        if (message != null) return message.toString();
      }

      switch (e.response!.statusCode) {
        case 400:
          return 'Invalid request.';
        case 401:
          return 'Session expired. Please login again.';
        case 403:
          return 'Access denied.';
        case 404:
          return 'Not found.';
        case 500:
          return 'Server error. Please try again later.';
        default:
          return 'Error: ${e.response!.statusCode}';
      }
    }

    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return 'Connection timeout.';
    }

    if (e.type == DioExceptionType.connectionError) {
      return 'Unable to connect to server.';
    }

    return 'Network error. Please try again.';
  }
}
