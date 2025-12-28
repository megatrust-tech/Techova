import 'dart:io';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

import '../../data/models/leave_item_model.dart';
import '../../data/models/pending_approval_count_model.dart';
import '../../data/repositories/leaves_repository.dart';

part 'pending_approvals_state.dart';

/// Cubit for managing pending approvals list for manager/HR
class PendingApprovalsCubit extends Cubit<PendingApprovalsState> {
  final LeavesRepository _repository;
  final String statusFilter; // 'PendingManager' or 'PendingHR'
  static const int _pageSize = 10;

  PendingApprovalsCubit(this._repository, {required this.statusFilter})
      : super(const PendingApprovalsInitial());

  /// Initialize and load pending approvals
  Future<void> initialize() async {
    emit(const PendingApprovalsLoaded(isLoading: true, isLoadingCount: true));
    await Future.wait([
      _loadApprovals(),
      _loadPendingCount(),
    ]);
  }

  /// Load pending approval count
  Future<void> _loadPendingCount() async {
    try {
      final counts = await _repository.getPendingApprovalCount();
      final currentState = state;
      if (currentState is PendingApprovalsLoaded) {
        emit(currentState.copyWith(
          pendingCounts: counts,
          isLoadingCount: false,
        ));
      }
    } on DioException {
      final currentState = state;
      if (currentState is PendingApprovalsLoaded) {
        emit(currentState.copyWith(isLoadingCount: false));
      }
    }
  }

  Future<void> _loadApprovals({bool refresh = false}) async {
    final currentState = state;
    if (currentState is! PendingApprovalsLoaded) return;

    if (refresh) {
      emit(currentState.copyWith(isLoading: true, currentPage: 1));
    }

    try {
      final response = await _repository.getPendingApprovals(
        pageNumber: 1,
        pageSize: _pageSize,
        status: statusFilter,
      );

      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          leaves: response.items,
          isLoading: false,
          currentPage: 1,
          hasMore: response.hasMore,
          clearErrorMessage: true,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          isLoading: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Load more approvals (pagination)
  Future<void> loadMore() async {
    final currentState = state;
    if (currentState is! PendingApprovalsLoaded) return;
    if (currentState.isLoadingMore || !currentState.hasMore) return;

    emit(currentState.copyWith(isLoadingMore: true));

    try {
      final nextPage = currentState.currentPage + 1;
      final response = await _repository.getPendingApprovals(
        pageNumber: nextPage,
        pageSize: _pageSize,
        status: statusFilter,
      );

      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          leaves: [...updatedState.leaves, ...response.items],
          isLoadingMore: false,
          currentPage: nextPage,
          hasMore: response.hasMore,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          isLoadingMore: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Refresh approvals list
  Future<void> refresh() async {
    final currentState = state;
    if (currentState is PendingApprovalsLoaded) {
      emit(currentState.copyWith(isLoadingCount: true));
    }

    await Future.wait([
      _loadApprovals(refresh: true),
      _loadPendingCount(),
    ]);
  }


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

  /// Download audit logs as CSV and share
  Future<void> downloadAuditLogs() async {
    final currentState = state;
    if (currentState is! PendingApprovalsLoaded) return;
    if (currentState.isDownloading) return;

    emit(currentState.copyWith(isDownloading: true, clearDownloadError: true));

    try {
      final bytes = await _repository.downloadAuditLogs();
      
      // Check for empty response
      if (bytes.isEmpty) {
        final updatedState = state;
        if (updatedState is PendingApprovalsLoaded) {
          emit(updatedState.copyWith(
            isDownloading: false,
            downloadError: 'No audit logs found',
          ));
        }
        return;
      }

      // Generate filename with timestamp
      final timestamp = DateFormat('yyyyMMdd_HHmmss').format(DateTime.now());
      final filename = 'leave_audit_logs_$timestamp.csv';

      // Save to temporary directory
      final tempDir = await getTemporaryDirectory();
      final file = File('${tempDir.path}/$filename');
      await file.writeAsBytes(Uint8List.fromList(bytes));

      // Share the file
      await Share.shareXFiles(
        [XFile(file.path)],
        subject: 'Leave Audit Logs',
      );

      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(isDownloading: false));
      }
    } on DioException catch (e) {
      String errorMessage;
      if (e.response?.statusCode == 401 || e.response?.statusCode == 403) {
        errorMessage = 'Unauthorized access';
      } else {
        errorMessage = _extractErrorMessage(e);
      }

      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          isDownloading: false,
          downloadError: errorMessage,
        ));
      }
    } catch (e) {
      final updatedState = state;
      if (updatedState is PendingApprovalsLoaded) {
        emit(updatedState.copyWith(
          isDownloading: false,
          downloadError: 'Failed to download audit logs',
        ));
      }
    }
  }
}
